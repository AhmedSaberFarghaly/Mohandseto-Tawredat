import 'package:flutter/material.dart';

import '../../core/theme/app_tokens.dart';

class BrandMark extends StatelessWidget {
  const BrandMark({super.key, this.compact = false, this.onDark = false});
  final bool compact;
  final bool onDark;

  @override
  Widget build(BuildContext context) {
    final size = compact ? 46.0 : 62.0;
    final mark = Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: onDark ? Colors.white : AppColors.primary,
        borderRadius: BorderRadius.circular(compact ? 13 : 18),
        border: Border.all(
          color: onDark ? Colors.white : AppColors.primaryTint,
        ),
        boxShadow: AppShadows.soft,
      ),
      child: Icon(
        Icons.storefront_rounded,
        color: onDark ? AppColors.primary : Colors.white,
        size: compact ? 25 : 32,
      ),
    );
    final copy = Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'مهندسيتو توريدات',
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: Theme.of(context).textTheme.titleLarge?.copyWith(
            color: onDark ? Colors.white : AppColors.primary,
            fontWeight: FontWeight.w700,
          ),
        ),
        Text(
          'كل احتياجات شركتك في مكان واحد',
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: TextStyle(
            color: onDark ? Colors.white70 : AppColors.gray500,
            fontSize: 11,
            fontWeight: FontWeight.w400,
          ),
        ),
      ],
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 300) {
          return Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              mark,
              const SizedBox(height: 9),
              DefaultTextStyle.merge(textAlign: TextAlign.center, child: copy),
            ],
          );
        }
        return Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            mark,
            const SizedBox(width: 12),
            Flexible(child: copy),
          ],
        );
      },
    );
  }
}

class AuthShell extends StatelessWidget {
  const AuthShell({
    super.key,
    required this.child,
    this.title,
    this.subtitle,
    this.showBack = false,
  });

  final Widget child;
  final String? title;
  final String? subtitle;
  final bool showBack;

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    body: SafeArea(
      child: Align(
        alignment: Alignment.topCenter,
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 28, 20, 24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    if (showBack)
                      IconButton(
                        tooltip: 'رجوع',
                        onPressed: () => Navigator.maybePop(context),
                        icon: const Icon(Icons.arrow_back_ios_new_rounded),
                      )
                    else
                      const SizedBox(width: 48),
                    const Expanded(
                      child: Center(child: BrandMark(compact: true)),
                    ),
                    const SizedBox(width: 48),
                  ],
                ),
                if (title != null) ...[
                  const SizedBox(height: 28),
                  Text(
                    title!,
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                      color: AppColors.gray900,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
                if (subtitle != null) ...[
                  const SizedBox(height: 6),
                  Text(
                    subtitle!,
                    textAlign: TextAlign.center,
                    style: Theme.of(
                      context,
                    ).textTheme.bodyMedium?.copyWith(color: AppColors.gray500),
                  ),
                ],
                const SizedBox(height: 22),
                Container(
                  padding: const EdgeInsets.all(18),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(AppRadius.xl),
                    border: Border.all(color: AppColors.gray150),
                    boxShadow: AppShadows.soft,
                  ),
                  child: child,
                ),
                const SizedBox(height: 18),
                const Wrap(
                  alignment: WrapAlignment.center,
                  spacing: 18,
                  runSpacing: 8,
                  children: [
                    _TrustItem(Icons.verified_user_outlined, 'دفع آمن'),
                    _TrustItem(Icons.local_shipping_outlined, 'توصيل موثوق'),
                    _TrustItem(Icons.support_agent_outlined, 'دعم مستمر'),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    ),
  );
}

class _TrustItem extends StatelessWidget {
  const _TrustItem(this.icon, this.label);
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: [
      Icon(icon, size: 15, color: AppColors.gray500),
      const SizedBox(width: 4),
      Text(
        label,
        style: const TextStyle(
          color: AppColors.gray500,
          fontSize: 9,
          fontWeight: FontWeight.w700,
        ),
      ),
    ],
  );
}

class InlineError extends StatelessWidget {
  const InlineError(this.message, {super.key});
  final String message;

  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 16),
    padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(
      color: AppColors.errorTint,
      borderRadius: BorderRadius.circular(AppRadius.md),
    ),
    child: Row(
      children: [
        const Icon(
          Icons.error_outline_rounded,
          color: AppColors.error,
          size: 20,
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(message, style: const TextStyle(color: AppColors.error)),
        ),
      ],
    ),
  );
}

class StepHeader extends StatelessWidget {
  const StepHeader({super.key, required this.current, required this.total});
  final int current;
  final int total;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            'الخطوة $current من $total',
            style: const TextStyle(
              color: AppColors.primary,
              fontWeight: FontWeight.w700,
            ),
          ),
          Text(
            '${(current / total * 100).round()}%',
            style: const TextStyle(color: AppColors.gray500),
          ),
        ],
      ),
      const SizedBox(height: 8),
      LinearProgressIndicator(
        value: current / total,
        minHeight: 6,
        borderRadius: BorderRadius.circular(AppRadius.pill),
        backgroundColor: AppColors.gray150,
      ),
    ],
  );
}
