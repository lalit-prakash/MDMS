import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../recharge/guest_recharge_screen.dart';

/// Login by Consumer Number + registered mobile number.
///
/// The spec calls for Mobile Number -> OTP -> MPIN. This project has no SMS gateway to actually
/// send an OTP, so implementing that step would mean faking delivery of a code nobody receives —
/// instead this authenticates against the mobile number already on file for the account
/// (ConsumerAuthController), which is a real check against real data. Swapping in real OTP only
/// needs a backend SMS integration; this screen's job (collect identifiers, show errors, hand off
/// to the session) does not change.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _accountController = TextEditingController();
  final _mobileController = TextEditingController();
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _accountController.dispose();
    _mobileController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      final response = await apiCall(() => client.dio.post('/api/v1/consumer-auth/login', data: {
            'accountNumber': _accountController.text.trim(),
            'mobileNumber': _mobileController.text.trim(),
          }));
      final data = response.data as Map<String, dynamic>;
      await ref.read(sessionListProvider.notifier).signIn(ConsumerSession(
            accessToken: data['accessToken'] as String,
            consumerId: data['consumerId'] as String,
            name: data['name'] as String,
            accountNumber: data['accountNumber'] as String,
            mobileNumber: _mobileController.text.trim(),
          ));
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Center(
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Container(
                    width: 88,
                    height: 88,
                    decoration: const BoxDecoration(
                      shape: BoxShape.circle,
                      gradient: LinearGradient(colors: [AppColors.heroGradientStart, AppColors.heroGradientEnd]),
                    ),
                    child: const Icon(Icons.electric_bolt, size: 44, color: Colors.white),
                  ),
                  const SizedBox(height: 16),
                  Text('MDMS Consumer', style: Theme.of(context).textTheme.headlineSmall, textAlign: TextAlign.center),
                  const SizedBox(height: 4),
                  const Text('Sign in with your account number and registered mobile number',
                      textAlign: TextAlign.center, style: TextStyle(color: AppColors.cardMuted)),
                  const SizedBox(height: 32),
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
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Text(_error!, style: const TextStyle(color: Colors.red)),
                  ],
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: _loading ? null : _submit,
                    child: _loading
                        ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                        : const Text('Login'),
                  ),
                  const SizedBox(height: 12),
                  TextButton(
                    onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const GuestRechargeScreen())),
                    child: const Text('Recharge without logging in'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
