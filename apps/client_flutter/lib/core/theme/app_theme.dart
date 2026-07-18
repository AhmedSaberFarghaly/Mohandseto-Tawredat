import 'package:flutter/material.dart';

import 'app_tokens.dart';

abstract final class AppTheme {
  static const fontFamily = 'IBMPlexSansArabic';

  static ThemeData light() {
    final base = ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: AppColors.primary,
        primary: AppColors.primary,
        error: AppColors.error,
        surface: AppColors.card,
      ),
      scaffoldBackgroundColor: AppColors.background,
    );

    // Typographic scale: clear hierarchy between titles, body and captions.
    final textTheme = base.textTheme
        .copyWith(
          headlineMedium: const TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.w700,
            height: 1.35,
          ),
          headlineSmall: const TextStyle(
            fontSize: 19,
            fontWeight: FontWeight.w700,
            height: 1.35,
          ),
          titleLarge: const TextStyle(
            fontSize: 17,
            fontWeight: FontWeight.w700,
            height: 1.35,
          ),
          titleMedium: const TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.w700,
            height: 1.4,
          ),
          titleSmall: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            height: 1.4,
          ),
          bodyLarge: const TextStyle(
            fontSize: 14.5,
            fontWeight: FontWeight.w400,
            height: 1.55,
          ),
          bodyMedium: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w400,
            height: 1.55,
          ),
          bodySmall: const TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w400,
            height: 1.5,
            color: AppColors.gray500,
          ),
          labelLarge: const TextStyle(
            fontSize: 13.5,
            fontWeight: FontWeight.w600,
            height: 1.35,
          ),
          labelMedium: const TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w600,
            height: 1.35,
          ),
          labelSmall: const TextStyle(
            fontSize: 10.5,
            fontWeight: FontWeight.w600,
            height: 1.3,
            letterSpacing: 0,
          ),
        )
        .apply(
          fontFamily: fontFamily,
          bodyColor: AppColors.gray800,
          displayColor: AppColors.gray900,
        );

    return base.copyWith(
      textTheme: textTheme,
      pageTransitionsTheme: const PageTransitionsTheme(
        builders: {
          TargetPlatform.android: CupertinoPageTransitionsBuilder(),
          TargetPlatform.iOS: CupertinoPageTransitionsBuilder(),
          TargetPlatform.windows: CupertinoPageTransitionsBuilder(),
        },
      ),
      splashFactory: InkSparkle.splashFactory,
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.card,
        foregroundColor: AppColors.gray900,
        elevation: 0,
        scrolledUnderElevation: 0,
        surfaceTintColor: Colors.transparent,
        centerTitle: true,
        toolbarHeight: 58,
        titleTextStyle: TextStyle(
          fontFamily: fontFamily,
          fontSize: 17,
          fontWeight: FontWeight.w700,
          color: AppColors.gray900,
        ),
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: AppColors.gray800,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.md),
        ),
      ),
      cardTheme: CardThemeData(
        color: AppColors.card,
        elevation: .35,
        shadowColor: const Color(0x10102846),
        surfaceTintColor: Colors.transparent,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.lg),
          side: const BorderSide(color: AppColors.gray150),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: Colors.white,
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: const TextStyle(
            fontFamily: fontFamily,
            fontSize: 14,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.gray800,
          minimumSize: const Size.fromHeight(50),
          side: const BorderSide(color: AppColors.gray200),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
          textStyle: const TextStyle(
            fontFamily: fontFamily,
            fontSize: 13,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primary,
          textStyle: const TextStyle(
            fontFamily: fontFamily,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.gray25,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: AppColors.gray200),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: AppColors.gray200),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(14),
          borderSide: const BorderSide(color: AppColors.error),
        ),
      ),
      chipTheme: base.chipTheme.copyWith(
        backgroundColor: AppColors.card,
        selectedColor: AppColors.primaryTint,
        side: const BorderSide(color: AppColors.gray200),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.pill),
        ),
        labelStyle: const TextStyle(
          fontFamily: fontFamily,
          fontWeight: FontWeight.w600,
          color: AppColors.gray700,
        ),
      ),
      dividerTheme: const DividerThemeData(
        color: AppColors.gray150,
        thickness: 1,
        space: 1,
      ),
      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        showDragHandle: true,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
        ),
      ),
      dialogTheme: DialogThemeData(
        backgroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.xxl),
        ),
      ),
      listTileTheme: const ListTileThemeData(
        iconColor: AppColors.gray600,
        textColor: AppColors.gray800,
        contentPadding: EdgeInsets.symmetric(horizontal: 14, vertical: 3),
      ),
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: AppColors.card,
        selectedItemColor: AppColors.primary,
        unselectedItemColor: AppColors.gray400,
        type: BottomNavigationBarType.fixed,
      ),
    );
  }

  static ThemeData dark() {
    final base = ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      colorScheme: ColorScheme.fromSeed(
        seedColor: AppColors.primaryLight,
        brightness: Brightness.dark,
      ),
    );
    final darkTextTheme = light().textTheme.apply(
      bodyColor: const Color(0xFFE7ECF3),
      displayColor: Colors.white,
    );
    return base.copyWith(
      textTheme: darkTextTheme,
      scaffoldBackgroundColor: const Color(0xFF0B1624),
      appBarTheme: const AppBarTheme(
        backgroundColor: Color(0xFF101F30),
        centerTitle: true,
        elevation: 0,
      ),
      cardTheme: CardThemeData(
        color: const Color(0xFF132437),
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.lg),
          side: const BorderSide(color: Color(0xFF24374B)),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.primaryLight,
          minimumSize: const Size.fromHeight(52),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(AppRadius.md),
          ),
          textStyle: const TextStyle(
            fontFamily: fontFamily,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: const Color(0xFF132437),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.md),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppRadius.md),
          borderSide: const BorderSide(color: Color(0xFF30465D)),
        ),
      ),
    );
  }
}
