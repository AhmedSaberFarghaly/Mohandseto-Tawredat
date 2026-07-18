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
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [Color(0xFF0754D5), AppColors.primaryDark, Color(0xFF061B43)],
        ),
      ),
      child: SafeArea(
        child: Stack(
          children: [
            Positioned(
              top: -80,
              left: -70,
              child: _SplashOrb(size: 230, opacity: .08),
            ),
            Positioned(
              bottom: 70,
              right: -85,
              child: _SplashOrb(size: 250, opacity: .06),
            ),
            Column(
              children: [
                const Spacer(flex: 3),
                Container(
                  width: 96,
                  height: 96,
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(27),
                    boxShadow: AppShadows.floating,
                  ),
                  child: const Icon(
                    Icons.storefront_rounded,
                    color: AppColors.primary,
                    size: 49,
                  ),
                ),
                const SizedBox(height: AppSpacing.xxl),
                const Text(
                  'مهندسيتو توريدات',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontFamily: AppTheme.fontFamily,
                    color: Colors.white,
                    fontSize: 27,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 5),
                const Text(
                  'Mohandseto Tawredat',
                  style: TextStyle(
                    fontFamily: AppTheme.fontFamily,
                    color: Colors.white60,
                    fontSize: 11,
                    letterSpacing: .5,
                  ),
                ),
                const SizedBox(height: AppSpacing.lg),
                const Text(
                  'تجارة أسهل لشركتك، من الطلب حتى التوصيل',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontFamily: AppTheme.fontFamily,
                    color: Colors.white,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 17),
                const Wrap(
                  alignment: WrapAlignment.center,
                  spacing: 9,
                  runSpacing: 8,
                  children: [
                    _SplashFeature(Icons.verified_outlined, 'موردون موثوقون'),
                    _SplashFeature(Icons.flash_on_outlined, 'طلب أسرع'),
                    _SplashFeature(Icons.route_outlined, 'تتبع مباشر'),
                  ],
                ),
                const Spacer(flex: 3),
                const SizedBox(
                  width: 110,
                  child: LinearProgressIndicator(
                    minHeight: 4,
                    borderRadius: BorderRadius.all(Radius.circular(8)),
                    backgroundColor: Colors.white12,
                    color: Colors.white,
                  ),
                ),
                const SizedBox(height: AppSpacing.sm),
                const Text(
                  'تجهيز تجربة الشراء الخاصة بك',
                  style: TextStyle(
                    fontFamily: AppTheme.fontFamily,
                    color: Colors.white54,
                    fontSize: 9.5,
                  ),
                ),
                const SizedBox(height: 5),
                const Text(
                  'v0.2.0',
                  style: TextStyle(
                    fontFamily: AppTheme.fontFamily,
                    color: Colors.white38,
                    fontSize: 9,
                  ),
                ),
                const SizedBox(height: AppSpacing.xxl),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

class _SplashOrb extends StatelessWidget {
  const _SplashOrb({required this.size, required this.opacity});
  final double size, opacity;

  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(
      shape: BoxShape.circle,
      color: Colors.white.withValues(alpha: opacity),
    ),
  );
}

class _SplashFeature extends StatelessWidget {
  const _SplashFeature(this.icon, this.label);
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
    decoration: BoxDecoration(
      color: Colors.white.withValues(alpha: .1),
      borderRadius: BorderRadius.circular(AppRadius.pill),
      border: Border.all(color: Colors.white.withValues(alpha: .12)),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, color: Colors.white, size: 15),
        const SizedBox(width: 5),
        Text(
          label,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 8.5,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
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
