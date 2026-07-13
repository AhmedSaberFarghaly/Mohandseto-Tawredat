import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class ThemeModeController extends Notifier<ThemeMode> {
  @override
  ThemeMode build() => ThemeMode.system;
  void set(String value) => state = switch (value) {
    'light' => ThemeMode.light,
    'dark' => ThemeMode.dark,
    _ => ThemeMode.system,
  };
}

class LocaleController extends Notifier<Locale> {
  @override
  Locale build() => const Locale('ar');
  void set(String value) => state = Locale(value == 'en' ? 'en' : 'ar');
}

final themeModeProvider = NotifierProvider<ThemeModeController, ThemeMode>(
  ThemeModeController.new,
);
final localeProvider = NotifierProvider<LocaleController, Locale>(
  LocaleController.new,
);
