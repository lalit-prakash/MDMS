import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';

/// Real password change for a consumer who has registered a password (see RegisterScreen) --
/// requires the current password to be correct (verified server-side), never just accepted.
class ChangePasswordScreen extends ConsumerStatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  ConsumerState<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends ConsumerState<ChangePasswordScreen> {
  final _currentController = TextEditingController();
  final _newController = TextEditingController();
  final _confirmController = TextEditingController();
  bool _loading = false;
  String? _error;
  String? _success;

  Future<void> _submit() async {
    if (_newController.text != _confirmController.text) {
      setState(() => _error = 'New passwords do not match.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
      _success = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      await apiCall(() => client.dio.post('/api/v1/consumer-auth/password/change', data: {
            'currentPassword': _currentController.text,
            'newPassword': _newController.text,
          }));
      setState(() => _success = 'Password changed successfully.');
      _currentController.clear();
      _newController.clear();
      _confirmController.clear();
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Change Password')),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: ListView(
          children: [
            const Text('Only available if you have registered for password login. Use "Forgot password?" on the login screen if you have not set one yet, or don\'t remember your current password.',
                style: TextStyle(fontSize: 12, color: AppColors.textTertiary)),
            const SizedBox(height: 20),
            TextField(
              controller: _currentController,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Current Password', prefixIcon: Icon(Icons.lock_outline)),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _newController,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'New Password', prefixIcon: Icon(Icons.lock_outline)),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _confirmController,
              obscureText: true,
              decoration: const InputDecoration(labelText: 'Confirm New Password', prefixIcon: Icon(Icons.lock_outline)),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: const TextStyle(color: AppColors.critical)),
            ],
            if (_success != null) ...[
              const SizedBox(height: 12),
              Text(_success!, style: const TextStyle(color: AppColors.success)),
            ],
            const SizedBox(height: 20),
            FilledButton(
              onPressed: _loading ? null : _submit,
              child: _loading ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2)) : const Text('Change Password'),
            ),
          ],
        ),
      ),
    );
  }
}
