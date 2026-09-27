import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/mpin.dart';

/// Two-step MPIN creation: enter a 4-digit PIN, then confirm it matches. Reached from Settings.
class MpinSetupScreen extends ConsumerStatefulWidget {
  const MpinSetupScreen({super.key});

  @override
  ConsumerState<MpinSetupScreen> createState() => _MpinSetupScreenState();
}

class _MpinSetupScreenState extends ConsumerState<MpinSetupScreen> {
  String _first = '';
  String _entry = '';
  bool _confirming = false;
  String? _error;

  void _onDigit(String d) {
    if (_entry.length >= 4) return;
    setState(() {
      _entry += d;
      _error = null;
    });
    if (_entry.length == 4) _onComplete();
  }

  void _onBackspace() {
    if (_entry.isEmpty) return;
    setState(() => _entry = _entry.substring(0, _entry.length - 1));
  }

  Future<void> _onComplete() async {
    if (!_confirming) {
      setState(() {
        _first = _entry;
        _entry = '';
        _confirming = true;
      });
      return;
    }
    if (_entry != _first) {
      setState(() {
        _error = 'PINs did not match. Try again.';
        _entry = '';
        _confirming = false;
        _first = '';
      });
      return;
    }
    await ref.read(mpinControllerProvider.notifier).setPin(_entry);
    if (mounted) Navigator.of(context).pop(true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Set MPIN')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            const SizedBox(height: 24),
            Text(_confirming ? 'Confirm your 4-digit MPIN' : 'Create a 4-digit MPIN', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            const Text('Used to quickly unlock this app on this device without re-entering your account details.',
                textAlign: TextAlign.center, style: TextStyle(color: AppColors.cardMuted, fontSize: 12)),
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
            _Keypad(onDigit: _onDigit, onBackspace: _onBackspace),
          ],
        ),
      ),
    );
  }
}

class _Keypad extends StatelessWidget {
  final ValueChanged<String> onDigit;
  final VoidCallback onBackspace;
  const _Keypad({required this.onDigit, required this.onBackspace});

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
