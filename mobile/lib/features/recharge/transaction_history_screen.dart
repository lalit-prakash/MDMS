import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/pdf_export.dart';
import 'recharge_screen.dart';

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');

/// Full wallet transaction history with a date-range filter and a "Download Statement" (PDF)
/// action — all real WalletTransaction rows from the same transactionsProvider the Recharge
/// screen's "Recent Transactions" preview uses, just unfiltered and with date narrowing.
class TransactionHistoryScreen extends ConsumerStatefulWidget {
  const TransactionHistoryScreen({super.key});

  @override
  ConsumerState<TransactionHistoryScreen> createState() => _TransactionHistoryScreenState();
}

class _TransactionHistoryScreenState extends ConsumerState<TransactionHistoryScreen> {
  DateTimeRange? _range;

  Future<void> _pickRange() async {
    final now = DateTime.now();
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(now.year - 3),
      lastDate: now,
      initialDateRange: _range,
    );
    if (picked != null) setState(() => _range = picked);
  }

  List<WalletTransactionRow> _filtered(List<WalletTransactionRow> rows) {
    if (_range == null) return rows;
    final start = DateTime(_range!.start.year, _range!.start.month, _range!.start.day);
    final end = DateTime(_range!.end.year, _range!.end.month, _range!.end.day, 23, 59, 59);
    return rows.where((t) => !t.createdAtUtc.toLocal().isBefore(start) && !t.createdAtUtc.toLocal().isAfter(end)).toList();
  }

  @override
  Widget build(BuildContext context) {
    final txAsync = ref.watch(transactionsProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Transaction History')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _pickRange,
                    icon: const Icon(Icons.date_range_outlined, size: 18),
                    label: Text(_range == null
                        ? 'Filter by date'
                        : '${DateFormat.MMMd().format(_range!.start)} - ${DateFormat.MMMd().format(_range!.end)}'),
                  ),
                ),
                if (_range != null)
                  IconButton(icon: const Icon(Icons.clear), tooltip: 'Clear filter', onPressed: () => setState(() => _range = null)),
              ],
            ),
          ),
          Expanded(
            child: txAsync.when(
              data: (rows) {
                final filtered = _filtered(rows);
                if (filtered.isEmpty) {
                  return const Center(child: Text('No transactions in this period.'));
                }
                return ListView.builder(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  itemCount: filtered.length,
                  itemBuilder: (context, i) {
                    final t = filtered[i];
                    return Card(
                      child: ListTile(
                        leading: Icon(t.amount >= 0 ? Icons.arrow_downward : Icons.arrow_upward,
                            color: t.amount >= 0 ? AppColors.success : AppColors.critical),
                        title: Text(t.type),
                        subtitle: Text('${DateFormat.yMMMd().add_jm().format(t.createdAtUtc.toLocal())}\nRef: ${t.reference}'),
                        isThreeLine: true,
                        trailing: Text(_currency.format(t.amount.abs()),
                            style: TextStyle(color: t.amount >= 0 ? AppColors.success : AppColors.critical, fontWeight: FontWeight.bold)),
                      ),
                    );
                  },
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => Center(child: Text(e.toString(), style: const TextStyle(color: AppColors.critical))),
            ),
          ),
        ],
      ),
      floatingActionButton: txAsync.maybeWhen(
        data: (rows) => FloatingActionButton.extended(
          onPressed: () => _downloadStatement(_filtered(rows)),
          icon: const Icon(Icons.download_outlined),
          label: const Text('Download Statement'),
        ),
        orElse: () => null,
      ),
    );
  }

  Future<void> _downloadStatement(List<WalletTransactionRow> rows) async {
    final period = _range == null
        ? 'All time'
        : '${DateFormat.yMMMd().format(_range!.start)} - ${DateFormat.yMMMd().format(_range!.end)}';
    await shareStatementPdf(
      title: 'Wallet Statement',
      subtitle: 'Period: $period',
      columnHeaders: const ['Date', 'Type', 'Reference', 'Amount', 'Balance After'],
      rows: rows
          .map((t) => [
                DateFormat.yMMMd().add_jm().format(t.createdAtUtc.toLocal()),
                t.type,
                t.reference,
                '${t.amount >= 0 ? '+' : '-'}${_currency.format(t.amount.abs())}',
                _currency.format(t.balanceAfter),
              ])
          .toList(),
      footerNote: 'Generated from real wallet transaction records.',
    );
  }
}
