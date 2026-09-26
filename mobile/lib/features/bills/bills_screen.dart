import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

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

/// Bill/Statement history from real Billing Profile snapshots (BillingProfile — the meter's own
/// monthly commercial snapshot, per its own doc comment). There is no tariff-calculation/billing
/// engine in this project, so no charges/amount-due figure is shown — only the metered facts a
/// real billing system would consume to produce the actual bill. PDF download from the full spec
/// is not implemented (no bill-document generation exists on the backend).
class BillsScreen extends ConsumerWidget {
  const BillsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final billsAsync = ref.watch(billsProvider);

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
                    return Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(DateFormat.yMMMM().format(b.billingDate), style: Theme.of(context).textTheme.titleMedium),
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
}
