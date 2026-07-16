import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_tokens.dart';
import '../../core/theme/app_theme.dart';
import '../../core/api/engagement_repository.dart';

class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends ConsumerState<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _checkRuntime();
  }

  Future<void> _checkRuntime() async {
    final minimumDelay = Future<void>.delayed(
      const Duration(milliseconds: 1600),
    );
    MobileAppConfigModel? config;
    try {
      config = await ref
          .read(engagementRepositoryProvider)
          .appConfig()
          .timeout(const Duration(seconds: 1));
    } catch (_) {}
    await minimumDelay;
    if (!mounted) return;
    if (config?.maintenance == true) {
      context.go('/system/maintenance', extra: config);
      return;
    }
    if (config != null && versionLess('0.2.0', config.minimum)) {
      context.go('/system/update-required', extra: config);
      return;
    }
    if (config != null && versionLess('0.2.0', config.latest)) {
      context.go('/system/update-available', extra: config);
      return;
    }
    context.go('/login');
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: Container(
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF0D1F3F), AppColors.splashNavy, Color(0xFF0A1A33)],
        ),
      ),
      child: SafeArea(
        child: Column(
          children: [
            const Spacer(flex: 3),
            Container(
              width: 88,
              height: 88,
              decoration: BoxDecoration(
                color: AppColors.primaryLight,
                borderRadius: BorderRadius.circular(AppRadius.xl),
              ),
              child: const Icon(
                Icons.inventory_2_rounded,
                color: Colors.white,
                size: 44,
              ),
            ),
            const SizedBox(height: AppSpacing.xxl),
            Text(
              'مهندسيتو توريدات',
              style: const TextStyle(
                fontFamily: AppTheme.fontFamily,
                color: Colors.white,
                fontSize: 28,
                fontWeight: FontWeight.w700,
              ),
            ),
            Text(
              'Mohandseto Tawredat',
              style: const TextStyle(
                fontFamily: AppTheme.fontFamily,
                color: Colors.white60,
                fontSize: 12,
              ),
            ),
            const SizedBox(height: AppSpacing.lg),
            Text(
              'كل احتياجات شركتك... في مكان واحد',
              style: TextStyle(
                fontFamily: AppTheme.fontFamily,
                color: Colors.white.withValues(alpha: .85),
                fontSize: 14,
              ),
            ),
            const Spacer(flex: 3),
            const SizedBox(
              width: 96,
              child: LinearProgressIndicator(
                minHeight: 4,
                backgroundColor: Colors.white12,
                color: AppColors.primaryLight,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            Text(
              'v0.2.0',
              style: const TextStyle(
                fontFamily: AppTheme.fontFamily,
                color: Colors.white38,
                fontSize: 10,
              ),
            ),
            const SizedBox(height: AppSpacing.xxl),
          ],
        ),
      ),
    ),
  );
}

bool versionLess(String current, String target) {
  final a = current.split('.').map(int.tryParse).map((v) => v ?? 0).toList();
  final b = target.split('.').map(int.tryParse).map((v) => v ?? 0).toList();
  for (var i = 0; i < 3; i++) {
    final av = i < a.length ? a[i] : 0, bv = i < b.length ? b[i] : 0;
    if (av != bv) return av < bv;
  }
  return false;
}
