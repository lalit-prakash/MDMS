import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_settings.dart';
import '../../core/session.dart';
import '../auth/login_screen.dart';
import '../dashboard/dashboard_screen.dart';
import '../settings/settings_screen.dart';
import 'linked_accounts_screen.dart';

/// Consumer identity (real data via /customers/{id}/summary), linked accounts/organisations,
/// settings, and sign-out.
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summaryAsync = ref.watch(summaryProvider);
    final session = ref.watch(sessionProvider);
    final settings = ref.watch(appSettingsProvider);
    final linkedCount = ref.watch(sessionListProvider).length;

    return Scaffold(
      appBar: AppBar(title: const Text('Profile')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Builder(builder: (context) {
            final hasPhoto = settings.profilePhotoPath != null && File(settings.profilePhotoPath!).existsSync();
            return Center(
              child: CircleAvatar(
                radius: 32,
                backgroundImage: hasPhoto ? FileImage(File(settings.profilePhotoPath!)) : null,
                child: hasPhoto
                    ? null
                    : Text(session != null && session.name.isNotEmpty ? session.name.substring(0, 1) : '?', style: const TextStyle(fontSize: 28)),
              ),
            );
          }),
          const SizedBox(height: 12),
          Center(child: Text(session?.name ?? '', style: Theme.of(context).textTheme.titleLarge)),
          Center(child: Text(session?.organisationName ?? '', style: Theme.of(context).textTheme.bodySmall)),
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
          const SizedBox(height: 16),
          Card(
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.account_balance_wallet_outlined),
                  title: const Text('Add Account'),
                  subtitle: Text('Link another connection ($linkedCount linked)'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const LinkedAccountsScreen())),
                ),
                ListTile(
                  leading: const Icon(Icons.corporate_fare_outlined),
                  title: const Text('Add Organisation'),
                  subtitle: const Text('Link an account from another organisation'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const LinkedAccountsScreen())),
                ),
                ListTile(
                  leading: const Icon(Icons.settings_outlined),
                  title: const Text('Settings'),
                  subtitle: const Text('Theme, notifications, profile photo'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const SettingsScreen())),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: ListTile(
              leading: const Icon(Icons.logout, color: Colors.red),
              title: const Text('Logout', style: TextStyle(color: Colors.red)),
              onTap: () async {
                final confirmed = await showDialog<bool>(
                  context: context,
                  builder: (dialogContext) => AlertDialog(
                    title: const Text('Log out?'),
                    content: linkedCount > 1
                        ? Text('This will sign you out of all $linkedCount linked accounts on this device.')
                        : const Text('You will need your account number and mobile number to sign in again.'),
                    actions: [
                      TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text('Cancel')),
                      TextButton(onPressed: () => Navigator.of(dialogContext).pop(true), child: const Text('Logout')),
                    ],
                  ),
                );
                if (confirmed != true) return;
                await ref.read(sessionListProvider.notifier).signOut();
                if (context.mounted) {
                  Navigator.of(context).pushAndRemoveUntil(MaterialPageRoute(builder: (_) => const LoginScreen()), (route) => false);
                }
              },
            ),
          ),
        ],
      ),
    );
  }
}
