import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_theme.dart';
import '../../core/providers.dart';
import '../../core/session.dart';

/// "Add Account" / "Add Organisation": a consumer can link another connection billed to the same
/// registered mobile number -- including one in a different organisation (tenant), since each
/// linked account's own login token already carries its own tenant claim. Backed by real
/// GET /consumer-auth/accounts?mobileNumber=... (searches across every organisation) and the
/// same POST /consumer-auth/login used for the initial sign-in.
class LinkedAccountsScreen extends ConsumerWidget {
  const LinkedAccountsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final sessions = ref.watch(sessionListProvider);
    final activeIndex = ref.read(sessionListProvider.notifier).activeIndex;

    return Scaffold(
      appBar: AppBar(title: const Text('Linked Accounts')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const _AddAccountScreen())),
        icon: const Icon(Icons.add),
        label: const Text('Add Account'),
      ),
      body: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: sessions.length,
        itemBuilder: (context, i) {
          final s = sessions[i];
          final isActive = i == activeIndex;
          return Card(
            child: ListTile(
              leading: CircleAvatar(backgroundColor: isActive ? AppColors.accent : AppColors.cardMuted, child: Text(s.name.isNotEmpty ? s.name[0].toUpperCase() : '?', style: const TextStyle(color: Colors.white))),
              title: Text(s.name, style: const TextStyle(fontWeight: FontWeight.w600)),
              subtitle: Text('${s.accountNumber}\n${s.organisationName}'),
              isThreeLine: true,
              trailing: isActive
                  ? const Chip(label: Text('Active'), backgroundColor: AppColors.accent, labelStyle: TextStyle(color: Colors.white, fontSize: 11))
                  : Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        TextButton(onPressed: () => ref.read(sessionListProvider.notifier).switchTo(i), child: const Text('Switch')),
                        IconButton(icon: const Icon(Icons.close, size: 18), onPressed: () => ref.read(sessionListProvider.notifier).removeAccount(i)),
                      ],
                    ),
            ),
          );
        },
      ),
    );
  }
}

class _AddAccountScreen extends ConsumerStatefulWidget {
  const _AddAccountScreen();

  @override
  ConsumerState<_AddAccountScreen> createState() => _AddAccountScreenState();
}

class _AddAccountScreenState extends ConsumerState<_AddAccountScreen> {
  final _accountController = TextEditingController();
  bool _submitting = false;
  String? _error;

  Future<void> _submit() async {
    setState(() {
      _submitting = true;
      _error = null;
    });
    try {
      final client = ref.read(apiClientProvider);
      final currentMobile = ref.read(sessionProvider)!.mobileNumber;

      final accountsResponse = await apiCall(() => client.dio.get('/api/v1/consumer-auth/accounts', queryParameters: {'mobileNumber': currentMobile}));
      final accounts = (accountsResponse.data as List).cast<Map<String, dynamic>>();
      final target = accounts.firstWhere(
        (a) => (a['accountNumber'] as String).toLowerCase() == _accountController.text.trim().toLowerCase(),
        orElse: () => {},
      );
      if (target.isEmpty) {
        setState(() => _error = 'No account with that number is registered to your mobile number ($currentMobile).');
        return;
      }

      final loginResponse = await apiCall(() => client.dio.post('/api/v1/consumer-auth/login', data: {
            'accountNumber': target['accountNumber'],
            'mobileNumber': currentMobile,
          }));
      final data = loginResponse.data as Map<String, dynamic>;
      await ref.read(sessionListProvider.notifier).addAccount(ConsumerSession(
            accessToken: data['accessToken'] as String,
            consumerId: data['consumerId'] as String,
            name: data['name'] as String,
            accountNumber: data['accountNumber'] as String,
            mobileNumber: currentMobile,
            organisationName: target['tenantName'] as String,
          ));
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Add Account / Organisation')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text('Enter another account number registered to your same mobile number. If it belongs to a different organisation, it will be added as a new organisation too.'),
            const SizedBox(height: 16),
            TextField(
              controller: _accountController,
              decoration: const InputDecoration(labelText: 'Account Number', prefixIcon: Icon(Icons.badge_outlined)),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: const TextStyle(color: Colors.red)),
            ],
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _submitting ? null : _submit,
              child: _submitting ? const CircularProgressIndicator(strokeWidth: 2) : const Text('ADD'),
            ),
          ],
        ),
      ),
    );
  }
}
