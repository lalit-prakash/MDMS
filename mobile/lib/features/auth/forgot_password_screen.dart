import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';

/// Reset a forgotten password: verify identity (account number + registered mobile number),
/// request a verification code, then set a new password. Same disclosed development-placeholder
/// OTP as RegisterScreen until a real email/SMS provider is configured.
class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _accountController = TextEditingController();
  final _mobileController = TextEditingController();
  final _otpController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _otpRequested = false;
  bool _loading = false;
  bool _done = false;
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
      final response = await apiCall(() => client.dio.post('/api/v1/consumer-auth/password/forgot/request-otp', data: {
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

  Future<void> _resetPassword() async {
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
      await apiCall(() => client.dio.post('/api/v1/consumer-auth/password/forgot/complete', data: {
            'accountNumber': _accountController.text.trim(),
            'mobileNumber': _mobileController.text.trim(),
            'otp': _otpController.text.trim(),
            'newPassword': _passwordController.text,
          }));
      setState(() => _done = true);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_done) {
      return Scaffold(
        appBar: AppBar(title: const Text('Reset Password')),
        body: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.check_circle_outline, size: 56, color: AppColors.success),
              const SizedBox(height: 16),
              const Text('Password reset successfully.', textAlign: TextAlign.center, style: TextStyle(fontWeight: FontWeight.w600)),
              const SizedBox(height: 20),
              FilledButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Back to Login')),
            ],
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Forgot Password')),
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
                decoration: const InputDecoration(labelText: 'New Password', prefixIcon: Icon(Icons.lock_outline)),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _confirmController,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Confirm New Password', prefixIcon: Icon(Icons.lock_outline)),
              ),
              const SizedBox(height: 20),
              FilledButton(
                onPressed: _loading ? null : _resetPassword,
                child: _loading ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Reset Password'),
              ),
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
