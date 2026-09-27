import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Device-local app preferences: theme mode, notification toggles/threshold, and a locally
/// chosen profile photo path. None of this is synced to the backend (there is no
/// notification-preferences or profile-photo endpoint in this project) — every screen that
/// reads these should make clear they are per-device settings, not account settings.
class AppSettings {
  final ThemeMode themeMode;
  final bool lowBalanceAlertsEnabled;
  final double lowBalanceThreshold;
  final bool consumptionAlertsEnabled;
  final String? profilePhotoPath;
  final bool onboardingSeen;

  const AppSettings({
    this.themeMode = ThemeMode.system,
    this.lowBalanceAlertsEnabled = true,
    this.lowBalanceThreshold = 200,
    this.consumptionAlertsEnabled = true,
    this.profilePhotoPath,
    this.onboardingSeen = false,
  });

  AppSettings copyWith({
    ThemeMode? themeMode,
    bool? lowBalanceAlertsEnabled,
    double? lowBalanceThreshold,
    bool? consumptionAlertsEnabled,
    String? profilePhotoPath,
    bool? onboardingSeen,
  }) =>
      AppSettings(
        themeMode: themeMode ?? this.themeMode,
        lowBalanceAlertsEnabled: lowBalanceAlertsEnabled ?? this.lowBalanceAlertsEnabled,
        lowBalanceThreshold: lowBalanceThreshold ?? this.lowBalanceThreshold,
        consumptionAlertsEnabled: consumptionAlertsEnabled ?? this.consumptionAlertsEnabled,
        profilePhotoPath: profilePhotoPath ?? this.profilePhotoPath,
        onboardingSeen: onboardingSeen ?? this.onboardingSeen,
      );
}

class AppSettingsController extends StateNotifier<AppSettings> {
  final _readyCompleter = Completer<void>();
  Future<void> get ready => _readyCompleter.future;

  AppSettingsController() : super(const AppSettings()) {
    _restore();
  }

  Future<void> _restore() async {
    final prefs = await SharedPreferences.getInstance();
    state = AppSettings(
      themeMode: ThemeMode.values[prefs.getInt('themeMode') ?? ThemeMode.system.index],
      lowBalanceAlertsEnabled: prefs.getBool('lowBalanceAlertsEnabled') ?? true,
      lowBalanceThreshold: prefs.getDouble('lowBalanceThreshold') ?? 200,
      consumptionAlertsEnabled: prefs.getBool('consumptionAlertsEnabled') ?? true,
      profilePhotoPath: prefs.getString('profilePhotoPath'),
      onboardingSeen: prefs.getBool('onboardingSeen') ?? false,
    );
    if (!_readyCompleter.isCompleted) _readyCompleter.complete();
  }

  Future<void> markOnboardingSeen() async {
    state = state.copyWith(onboardingSeen: true);
    (await SharedPreferences.getInstance()).setBool('onboardingSeen', true);
  }

  Future<void> setThemeMode(ThemeMode mode) async {
    state = state.copyWith(themeMode: mode);
    (await SharedPreferences.getInstance()).setInt('themeMode', mode.index);
  }

  Future<void> setLowBalanceAlertsEnabled(bool v) async {
    state = state.copyWith(lowBalanceAlertsEnabled: v);
    (await SharedPreferences.getInstance()).setBool('lowBalanceAlertsEnabled', v);
  }

  Future<void> setLowBalanceThreshold(double v) async {
    state = state.copyWith(lowBalanceThreshold: v);
    (await SharedPreferences.getInstance()).setDouble('lowBalanceThreshold', v);
  }

  Future<void> setConsumptionAlertsEnabled(bool v) async {
    state = state.copyWith(consumptionAlertsEnabled: v);
    (await SharedPreferences.getInstance()).setBool('consumptionAlertsEnabled', v);
  }

  Future<void> setProfilePhotoPath(String? path) async {
    state = state.copyWith(profilePhotoPath: path);
    final prefs = await SharedPreferences.getInstance();
    if (path == null) {
      prefs.remove('profilePhotoPath');
    } else {
      prefs.setString('profilePhotoPath', path);
    }
  }
}

final appSettingsProvider = StateNotifierProvider<AppSettingsController, AppSettings>((ref) => AppSettingsController());

/// Resolves once device-local settings have finished loading -- awaited by the splash screen
/// alongside sessionReadyProvider before it decides where to route.
final settingsReadyProvider = FutureProvider<void>((ref) => ref.watch(appSettingsProvider.notifier).ready);
