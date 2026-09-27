import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/mpin.dart';
import '../../core/session.dart';
import '../auth/login_screen.dart';
import '../dashboard/dashboard_screen.dart';

/// Shown at app start (after splash) when the device already has a real signed-in session and an
/// MPIN was previously set. Verifies the entered PIN against the stored hash before letting the
/// user back into that real session -- not a login by itself, just a lock screen in front of one.
class MpinUnlockScreen extends ConsumerStatefulWidget {
  const MpinUnlockScreen({super.key});

  @override
  ConsumerState<MpinUnlockScreen> createState() => _MpinUnlockScreenState();
}

class _MpinUnlockScreenState extends ConsumerState<MpinUnlockScreen> {
  String _entry = '';
  String? _error;
  bool _checking = false;

  void _onDigit(String d) {
    if (_entry.length >= 4 || _checking) return;
    setState(() {
      _entry += d;
      _error = null;
    });
    if (_entry.length == 4) _verify();
  }

  void _onBackspace() {
    if (_entry.isEmpty) return;
    setState(() => _entry = _entry.substring(0, _entry.length - 1));
  }

  Future<void> _verify() async {
    setState(() => _checking = true);
    final ok = await ref.read(mpinControllerProvider.notifier).verify(_entry);
    if (!mounted) return;
    if (ok) {
      Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => const DashboardScreen()));
    } else {
      setState(() {
        _error = 'Incorrect MPIN. Try again.';
        _entry = '';
        _checking = false;
      });
    }
  }

  Future<void> _useAccountLoginInstead() async {
    await ref.read(sessionListProvider.notifier).signOut();
    await ref.read(mpinControllerProvider.notifier).clear();
    if (mounted) {
      Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => const LoginScreen()));
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(sessionProvider);
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            children: [
              const SizedBox(height: 48),
              const Icon(Icons.lock_outline, size: 40, color: AppColors.accent),
              const SizedBox(height: 16),
              Text('Welcome back${session != null ? ', ${session.name}' : ''}', style: Theme.of(context).textTheme.titleLarge, textAlign: TextAlign.center),
              const SizedBox(height: 4),
              const Text('Enter your MPIN to continue', style: TextStyle(color: AppColors.cardMuted)),
              const SizedBox(height: 32),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var i = 0; i < 4; i++)
                    Container(
                      margin: const EdgeInsets.symmetric(horizontal: 8),
                      width: 16,
                      height: 16,
                      decoration: BoxDecoration(shape: BoxShape.circle, color: i < _entry.length ? AppColors.accent : AppColors.cardMuted.withValues(alpha: 0.25)),
                    ),
                ],
              ),
              if (_error != null) ...[
                const SizedBox(height: 16),
                Text(_error!, style: const TextStyle(color: AppColors.critical)),
              ],
              const Spacer(),
              _MpinKeypad(onDigit: _onDigit, onBackspace: _onBackspace),
              const SizedBox(height: 12),
              TextButton(onPressed: _useAccountLoginInstead, child: const Text('Use account number instead')),
            ],
          ),
        ),
      ),
    );
  }
}

class _MpinKeypad extends StatelessWidget {
  final ValueChanged<String> onDigit;
  final VoidCallback onBackspace;
  const _MpinKeypad({required this.onDigit, required this.onBackspace});

  @override
  Widget build(BuildContext context) {
    Widget key(String label, {VoidCallback? onTap, Widget? child}) => Expanded(
          child: InkWell(
            onTap: onTap ?? (label.isEmpty ? null : () => onDigit(label)),
            borderRadius: BorderRadius.circular(40),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 14),
              child: Center(child: child ?? Text(label, style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w600))),
            ),
          ),
        );

    return Column(
      children: [
        Row(children: [key('1'), key('2'), key('3')]),
        Row(children: [key('4'), key('5'), key('6')]),
        Row(children: [key('7'), key('8'), key('9')]),
        Row(children: [
          key('', onTap: () {}),
          key('0'),
          key('', onTap: onBackspace, child: const Icon(Icons.backspace_outlined, size: 20)),
        ]),
      ],
    );
  }
}
