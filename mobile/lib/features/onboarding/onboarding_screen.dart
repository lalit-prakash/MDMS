import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/app_settings.dart';
import '../../core/app_theme.dart';
import '../auth/login_screen.dart';

class _OnboardingPage {
  final IconData icon;
  final String title;
  final String description;
  const _OnboardingPage(this.icon, this.title, this.description);
}

const _pages = [
  _OnboardingPage(Icons.show_chart, 'Track Your Consumption', 'See daily, weekly and monthly usage from your real meter data, with trends and drill-down detail.'),
  _OnboardingPage(Icons.account_balance_wallet_outlined, 'Recharge Your Wallet', 'Top up your prepaid balance in seconds and keep an eye on every transaction.'),
  _OnboardingPage(Icons.notifications_active_outlined, 'Stay Informed', 'Get alerts on low balance, meter issues and complaint updates, right when they happen.'),
];

/// Shown once on first launch (tracked via AppSettings.onboardingSeen) — purely presentational
/// feature highlights, shown before the real Login screen.
class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  final _controller = PageController();
  int _page = 0;

  void _finish() async {
    await ref.read(appSettingsProvider.notifier).markOnboardingSeen();
    if (mounted) Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => const LoginScreen()));
  }

  @override
  Widget build(BuildContext context) {
    final isLast = _page == _pages.length - 1;
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Align(
              alignment: Alignment.topRight,
              child: TextButton(onPressed: _finish, child: const Text('Skip')),
            ),
            Expanded(
              child: PageView.builder(
                controller: _controller,
                itemCount: _pages.length,
                onPageChanged: (i) => setState(() => _page = i),
                itemBuilder: (context, i) {
                  final p = _pages[i];
                  return Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 32),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Container(
                          width: 140,
                          height: 140,
                          decoration: BoxDecoration(color: AppColors.accent.withValues(alpha: 0.08), shape: BoxShape.circle),
                          child: Icon(p.icon, size: 64, color: AppColors.accent),
                        ),
                        const SizedBox(height: 32),
                        Text(p.title, textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700)),
                        const SizedBox(height: 12),
                        Text(p.description, textAlign: TextAlign.center, style: const TextStyle(color: AppColors.cardMuted, fontSize: 14, height: 1.4)),
                      ],
                    ),
                  );
                },
              ),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                for (var i = 0; i < _pages.length; i++)
                  AnimatedContainer(
                    duration: const Duration(milliseconds: 200),
                    margin: const EdgeInsets.symmetric(horizontal: 4),
                    width: i == _page ? 20 : 6,
                    height: 6,
                    decoration: BoxDecoration(color: i == _page ? AppColors.accent : AppColors.cardMuted.withValues(alpha: 0.3), borderRadius: BorderRadius.circular(3)),
                  ),
              ],
            ),
            Padding(
              padding: const EdgeInsets.all(24),
              child: FilledButton(
                onPressed: () {
                  if (isLast) {
                    _finish();
                  } else {
                    _controller.nextPage(duration: const Duration(milliseconds: 250), curve: Curves.easeOut);
                  }
                },
                child: Text(isLast ? 'Get Started' : 'Next'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
