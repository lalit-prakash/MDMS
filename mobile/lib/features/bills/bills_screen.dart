import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/pdf_export.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../recharge/recharge_screen.dart';

class BillingProfileRow {
  final DateTime billingDate;
  final double cumulativeKwhImport;
  final double cumulativeKvahImport;
  final double cumulativeKwhExport;
  final double cumulativeKvahExport;
  final double averagePowerFactor;
  final double maximumDemandKw;
  final double maximumDemandKva;

  BillingProfileRow.fromJson(Map<String, dynamic> j)
      : billingDate = DateTime.parse(j['billingDate'] as String),
        cumulativeKwhImport = (j['cumulativeKwhImport'] as num).toDouble(),
        cumulativeKvahImport = (j['cumulativeKvahImport'] as num).toDouble(),
        cumulativeKwhExport = (j['cumulativeKwhExport'] as num).toDouble(),
        cumulativeKvahExport = (j['cumulativeKvahExport'] as num).toDouble(),
        averagePowerFactor = (j['averagePowerFactor'] as num).toDouble(),
        maximumDemandKw = (j['maximumDemandKw'] as num).toDouble(),
        maximumDemandKva = (j['maximumDemandKva'] as num).toDouble();
}

final billsProvider = FutureProvider.autoDispose<List<BillingProfileRow>>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  final response = await apiCall(() => client.dio.get('/api/v1/customers/${session.consumerId}/bills'));
  return (response.data as List).map((e) => BillingProfileRow.fromJson(e as Map<String, dynamic>)).toList();
});

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

/// Bill/Statement history from real Billing Profile snapshots (BillingProfile — the meter's own
/// monthly commercial snapshot, per its own doc comment). There is no tariff-calculation/billing
/// engine in this project, so no invented amount-due figure is shown; where the consumer is
/// prepaid, "Amount Charged" is the real sum of that month's ConsumptionDebit wallet transactions
/// (what was actually debited, not a recomputed tariff estimate) -- omitted for postpaid or a
/// month with no matching debits, never fabricated as zero.
class BillsScreen extends ConsumerWidget {
  const BillsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final billsAsync = ref.watch(billsProvider);
    final txAsync = ref.watch(transactionsProvider);

    double? chargedForMonth(DateTime billingDate) {
      return txAsync.maybeWhen(
        data: (rows) {
          final debits = rows.where((t) =>
              t.type == 'ConsumptionDebit' && t.createdAtUtc.year == billingDate.year && t.createdAtUtc.month == billingDate.month);
          if (debits.isEmpty) return null;
          return debits.fold<double>(0, (a, t) => a + t.amount.abs());
        },
        orElse: () => null,
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Bills & Statements')),
      body: RefreshIndicator(
        onRefresh: () async => ref.invalidate(billsProvider),
        child: billsAsync.when(
          data: (rows) => rows.isEmpty
              ? ListView(children: const [Padding(padding: EdgeInsets.all(24), child: Text('No billing statements available yet.'))])
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: rows.length,
                  itemBuilder: (context, i) {
                    final b = rows[i];
                    final charged = chargedForMonth(b.billingDate);
                    return Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Text(DateFormat.yMMMM().format(b.billingDate), style: Theme.of(context).textTheme.titleMedium),
                                IconButton(
                                  icon: const Icon(Icons.download_outlined),
                                  tooltip: 'Download Bill (PDF)',
                                  onPressed: () => _downloadBill(b, charged),
                                ),
                              ],
                            ),
                            if (charged != null) ...[
                              Text('Amount Charged: ${_currency.format(charged)}', style: const TextStyle(fontWeight: FontWeight.w700, color: Colors.green)),
                              const SizedBox(height: 4),
                            ],
                            const Divider(),
                            _row('Cumulative kWh Import', b.cumulativeKwhImport),
                            _row('Cumulative kVAh Import', b.cumulativeKvahImport),
                            _row('Cumulative kWh Export', b.cumulativeKwhExport),
                            _row('Cumulative kVAh Export', b.cumulativeKvahExport),
                            _row('Average Power Factor', b.averagePowerFactor),
                            _row('Maximum Demand (kW)', b.maximumDemandKw),
                            _row('Maximum Demand (kVA)', b.maximumDemandKva),
                          ],
                        ),
                      ),
                    );
                  },
                ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Padding(padding: const EdgeInsets.all(24), child: Text(e.toString(), style: const TextStyle(color: Colors.red)))),
        ),
      ),
    );
  }

  Widget _row(String label, double value) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 2),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [Text(label), Text(value.toStringAsFixed(3), style: const TextStyle(fontWeight: FontWeight.w600))],
        ),
      );

  Future<void> _downloadBill(BillingProfileRow b, double? charged) async {
    await shareStatementPdf(
      title: 'Bill - ${DateFormat.yMMMM().format(b.billingDate)}',
      subtitle: 'Metered usage snapshot for the billing period',
      columnHeaders: const ['Parameter', 'Value'],
      rows: [
        ['Cumulative kWh Import', b.cumulativeKwhImport.toStringAsFixed(3)],
        ['Cumulative kVAh Import', b.cumulativeKvahImport.toStringAsFixed(3)],
        ['Cumulative kWh Export', b.cumulativeKwhExport.toStringAsFixed(3)],
        ['Cumulative kVAh Export', b.cumulativeKvahExport.toStringAsFixed(3)],
        ['Average Power Factor', b.averagePowerFactor.toStringAsFixed(3)],
        ['Maximum Demand (kW)', b.maximumDemandKw.toStringAsFixed(3)],
        ['Maximum Demand (kVA)', b.maximumDemandKva.toStringAsFixed(3)],
        if (charged != null) ['Amount Charged', _currency.format(charged)],
      ],
      footerNote: charged != null
          ? 'Amount Charged is the real sum of prepaid wallet consumption debits for this month.'
          : 'This is a metered-usage summary, not a tariff invoice — this project has no billing/tariff-calculation engine.',
    );
  }
}
