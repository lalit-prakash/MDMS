import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'core/app_settings.dart';
import 'core/app_theme.dart';
import 'features/onboarding/splash_screen.dart';

void main() {
  runApp(const ProviderScope(child: MdmsConsumerApp()));
}

class MdmsConsumerApp extends ConsumerWidget {
  const MdmsConsumerApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final settings = ref.watch(appSettingsProvider);
    return MaterialApp(
      title: 'MDMS Consumer',
      debugShowCheckedModeBanner: false,
      theme: buildAppTheme(),
      darkTheme: buildAppDarkTheme(),
      themeMode: settings.themeMode,
      home: const SplashScreen(),
    );
  }
}
