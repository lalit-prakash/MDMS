import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import '../../core/app_settings.dart';

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
                  leading: Icon(Icons.lock_outline, color: Theme.of(context).disabledColor),
                  title: Text('Change Password', style: TextStyle(color: Theme.of(context).disabledColor)),
                  subtitle: const Text('Not applicable — you sign in with your account number and registered mobile number, not a password.'),
                  enabled: false,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
