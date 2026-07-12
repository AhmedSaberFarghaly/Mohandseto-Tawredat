import 'package:flutter/material.dart';

/// Design tokens — mirrors packages/design_tokens/tokens.json (source of truth).
abstract final class AppColors {
  // brand
  static const primary = Color(0xFF023BAA);
  static const primaryDark = Color(0xFF012E86);
  static const primaryLight = Color(0xFF2F62C9);
  static const primaryTint = Color(0xFFEFF3FC);
  static const splashNavy = Color(0xFF10255C);

  // neutrals
  static const gray0 = Color(0xFFFFFFFF);
  static const gray25 = Color(0xFFFBFCFE);
  static const gray50 = Color(0xFFF7F9FC);
  static const gray100 = Color(0xFFF3F6F9);
  static const gray150 = Color(0xFFF0F2F5);
  static const gray200 = Color(0xFFE4E7EC);
  static const gray300 = Color(0xFFD0D5DD);
  static const gray400 = Color(0xFF98A2B3);
  static const gray500 = Color(0xFF667085);
  static const gray600 = Color(0xFF475467);
  static const gray700 = Color(0xFF344054);
  static const gray800 = Color(0xFF1D2939);
  static const gray900 = Color(0xFF101828);

  // semantic
  static const success = Color(0xFF067647);
  static const successTint = Color(0xFFECFDF3);
  static const warning = Color(0xFFF79009);
  static const warningTint = Color(0xFFFFF4E8);
  static const error = Color(0xFFD92D20);
  static const errorTint = Color(0xFFFEF3F2);
  static const info = Color(0xFF175CD3);
  static const infoTint = Color(0xFFEFF8FF);

  // client app surfaces
  static const background = Color(0xFFF8F9FB);
  static const card = Color(0xFFFFFFFF);
}

abstract final class AppSpacing {
  static const xs = 4.0;
  static const sm = 8.0;
  static const md = 12.0;
  static const lg = 16.0;
  static const xl = 20.0;
  static const xxl = 24.0;
  static const xxxl = 32.0;
}

abstract final class AppRadius {
  static const sm = 8.0;
  static const md = 12.0;
  static const lg = 16.0;
  static const xl = 20.0;
  static const pill = 999.0;
}
