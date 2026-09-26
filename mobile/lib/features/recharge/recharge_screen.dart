import 'dart:math';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../dashboard/dashboard_screen.dart';

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');
const _presetAmounts = [100, 200, 500, 1000, 2000];

class WalletTransactionRow {
  final String type;
  final double amount;
  final double balanceAfter;
  final String reference;
  final DateTime createdAtUtc;

  WalletTransactionRow.fromJson(Map<String, dynamic> j)
      : type = j['type'] as String,
        amount = (j['amount'] as num).toDouble(),
        balanceAfter = (j['balanceAfter'] as num).toDouble(),
        reference = j['reference'] as String,
        createdAtUtc = DateTime.parse(j['createdAtUtc'] as String);
}

final transactionsProvider = FutureProvider.autoDispose<List<WalletTransactionRow>>((ref) async {
  final client = ref.watch(apiClientProvider);
  final session = ref.watch(sessionProvider)!;
  try {
    final response = await apiCall(() => client.dio.get('/api/v1/prepaid/accounts/${session.consumerId}/transactions'));
    return (response.data as List).map((e) => WalletTransactionRow.fromJson(e as Map<String, dynamic>)).toList();
  } on Exception catch (e) {
    if (e.toString().contains('not found')) return [];
    rethrow;
  }
});

/// Recharge, wallet history and payment status — everything is real backend data
/// (PrepaidController). Note: no real payment gateway is integrated in this project, so
/// "recharge" here credits the wallet directly once submitted, rather than routing through a
/// payment gateway + separate verification step as the full spec calls for. That gateway
/// integration is the actual missing piece, not something this screen fakes.
class RechargeScreen extends ConsumerStatefulWidget {
  const RechargeScreen({super.key});

  @override
  ConsumerState<RechargeScreen> createState() => _RechargeScreenState();
}

class _RechargeScreenState extends ConsumerState<RechargeScreen> {
  int? _selectedPreset = 500;
  final _customController = TextEditingController();
  bool _submitting = false;
  String? _error;
  String? _success;

  double? get _amount {
    if (_customController.text.trim().isNotEmpty) {
      return double.tryParse(_customController.text.trim());
    }
    return _selectedPreset?.toDouble();
  }

  Future<void> _recharge() async {
    final amount = _amount;
    if (amount == null || amount <= 0) {
      setState(() => _error = 'Enter a valid amount.');
      return;
    }
    setState(() {
      _submitting = true;
      _error = null;
      _success = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      final session = ref.read(sessionProvider)!;
      final reference = 'MOBILE-${session.consumerId}-${DateTime.now().millisecondsSinceEpoch}-${Random().nextInt(9999)}';
      await apiCall(() => client.dio.post('/api/v1/prepaid/recharge', data: {
            'customerId': session.consumerId,
            'amount': amount,
            'reference': reference,
          }));
      setState(() => _success = 'Recharge of ${_currency.format(amount)} completed successfully.');
      ref.invalidate(summaryProvider);
      ref.invalidate(transactionsProvider);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final txAsync = ref.watch(transactionsProvider);
    final summaryAsync = ref.watch(summaryProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Recharge & Wallet')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          summaryAsync.when(
            data: (s) => HeroGradientCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('WALLET BALANCE', style: TextStyle(fontSize: 12, letterSpacing: 1, color: Colors.white70)),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.18), borderRadius: BorderRadius.circular(20)),
                        child: Text(s.connectionType, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    s.walletBalance != null ? _currency.format(s.walletBalance) : 'No prepaid account',
                    style: const TextStyle(fontSize: 30, fontWeight: FontWeight.w700),
                  ),
                  if (s.lastRechargeAmount != null) ...[
                    const SizedBox(height: 10),
                    Container(height: 1, color: Colors.white24),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        const Icon(Icons.history, size: 16, color: Colors.white70),
                        const SizedBox(width: 6),
                        Text(
                          'Last Recharge: ${_currency.format(s.lastRechargeAmount)} on ${DateFormat.yMMMd().format(s.lastRechargeAtUtc!.toLocal())}',
                          style: const TextStyle(fontSize: 12, color: Colors.white70),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
            loading: () => const SizedBox(height: 120, child: Center(child: CircularProgressIndicator(color: Colors.white))),
            error: (e, _) => const SizedBox.shrink(),
          ),
          const SizedBox(height: 20),
          Text('Select Amount', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: _presetAmounts
                .map((a) => ChoiceChip(
                      label: Text(_currency.format(a)),
                      selected: _selectedPreset == a && _customController.text.isEmpty,
                      onSelected: (_) => setState(() {
                        _selectedPreset = a;
                        _customController.clear();
                      }),
                    ))
                .toList(),
          ),
          const SizedBox(height: 16),
          TextField(
            controller: _customController,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(labelText: 'Or enter custom amount', border: OutlineInputBorder(), prefixText: '₹ '),
            onChanged: (_) => setState(() {}),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: const TextStyle(color: Colors.red)),
          ],
          if (_success != null) ...[
            const SizedBox(height: 12),
            Text(_success!, style: const TextStyle(color: Colors.green)),
          ],
          const SizedBox(height: 16),
          FilledButton(
            onPressed: _submitting ? null : _recharge,
            child: _submitting ? const CircularProgressIndicator(strokeWidth: 2) : const Text('RECHARGE NOW'),
          ),
          const SizedBox(height: 24),
          Text('Recent Transactions', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          txAsync.when(
            data: (rows) => rows.isEmpty
                ? const Padding(padding: EdgeInsets.all(16), child: Text('No transactions yet.'))
                : Column(
                    children: rows
                        .map((t) => Card(
                              child: ListTile(
                                leading: Icon(t.amount >= 0 ? Icons.arrow_downward : Icons.arrow_upward,
                                    color: t.amount >= 0 ? Colors.green : Colors.red),
                                title: Text(t.type),
                                subtitle: Text('${DateFormat.yMMMd().add_jm().format(t.createdAtUtc.toLocal())}\nRef: ${t.reference}'),
                                isThreeLine: true,
                                trailing: Text(_currency.format(t.amount.abs()),
                                    style: TextStyle(color: t.amount >= 0 ? Colors.green : Colors.red, fontWeight: FontWeight.bold)),
                              ),
                            ))
                        .toList(),
                  ),
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => Text(e.toString(), style: const TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );
  }
}
