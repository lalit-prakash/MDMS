import 'dart:math';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/providers.dart';

final _currency = NumberFormat.currency(locale: 'en_IN', symbol: '₹');
const _presetAmounts = [100, 200, 500, 1000, 2000];

/// Recharge without signing in — the account number is verified against its own registered
/// mobile number (same real check ConsumerAuthController.Login uses) before crediting, via
/// POST /consumer-auth/guest-recharge. No session is created or required.
class GuestRechargeScreen extends ConsumerStatefulWidget {
  const GuestRechargeScreen({super.key});

  @override
  ConsumerState<GuestRechargeScreen> createState() => _GuestRechargeScreenState();
}

class _GuestRechargeScreenState extends ConsumerState<GuestRechargeScreen> {
  final _accountController = TextEditingController();
  final _mobileController = TextEditingController();
  int? _selectedPreset = 500;
  final _customController = TextEditingController();
  bool _submitting = false;
  String? _error;
  String? _success;

  double? get _amount {
    if (_customController.text.trim().isNotEmpty) return double.tryParse(_customController.text.trim());
    return _selectedPreset?.toDouble();
  }

  Future<void> _submit() async {
    final amount = _amount;
    if (_accountController.text.trim().isEmpty || _mobileController.text.trim().isEmpty) {
      setState(() => _error = 'Enter your account number and registered mobile number.');
      return;
    }
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
      final reference = 'GUEST-${_accountController.text.trim()}-${DateTime.now().millisecondsSinceEpoch}-${Random().nextInt(9999)}';
      final response = await apiCall(() => client.dio.post('/api/v1/consumer-auth/guest-recharge', data: {
            'accountNumber': _accountController.text.trim(),
            'mobileNumber': _mobileController.text.trim(),
            'amount': amount,
            'reference': reference,
          }));
      final data = response.data as Map<String, dynamic>;
      setState(() => _success =
          'Recharge of ${_currency.format(amount)} completed. New balance: ${_currency.format((data['balanceAfter'] as num).toDouble())}');
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Guest Recharge')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(
            controller: _accountController,
            decoration: const InputDecoration(labelText: 'Consumer / Account Number', prefixIcon: Icon(Icons.badge_outlined)),
          ),
          const SizedBox(height: 16),
          TextField(
            controller: _mobileController,
            keyboardType: TextInputType.phone,
            decoration: const InputDecoration(labelText: 'Registered Mobile Number', prefixIcon: Icon(Icons.phone_outlined)),
          ),
          const SizedBox(height: 24),
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
            decoration: const InputDecoration(labelText: 'Or enter custom amount', prefixText: '₹ '),
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
          const SizedBox(height: 24),
          FilledButton(
            onPressed: _submitting ? null : _submit,
            child: _submitting ? const CircularProgressIndicator(strokeWidth: 2) : const Text('RECHARGE'),
          ),
        ],
      ),
    );
  }
}
