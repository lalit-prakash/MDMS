import 'package:flutter/material.dart';

/// Shared visual language for the whole app, modelled on the reference fintech-dashboard mockups:
/// a soft lavender-tinted scaffold background, a deep-indigo gradient "hero" card for the primary
/// balance/summary figure on every module (Wallet, Consumption, Meter, MD), and consistently
/// rounded (20px) white cards for everything else. Every screen should build on these tokens
/// rather than picking its own colors, so the app reads as one system.
class AppColors {
  static const scaffoldBg = Color(0xFFF3F1FA);
  static const heroGradientStart = Color(0xFF2A2A72);
  static const heroGradientEnd = Color(0xFF1E88E5);
  static const accent = Color(0xFF2A2A72);
  static const success = Color(0xFF16A34A);
  static const warning = Color(0xFFF59E0B);
  static const critical = Color(0xFFDC2626);
  static const cardMuted = Color(0xFF8B8B9E);
}

ThemeData buildAppTheme() {
  final base = ThemeData(
    useMaterial3: true,
    colorSchemeSeed: AppColors.accent,
    scaffoldBackgroundColor: AppColors.scaffoldBg,
    fontFamily: 'Roboto',
  );

  return _applyShared(base);
}

/// Dark variant — same card/button language, seeded for a dark scaffold instead of the light
/// lavender one. Applied when the user picks Dark (or System resolves to dark) in Settings.
ThemeData buildAppDarkTheme() {
  final base = ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    colorSchemeSeed: AppColors.heroGradientEnd,
    scaffoldBackgroundColor: const Color(0xFF121218),
    fontFamily: 'Roboto',
  );
  return base.copyWith(
    appBarTheme: const AppBarTheme(backgroundColor: Color(0xFF121218), foregroundColor: Colors.white, elevation: 0, centerTitle: false),
    cardTheme: CardThemeData(
      elevation: 0,
      color: const Color(0xFF1E1E27),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      margin: const EdgeInsets.only(bottom: 12),
    ),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: const Color(0xFF1E1E27),
      indicatorColor: AppColors.heroGradientEnd.withValues(alpha: 0.2),
      elevation: 3,
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: const Color(0xFF1E1E27),
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: AppColors.heroGradientEnd,
        minimumSize: const Size.fromHeight(52),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
    ),
  );
}

ThemeData _applyShared(ThemeData base) {

  return base.copyWith(
    appBarTheme: const AppBarTheme(
      backgroundColor: AppColors.scaffoldBg,
      foregroundColor: Colors.black87,
      elevation: 0,
      centerTitle: false,
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      color: Colors.white,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      margin: const EdgeInsets.only(bottom: 12),
    ),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: Colors.white,
      indicatorColor: AppColors.accent.withValues(alpha: 0.12),
      elevation: 3,
      labelTextStyle: WidgetStateProperty.resolveWith(
        (states) => TextStyle(
          fontSize: 11,
          fontWeight: states.contains(WidgetState.selected) ? FontWeight.w600 : FontWeight.w400,
          color: states.contains(WidgetState.selected) ? AppColors.accent : AppColors.cardMuted,
        ),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: AppColors.accent,
        minimumSize: const Size.fromHeight(52),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size.fromHeight(48),
        side: const BorderSide(color: Color(0xFFE3E1F0)),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        foregroundColor: Colors.black87,
      ),
    ),
  );
}

/// The recurring "hero" gradient card used for the primary figure at the top of Home, Wallet,
/// Consumption and Meter screens — matches the reference mockups' balance-card treatment.
class HeroGradientCard extends StatelessWidget {
  final Widget child;
  const HeroGradientCard({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(24),
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.heroGradientStart, AppColors.heroGradientEnd],
        ),
      ),
      child: DefaultTextStyle(
        style: const TextStyle(color: Colors.white),
        child: child,
      ),
    );
  }
}

/// A compact stat tile used for the Home screen's Today's Consumption / Daily Peak / MD Status
/// row of cards.
class StatTile extends StatelessWidget {
  final String label;
  final String value;
  final String? subtitle;
  final Color? subtitleColor;
  final IconData icon;

  const StatTile({super.key, required this.label, required this.value, this.subtitle, this.subtitleColor, required this.icon});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: AppColors.accent, size: 18),
            const SizedBox(height: 6),
            Text(label, style: const TextStyle(fontSize: 12, color: AppColors.cardMuted), maxLines: 1, overflow: TextOverflow.ellipsis),
            const SizedBox(height: 2),
            Text(value, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w700), maxLines: 1, overflow: TextOverflow.ellipsis),
            if (subtitle != null) ...[
              const SizedBox(height: 2),
              Text(subtitle!, style: TextStyle(fontSize: 10, color: subtitleColor ?? AppColors.cardMuted), maxLines: 1, overflow: TextOverflow.ellipsis),
            ],
          ],
        ),
      ),
    );
  }
}

/// A status pill (NORMAL / WARNING / BREACH etc.) — never color alone, always paired with text.
class StatusPill extends StatelessWidget {
  final String text;
  final Color color;
  const StatusPill({super.key, required this.text, required this.color});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: color.withValues(alpha: 0.12), borderRadius: BorderRadius.circular(20)),
      child: Text(text, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w700)),
    );
  }
}
