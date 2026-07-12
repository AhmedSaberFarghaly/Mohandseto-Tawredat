import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_tokens.dart';

/// Screen 7 — شاشة البداية (Splash).
/// Deep navy gradient with centered brand logo, name, and tagline,
/// with a slim progress indicator near the bottom (PDF p.5).
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
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
                  color: AppColors.primaryLight.withValues(alpha: 0.95),
                  borderRadius: BorderRadius.circular(AppRadius.xl),
                ),
                child: const Icon(Icons.layers, color: Colors.white, size: 44),
              ),
              const SizedBox(height: AppSpacing.xxl),
              Text(
                'مهندسيتو توريدات',
                style: GoogleFonts.cairo(
                  color: Colors.white,
                  fontSize: 28,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                'Mohandseto Tawredat',
                style: GoogleFonts.cairo(
                  color: Colors.white.withValues(alpha: 0.6),
                  fontSize: 12,
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              Text(
                'كل احتياجات شركتك... في مكان واحد',
                style: GoogleFonts.cairo(
                  color: Colors.white.withValues(alpha: 0.85),
                  fontSize: 14,
                ),
              ),
              const Spacer(flex: 3),
              SizedBox(
                width: 96,
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(AppRadius.pill),
                  child: LinearProgressIndicator(
                    minHeight: 4,
                    backgroundColor: Colors.white.withValues(alpha: 0.15),
                    color: AppColors.primaryLight,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
              Text(
                'v0.1.0',
                style: GoogleFonts.cairo(
                  color: Colors.white.withValues(alpha: 0.4),
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
}
