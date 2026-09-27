import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_settings.dart';
import '../../core/app_theme.dart';
import '../../core/mpin.dart';
import '../../core/session.dart';
import '../auth/login_screen.dart';
import '../dashboard/dashboard_screen.dart';
import '../mpin/mpin_unlock_screen.dart';
import 'onboarding_screen.dart';

/// App entry point. Waits for the real on-device session and settings to finish loading (never a
/// fixed guess-and-hope delay) before routing to the Dashboard (already signed in), the
/// onboarding carousel (first launch), or Login.
class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends ConsumerState<SplashScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _route());
  }

  Future<void> _route() async {
    final started = DateTime.now();
    await Future.wait([
      ref.read(sessionReadyProvider.future),
      ref.read(settingsReadyProvider.future),
      ref.read(mpinReadyProvider.future),
    ]);
    // A splash that flashes for a few milliseconds reads as broken, not fast -- hold it for a
    // minimum, real perceived-loading duration rather than however long storage happened to take.
    final elapsed = DateTime.now().difference(started);
    const minDisplay = Duration(milliseconds: 900);
    if (elapsed < minDisplay) await Future.delayed(minDisplay - elapsed);
    if (!mounted) return;

    final session = ref.read(sessionProvider);
    final onboardingSeen = ref.read(appSettingsProvider).onboardingSeen;
    final hasMpin = ref.read(mpinControllerProvider);

    Widget next;
    if (session != null && hasMpin) {
      next = const MpinUnlockScreen();
    } else if (session != null) {
      next = const DashboardScreen();
    } else if (!onboardingSeen) {
      next = const OnboardingScreen();
    } else {
      next = const LoginScreen();
    }
    Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => next));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        width: double.infinity,
        height: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [AppColors.heroGradientStart, AppColors.heroGradientEnd],
          ),
        ),
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 96,
                height: 96,
                decoration: const BoxDecoration(shape: BoxShape.circle, color: Colors.white),
                child: const Icon(Icons.electric_bolt, size: 52, color: AppColors.accent),
              ),
              const SizedBox(height: 20),
              const Text('MDMS Consumer', style: TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.w700)),
              const SizedBox(height: 6),
              const Text('Smart Electricity, Simpler Everyday', style: TextStyle(color: Colors.white70, fontSize: 13)),
              const SizedBox(height: 36),
              const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white70)),
            ],
          ),
        ),
      ),
    );
  }
}
