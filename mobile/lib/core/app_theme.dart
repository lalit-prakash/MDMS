import 'package:flutter/material.dart';

/// Design tokens per the "Smart Electricity App Color Palette & Typography Specification":
/// blue as the primary brand color, teal/green for energy/positive states, amber/red reserved for
/// warnings/alerts only. Every screen should build on these tokens rather than picking its own
/// colors, so the app reads as one system.
///
/// Font: the spec recommends Inter with Roboto as an accepted alternative "if native Android
/// consistency is preferred" -- this app uses Roboto rather than bundling/fetching a new font
/// family, since Inter would need real font assets this project doesn't have (no network font
/// fetch, to keep the app fully offline-capable).
class AppColors {
  // Primary blue
  static const primary = Color(0xFF155EEF);
  static const primaryPressed = Color(0xFF004EEB);
  static const primaryDark = Color(0xFF0B3FA8);
  static const primaryLight = Color(0xFFE8F1FF);
  static const primaryVeryLight = Color(0xFFF5F8FF);

  // Energy teal
  static const secondary = Color(0xFF0F9D8A);
  static const secondaryDark = Color(0xFF087F71);
  static const secondaryLight = Color(0xFFDDF7F2);

  // Status colors
  static const success = Color(0xFF12B76A);
  static const successLight = Color(0xFFECFDF3);
  static const successDark = Color(0xFF027A48);
  static const warning = Color(0xFFF79009);
  static const warningLight = Color(0xFFFFFAEB);
  static const warningDark = Color(0xFFB54708);
  static const critical = Color(0xFFF04438);
  static const criticalLight = Color(0xFFFEF3F2);
  static const criticalDark = Color(0xFFB42318);
  static const info = Color(0xFF2E90FA);
  static const infoLight = Color(0xFFEFF8FF);
  static const infoDark = Color(0xFF175CD3);

  // Neutrals
  static const textPrimary = Color(0xFF101828);
  static const textSecondary = Color(0xFF475467);
  static const textTertiary = Color(0xFF667085);
  static const textDisabled = Color(0xFF98A2B3);
  static const border = Color(0xFFD0D5DD);
  static const borderLight = Color(0xFFEAECF0);
  static const scaffoldBg = Color(0xFFF8F9FC);
  static const surface = Color(0xFFFFFFFF);
  static const secondaryBackground = Color(0xFFF2F4F7);

  /// Legacy alias kept so existing call sites (many screens) keep reading a sensible, muted
  /// low-emphasis color without a mass rename -- equivalent to textTertiary.
  static const cardMuted = textTertiary;

  /// Legacy alias -- the app's single "brand accent" used across icons/buttons before this
  /// palette existed; now simply primary.
  static const accent = primary;

  static const heroGradientStart = primaryDark;
  static const heroGradientEnd = primary;
}

/// Type scale per the specification (px sizes, Material's `fontSize` is logical pixels so these
/// map directly). Roboto is the "native Android consistency" alternative the spec names for
/// Inter.
class AppTypography {
  static const _family = 'Roboto';

  static const display = TextStyle(fontFamily: _family, fontSize: 32, fontWeight: FontWeight.w700, height: 40 / 32, color: AppColors.textPrimary);
  static const h1 = TextStyle(fontFamily: _family, fontSize: 28, fontWeight: FontWeight.w700, height: 36 / 28, color: AppColors.textPrimary);
  static const h2 = TextStyle(fontFamily: _family, fontSize: 24, fontWeight: FontWeight.w700, height: 32 / 24, color: AppColors.textPrimary);
  static const h3 = TextStyle(fontFamily: _family, fontSize: 20, fontWeight: FontWeight.w600, height: 28 / 20, color: AppColors.textPrimary);
  static const h4 = TextStyle(fontFamily: _family, fontSize: 18, fontWeight: FontWeight.w600, height: 24 / 18, color: AppColors.textPrimary);
  static const bodyLarge = TextStyle(fontFamily: _family, fontSize: 16, fontWeight: FontWeight.w400, height: 24 / 16, color: AppColors.textPrimary);
  static const body = TextStyle(fontFamily: _family, fontSize: 14, fontWeight: FontWeight.w400, height: 20 / 14, color: AppColors.textPrimary);
  static const bodyMedium = TextStyle(fontFamily: _family, fontSize: 14, fontWeight: FontWeight.w500, height: 20 / 14, color: AppColors.textPrimary);
  static const caption = TextStyle(fontFamily: _family, fontSize: 12, fontWeight: FontWeight.w400, height: 16 / 12, color: AppColors.textSecondary);
  static const overline = TextStyle(fontFamily: _family, fontSize: 11, fontWeight: FontWeight.w600, height: 16 / 11, letterSpacing: 0.5, color: AppColors.textSecondary);
}

/// 8-point spacing system from the specification.
class AppSpacing {
  static const xs = 4.0;
  static const sm = 8.0;
  static const md = 12.0;
  static const lg = 16.0;
  static const xl = 24.0;
  static const xxl = 32.0;
  static const xxxl = 40.0;
}

/// Corner radii from the specification.
class AppRadius {
  static const input = 10.0;
  static const button = 12.0;
  static const cardSmall = 12.0;
  static const card = 16.0;
}

ThemeData buildAppTheme() {
  final base = ThemeData(
    useMaterial3: true,
    colorSchemeSeed: AppColors.primary,
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
    colorSchemeSeed: AppColors.primary,
    scaffoldBackgroundColor: const Color(0xFF101828),
    fontFamily: 'Roboto',
  );
  return base.copyWith(
    textTheme: _textTheme(base.textTheme, dark: true),
    appBarTheme: const AppBarTheme(backgroundColor: Color(0xFF101828), foregroundColor: Colors.white, elevation: 0, centerTitle: false),
    cardTheme: CardThemeData(
      elevation: 0,
      color: const Color(0xFF1D2939),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppRadius.card)),
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
    ),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: const Color(0xFF1D2939),
      indicatorColor: AppColors.primary.withValues(alpha: 0.2),
      elevation: 3,
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: const Color(0xFF1D2939),
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(AppRadius.input), borderSide: BorderSide.none),
      contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: 14),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: AppColors.primary,
        minimumSize: const Size.fromHeight(48),
        textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppRadius.button)),
      ),
    ),
  );
}

TextTheme _textTheme(TextTheme base, {bool dark = false}) {
  final onDark = dark ? Colors.white : null;
  final onDarkSecondary = dark ? Colors.white70 : null;
  return base.copyWith(
    displayLarge: AppTypography.display.copyWith(color: onDark),
    displayMedium: AppTypography.display.copyWith(color: onDark),
    headlineLarge: AppTypography.h1.copyWith(color: onDark),
    headlineMedium: AppTypography.h2.copyWith(color: onDark),
    headlineSmall: AppTypography.h2.copyWith(color: onDark),
    titleLarge: AppTypography.h3.copyWith(color: onDark),
    titleMedium: AppTypography.h4.copyWith(color: onDark),
    bodyLarge: AppTypography.bodyLarge.copyWith(color: onDark),
    bodyMedium: AppTypography.body.copyWith(color: onDark),
    bodySmall: AppTypography.caption.copyWith(color: onDarkSecondary),
    labelLarge: const TextStyle(fontFamily: 'Roboto', fontSize: 16, fontWeight: FontWeight.w600, height: 24 / 16),
    labelMedium: AppTypography.bodyMedium.copyWith(color: onDark),
    labelSmall: AppTypography.overline.copyWith(color: onDarkSecondary),
  );
}

ThemeData _applyShared(ThemeData base) {
  return base.copyWith(
    textTheme: _textTheme(base.textTheme),
    appBarTheme: const AppBarTheme(
      backgroundColor: AppColors.scaffoldBg,
      foregroundColor: AppColors.textPrimary,
      elevation: 0,
      centerTitle: false,
      titleTextStyle: TextStyle(fontFamily: 'Roboto', fontSize: 18, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      color: AppColors.surface,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppRadius.card), side: const BorderSide(color: AppColors.borderLight)),
      margin: const EdgeInsets.only(bottom: AppSpacing.md),
    ),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: AppColors.surface,
      indicatorColor: AppColors.primary.withValues(alpha: 0.12),
      elevation: 3,
      labelTextStyle: WidgetStateProperty.resolveWith(
        (states) => TextStyle(
          fontSize: 12,
          fontWeight: states.contains(WidgetState.selected) ? FontWeight.w600 : FontWeight.w500,
          color: states.contains(WidgetState.selected) ? AppColors.primary : AppColors.textTertiary,
        ),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: AppColors.surface,
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(AppRadius.input), borderSide: const BorderSide(color: AppColors.border)),
      enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(AppRadius.input), borderSide: const BorderSide(color: AppColors.border)),
      focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(AppRadius.input), borderSide: const BorderSide(color: AppColors.primary, width: 1.5)),
      errorBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(AppRadius.input), borderSide: const BorderSide(color: AppColors.critical)),
      labelStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500, color: AppColors.textSecondary),
      contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg, vertical: 14),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: AppColors.primary,
        minimumSize: const Size.fromHeight(48),
        textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppRadius.button)),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size.fromHeight(48),
        side: const BorderSide(color: AppColors.border),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppRadius.button)),
        foregroundColor: AppColors.primary,
        textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(foregroundColor: AppColors.primary, textStyle: const TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
    ),
  );
}

/// The recurring "hero" gradient card used for the primary figure at the top of Home, Wallet,
/// Consumption and Meter screens — a trustworthy blue gradient per the palette (primaryDark to
/// primary), never the arbitrary purple/blue of earlier iterations.
class HeroGradientCard extends StatelessWidget {
  final Widget child;
  const HeroGradientCard({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppSpacing.xl),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(AppRadius.card),
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.heroGradientStart, AppColors.heroGradientEnd],
        ),
      ),
      child: DefaultTextStyle(
        style: const TextStyle(color: Colors.white, fontFamily: 'Roboto'),
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
  final Color? iconColor;

  const StatTile({super.key, required this.label, required this.value, this.subtitle, this.subtitleColor, required this.icon, this.iconColor});

  @override
  Widget build(BuildContext context) {
    final color = iconColor ?? AppColors.primary;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 32,
              height: 32,
              decoration: BoxDecoration(color: color.withValues(alpha: 0.12), shape: BoxShape.circle),
              child: Icon(icon, color: color, size: 17),
            ),
            const SizedBox(height: 8),
            Text(label, style: AppTypography.caption, maxLines: 1, overflow: TextOverflow.ellipsis),
            const SizedBox(height: 2),
            Text(value, style: const TextStyle(fontFamily: 'Roboto', fontSize: 17, fontWeight: FontWeight.w700, color: AppColors.textPrimary), maxLines: 1, overflow: TextOverflow.ellipsis),
            if (subtitle != null) ...[
              const SizedBox(height: 2),
              Text(subtitle!, style: TextStyle(fontFamily: 'Roboto', fontSize: 10, color: subtitleColor ?? AppColors.textTertiary), maxLines: 1, overflow: TextOverflow.ellipsis),
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
      child: Text(text, style: TextStyle(fontFamily: 'Roboto', color: color, fontSize: 11, fontWeight: FontWeight.w700)),
    );
  }
}
