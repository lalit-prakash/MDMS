import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'core/app_theme.dart';
import 'core/session.dart';
import 'features/auth/login_screen.dart';
import 'features/dashboard/dashboard_screen.dart';

void main() {
  runApp(const ProviderScope(child: MdmsConsumerApp()));
}

class MdmsConsumerApp extends ConsumerWidget {
  const MdmsConsumerApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionProvider);
    return MaterialApp(
      title: 'MDMS Consumer',
      debugShowCheckedModeBanner: false,
      theme: buildAppTheme(),
      home: session == null ? const LoginScreen() : const DashboardScreen(),
    );
  }
}
