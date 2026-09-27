import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';
import '../dashboard/dashboard_screen.dart';
import '../recharge/guest_recharge_screen.dart';
import 'register_screen.dart';
import 'forgot_password_screen.dart';

enum _LoginMode { mobile, password }

/// Login by Consumer Number + registered mobile number (real, no gateway needed), or by
/// Consumer Number + password once the consumer has registered a password (see RegisterScreen).
/// The spec calls for Mobile Number -> OTP -> MPIN; this project has no SMS/email gateway to
/// actually deliver an OTP, so the mobile-number mode authenticates against the mobile number
/// already on file for the account (ConsumerAuthController) instead of faking delivery — real,
/// not simulated. Registration/forgot-password use a disclosed development-placeholder OTP until
/// a real email/SMS provider is configured; see ConsumerAuthController's own doc comment.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _accountController = TextEditingController();
  final _mobileController = TextEditingController();
  final _passwordController = TextEditingController();
  _LoginMode _mode = _LoginMode.mobile;
  bool _loading = false;
  bool _obscure = true;
  String? _error;

  @override
  void dispose() {
    _accountController.dispose();
    _mobileController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      final response = await apiCall(() => client.dio.post(
            _mode == _LoginMode.mobile ? '/api/v1/consumer-auth/login' : '/api/v1/consumer-auth/password/login',
            data: _mode == _LoginMode.mobile
                ? {'accountNumber': _accountController.text.trim(), 'mobileNumber': _mobileController.text.trim()}
                : {'accountNumber': _accountController.text.trim(), 'password': _passwordController.text},
          ));
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
                  Text(
                    _mode == _LoginMode.mobile ? 'Sign in with your account number and registered mobile number' : 'Sign in with your account number and password',
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: AppColors.cardMuted),
                  ),
                  const SizedBox(height: 24),
                  _ModeToggle(value: _mode, onChanged: (m) => setState(() => _mode = m)),
                  const SizedBox(height: 20),
                  TextField(
                    controller: _accountController,
                    decoration: const InputDecoration(labelText: 'Consumer / Account Number', prefixIcon: Icon(Icons.badge_outlined)),
                  ),
                  const SizedBox(height: 16),
                  if (_mode == _LoginMode.mobile)
                    TextField(
                      controller: _mobileController,
                      keyboardType: TextInputType.phone,
                      decoration: const InputDecoration(labelText: 'Registered Mobile Number', prefixIcon: Icon(Icons.phone_outlined)),
                    )
                  else ...[
                    TextField(
                      controller: _passwordController,
                      obscureText: _obscure,
                      decoration: InputDecoration(
                        labelText: 'Password',
                        prefixIcon: const Icon(Icons.lock_outline),
                        suffixIcon: IconButton(
                          icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                          onPressed: () => setState(() => _obscure = !_obscure),
                        ),
                      ),
                    ),
                    Align(
                      alignment: Alignment.centerRight,
                      child: TextButton(
                        onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const ForgotPasswordScreen())),
                        child: const Text('Forgot password?'),
                      ),
                    ),
                  ],
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Text(_error!, style: const TextStyle(color: AppColors.critical)),
                  ],
                  const SizedBox(height: 12),
                  FilledButton(
                    onPressed: _loading ? null : _submit,
                    child: _loading
                        ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                        : const Text('Login'),
                  ),
                  const SizedBox(height: 12),
                  TextButton(
                    onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const RegisterScreen())),
                    child: const Text("New here? Register for password login"),
                  ),
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

class _ModeToggle extends StatelessWidget {
  final _LoginMode value;
  final ValueChanged<_LoginMode> onChanged;
  const _ModeToggle({required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    Widget seg(_LoginMode m, String label, IconData icon) {
      final selected = m == value;
      return Expanded(
        child: InkWell(
          borderRadius: BorderRadius.circular(10),
          onTap: () => onChanged(m),
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            padding: const EdgeInsets.symmetric(vertical: 10),
            decoration: BoxDecoration(color: selected ? AppColors.primary : Colors.transparent, borderRadius: BorderRadius.circular(10)),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(icon, size: 16, color: selected ? Colors.white : AppColors.textTertiary),
                const SizedBox(width: 6),
                Text(label, style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: selected ? Colors.white : AppColors.textTertiary)),
              ],
            ),
          ),
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.all(3),
      decoration: BoxDecoration(color: AppColors.scaffoldBg, borderRadius: BorderRadius.circular(12), border: Border.all(color: AppColors.borderLight)),
      child: Row(children: [
        seg(_LoginMode.mobile, 'Mobile Number', Icons.phone_outlined),
        seg(_LoginMode.password, 'Password', Icons.lock_outline),
      ]),
    );
  }
}
