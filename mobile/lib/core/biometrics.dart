import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:local_auth/local_auth.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Real device biometric unlock (fingerprint/face, via the local_auth plugin's own OS-level
/// prompt) as an alternative to typing the MPIN -- gates the same already-signed-in session, no
/// separate credential of our own to fake. "Enabled" is a device-local preference; actual
/// availability still depends on the device having biometrics enrolled at all.
class BiometricService {
  static final _auth = LocalAuthentication();

  static Future<bool> isDeviceSupported() async {
    try {
      final supported = await _auth.isDeviceSupported();
      final canCheck = await _auth.canCheckBiometrics;
      return supported && canCheck;
    } catch (_) {
      return false;
    }
  }

  static Future<bool> authenticate() async {
    try {
      return await _auth.authenticate(
        localizedReason: 'Authenticate to unlock MDMS Consumer',
        options: const AuthenticationOptions(biometricOnly: true, stickyAuth: true),
      );
    } catch (_) {
      return false;
    }
  }
}

class BiometricPreferenceController extends StateNotifier<bool> {
  BiometricPreferenceController() : super(false) {
    _restore();
  }

  Future<void> _restore() async {
    final prefs = await SharedPreferences.getInstance();
    state = prefs.getBool('biometricEnabled') ?? false;
  }

  Future<void> setEnabled(bool v) async {
    state = v;
    (await SharedPreferences.getInstance()).setBool('biometricEnabled', v);
  }
}

final biometricEnabledProvider = StateNotifierProvider<BiometricPreferenceController, bool>((ref) => BiometricPreferenceController());
