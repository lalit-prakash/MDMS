import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../dashboard/dashboard_screen.dart';

/// Register for password login: verify identity (account number + registered mobile number --
/// the same real check Login already uses), request a verification code, then set a password.
/// No real email/SMS provider is configured in this project yet, so the code is a disclosed
/// development placeholder (see ConsumerAuthController) rather than a faked delivery — the note
/// returned by the backend is shown verbatim on screen, not hidden.
class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _accountController = TextEditingController();
  final _mobileController = TextEditingController();
  final _otpController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _otpRequested = false;
  bool _loading = false;
  String? _error;
  String? _otpNote;

  Future<void> _requestOtp() async {
    if (_accountController.text.trim().isEmpty || _mobileController.text.trim().isEmpty) {
      setState(() => _error = 'Enter your account number and registered mobile number.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      final response = await apiCall(() => client.dio.post('/api/v1/consumer-auth/register/request-otp', data: {
            'accountNumber': _accountController.text.trim(),
            'mobileNumber': _mobileController.text.trim(),
          }));
      final data = response.data as Map<String, dynamic>;
      setState(() {
        _otpRequested = true;
        _otpNote = data['otpDeliveryNote'] as String?;
      });
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _completeRegistration() async {
    if (_passwordController.text != _confirmController.text) {
      setState(() => _error = 'Passwords do not match.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      final response = await apiCall(() => client.dio.post('/api/v1/consumer-auth/register/complete', data: {
            'accountNumber': _accountController.text.trim(),
            'mobileNumber': _mobileController.text.trim(),
            'otp': _otpController.text.trim(),
            'password': _passwordController.text,
          }));
      final data = response.data as Map<String, dynamic>;
      await ref.read(sessionListProvider.notifier).signIn(ConsumerSession(
            accessToken: data['accessToken'] as String,
            consumerId: data['consumerId'] as String,
            name: data['name'] as String,
            accountNumber: data['accountNumber'] as String,
            mobileNumber: _mobileController.text.trim(),
          ));
      if (mounted) {
        Navigator.of(context).pushAndRemoveUntil(MaterialPageRoute(builder: (_) => const DashboardScreen()), (route) => false);
      }
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Register')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: ListView(
          children: [
            TextField(
              controller: _accountController,
              enabled: !_otpRequested,
              decoration: const InputDecoration(labelText: 'Consumer / Account Number', prefixIcon: Icon(Icons.badge_outlined)),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _mobileController,
              enabled: !_otpRequested,
              keyboardType: TextInputType.phone,
              decoration: const InputDecoration(labelText: 'Registered Mobile Number', prefixIcon: Icon(Icons.phone_outlined)),
            ),
            if (!_otpRequested) ...[
              const SizedBox(height: 20),
              FilledButton(
                onPressed: _loading ? null : _requestOtp,
                child: _loading ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Send Verification Code'),
              ),
            ] else ...[
              const SizedBox(height: 16),
              if (_otpNote != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(color: AppColors.warningLight, borderRadius: BorderRadius.circular(10)),
                  child: Row(
                    children: [
                      const Icon(Icons.info_outline, size: 18, color: AppColors.warningDark),
                      const SizedBox(width: 8),
                      Expanded(child: Text(_otpNote!, style: const TextStyle(fontSize: 12, color: AppColors.warningDark))),
                    ],
                  ),
                ),
              const SizedBox(height: 12),
              TextField(
                controller: _otpController,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'Verification Code', prefixIcon: Icon(Icons.pin_outlined)),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _passwordController,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Create Password', prefixIcon: Icon(Icons.lock_outline)),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _confirmController,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Confirm Password', prefixIcon: Icon(Icons.lock_outline)),
              ),
              const SizedBox(height: 20),
              FilledButton(
                onPressed: _loading ? null : _completeRegistration,
                child: _loading ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Complete Registration'),
              ),
              TextButton(onPressed: () => setState(() => _otpRequested = false), child: const Text('Change account number or mobile number')),
            ],
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: const TextStyle(color: AppColors.critical)),
            ],
          ],
        ),
      ),
    );
  }
}
