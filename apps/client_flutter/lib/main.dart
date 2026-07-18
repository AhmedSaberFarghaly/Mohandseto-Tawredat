import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/api/api_client.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'core/theme/app_tokens.dart';
import 'core/theme/appearance_controller.dart';

void main() {
  runApp(const ProviderScope(child: MohandsetoApp()));
}

class MohandsetoApp extends ConsumerWidget {
  const MohandsetoApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => MaterialApp.router(
    title: 'مهندسيتو توريدات',
    debugShowCheckedModeBanner: false,
    theme: AppTheme.light(),
    darkTheme: AppTheme.dark(),
    themeMode: ref.watch(themeModeProvider),
    routerConfig: appRouter,
    locale: ref.watch(localeProvider),
    supportedLocales: const [Locale('ar'), Locale('en')],
    localizationsDelegates: const [
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    builder: (context, child) {
      final media = MediaQuery.of(context);
      final accessibilityScale = media.textScaler.scale(16) / 16;
      // The reference UI uses a compact commerce scale. Respect accessibility
      // without multiplying every explicit screen size a second time.
      final effectiveScale = accessibilityScale.clamp(1.0, 1.25).toDouble();
      return MediaQuery(
        data: media.copyWith(textScaler: TextScaler.linear(effectiveScale)),
        child: Stack(
          children: [
            child!,
            // global progress bar for add/edit/delete operations
            PositionedDirectional(
              top: media.padding.top,
              start: 0,
              end: 0,
              child: ValueListenableBuilder<int>(
                valueListenable: mutationInFlight,
                builder: (context, pending, _) => AnimatedSwitcher(
                  duration: const Duration(milliseconds: 200),
                  child: pending > 0
                      ? const LinearProgressIndicator(
                          key: ValueKey('global-progress'),
                          minHeight: 3,
                          backgroundColor: Colors.transparent,
                          color: AppColors.primary,
                        )
                      : const SizedBox.shrink(
                          key: ValueKey('global-progress-idle'),
                        ),
                ),
              ),
            ),
          ],
        ),
      );
    },
  );
}
