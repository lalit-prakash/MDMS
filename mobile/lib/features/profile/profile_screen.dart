import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/session.dart';
import '../dashboard/dashboard_screen.dart';

/// Consumer identity (real data via /customers/{id}/summary) and sign-out. Editable
/// profile/notification-preference fields from the full spec are not implemented — there is no
/// backend endpoint yet for a consumer to update their own mobile/email.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summaryAsync = ref.watch(summaryProvider);
    final session = ref.watch(sessionProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          CircleAvatar(radius: 32, child: Text(session?.name.substring(0, 1) ?? '?', style: const TextStyle(fontSize: 28))),
          const SizedBox(height: 12),
          Center(child: Text(session?.name ?? '', style: Theme.of(context).textTheme.titleLarge)),
          const SizedBox(height: 24),
          Card(
            child: Column(
              children: [
                ListTile(leading: const Icon(Icons.badge_outlined), title: const Text('Account Number'), subtitle: Text(session?.accountNumber ?? '-')),
                summaryAsync.maybeWhen(
                  data: (s) => ListTile(leading: const Icon(Icons.electric_meter_outlined), title: const Text('Meter Number'), subtitle: Text(s.meterNumber ?? 'Not assigned')),
                  orElse: () => const SizedBox.shrink(),
                ),
              ],
            ),
          ),
          const SizedBox(height: 24),
          Card(
            child: ListTile(
              leading: const Icon(Icons.logout, color: Colors.red),
              title: const Text('Logout', style: TextStyle(color: Colors.red)),
              onTap: () => ref.read(sessionProvider.notifier).signOut(),
            ),
          ),
        ],
      ),
    );
  }
}
