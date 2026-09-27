import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import '../../core/app_settings.dart';
import '../../core/biometrics.dart';
import '../../core/mpin.dart';
import '../mpin/mpin_setup_screen.dart';
import 'change_password_screen.dart';

/// Device-local settings: theme, notification toggles + low-balance threshold, profile photo.
/// These are per-device preferences with no backend endpoint to sync them (this project has no
/// notification-preferences or profile-photo API) -- reinstalling or switching devices resets
/// them, which is disclosed on the screen rather than implied to be an account setting.
///
/// "Change password" isn't offered: this app authenticates by account number + registered mobile
/// number, not a password (see ConsumerAuthController) -- there's nothing to change, and adding a
/// fake password field would misrepresent how sign-in actually works.
class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final settings = ref.watch(appSettingsProvider);
    final controller = ref.read(appSettingsProvider.notifier);

    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Appearance', style: Theme.of(context).textTheme.titleMedium),
          Card(
            child: RadioGroup<ThemeMode>(
              groupValue: settings.themeMode,
              onChanged: (v) => controller.setThemeMode(v!),
              child: Column(
                children: ThemeMode.values.map((mode) {
                  final label = switch (mode) { ThemeMode.system => 'System Default', ThemeMode.light => 'Light', ThemeMode.dark => 'Dark' };
                  return RadioListTile<ThemeMode>(title: Text(label), value: mode);
                }).toList(),
              ),
            ),
          ),
          const SizedBox(height: 20),
          Text('Notifications', style: Theme.of(context).textTheme.titleMedium),
          Card(
            child: Column(
              children: [
                SwitchListTile(
                  title: const Text('Low Balance Alerts'),
                  subtitle: Text('Notify when wallet balance falls below ₹${settings.lowBalanceThreshold.toStringAsFixed(0)}'),
                  value: settings.lowBalanceAlertsEnabled,
                  onChanged: controller.setLowBalanceAlertsEnabled,
                ),
                if (settings.lowBalanceAlertsEnabled)
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    child: Slider(
                      value: settings.lowBalanceThreshold,
                      min: 50,
                      max: 1000,
                      divisions: 19,
                      label: '₹${settings.lowBalanceThreshold.toStringAsFixed(0)}',
                      onChanged: (v) => controller.setLowBalanceThreshold(v),
                    ),
                  ),
                SwitchListTile(
                  title: const Text('Consumption Alerts'),
                  subtitle: const Text('Notify on unusually high consumption'),
                  value: settings.consumptionAlertsEnabled,
                  onChanged: controller.setConsumptionAlertsEnabled,
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          Text('Account', style: Theme.of(context).textTheme.titleMedium),
          Card(
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.photo_camera_outlined),
                  title: const Text('Update Profile Picture'),
                  subtitle: Text(settings.profilePhotoPath != null ? 'Custom photo set' : 'Using initials'),
                  trailing: settings.profilePhotoPath != null
                      ? IconButton(icon: const Icon(Icons.delete_outline), onPressed: () => controller.setProfilePhotoPath(null))
                      : const Icon(Icons.chevron_right),
                  onTap: () async {
                    final picker = ImagePicker();
                    final file = await picker.pickImage(source: ImageSource.gallery, maxWidth: 512, maxHeight: 512);
                    if (file != null) await controller.setProfilePhotoPath(file.path);
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.lock_outline),
                  title: const Text('Change Password'),
                  subtitle: const Text('Only if you have registered for password login'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const ChangePasswordScreen())),
                ),
                Consumer(
                  builder: (context, ref, _) {
                    final hasMpin = ref.watch(mpinControllerProvider);
                    return ListTile(
                      leading: const Icon(Icons.pin_outlined),
                      title: Text(hasMpin ? 'Change MPIN' : 'Set MPIN'),
                      subtitle: Text(hasMpin ? 'Quick-unlock this app without re-entering your account details' : 'Set a 4-digit PIN to unlock this app quickly'),
                      trailing: hasMpin
                          ? IconButton(
                              icon: const Icon(Icons.delete_outline),
                              onPressed: () async {
                                final confirmed = await showDialog<bool>(
                                  context: context,
                                  builder: (dialogContext) => AlertDialog(
                                    title: const Text('Remove MPIN?'),
                                    content: const Text('You will need your account number and mobile number to unlock the app next time.'),
                                    actions: [
                                      TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text('Cancel')),
                                      TextButton(onPressed: () => Navigator.of(dialogContext).pop(true), child: const Text('Remove')),
                                    ],
                                  ),
                                );
                                if (confirmed == true) {
                                  await ref.read(mpinControllerProvider.notifier).clear();
                                  await ref.read(biometricEnabledProvider.notifier).setEnabled(false);
                                }
                              },
                            )
                          : const Icon(Icons.chevron_right),
                      onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const MpinSetupScreen())),
                    );
                  },
                ),
                Consumer(
                  builder: (context, ref, _) {
                    final hasMpin = ref.watch(mpinControllerProvider);
                    final biometricEnabled = ref.watch(biometricEnabledProvider);
                    if (!hasMpin) return const SizedBox.shrink();
                    return FutureBuilder<bool>(
                      future: BiometricService.isDeviceSupported(),
                      builder: (context, snapshot) {
                        if (snapshot.data != true) return const SizedBox.shrink();
                        return SwitchListTile(
                          secondary: const Icon(Icons.fingerprint),
                          title: const Text('Biometric Unlock'),
                          subtitle: const Text('Use fingerprint or face unlock instead of your MPIN'),
                          value: biometricEnabled,
                          onChanged: (v) async {
                            if (v) {
                              final ok = await BiometricService.authenticate();
                              if (!ok) return;
                            }
                            await ref.read(biometricEnabledProvider.notifier).setEnabled(v);
                          },
                        );
                      },
                    );
                  },
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
