import 'package:flutter/material.dart';

import '../theme/app_tokens.dart';

/// Lightweight shimmer skeleton without external packages.
class Skeleton extends StatefulWidget {
  const Skeleton({
    super.key,
    this.width,
    this.height,
    this.radius = 10,
    this.circle = false,
  });
  final double? width;
  final double? height;
  final double radius;
  final bool circle;

  @override
  State<Skeleton> createState() => _SkeletonState();
}

class _SkeletonState extends State<Skeleton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1100),
  )..repeat();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: _controller,
    builder: (context, _) {
      final t = _controller.value;
      return Container(
        width: widget.width,
        height: widget.height,
        decoration: BoxDecoration(
          shape: widget.circle ? BoxShape.circle : BoxShape.rectangle,
          borderRadius: widget.circle
              ? null
              : BorderRadius.circular(widget.radius),
          gradient: LinearGradient(
            begin: Alignment(-1 - 2 * (1 - t), 0),
            end: Alignment(1 + 2 * t, 0),
            colors: const [
              Color(0xFFE9EDF3),
              Color(0xFFF6F8FB),
              Color(0xFFE9EDF3),
            ],
          ),
        ),
      );
    },
  );
}

/// Grid of product-card placeholders shown while the catalog loads.
class ProductGridSkeleton extends StatelessWidget {
  const ProductGridSkeleton({super.key, this.count = 6});
  final int count;

  @override
  Widget build(BuildContext context) => GridView.builder(
    padding: const EdgeInsets.fromLTRB(16, 6, 16, 20),
    physics: const NeverScrollableScrollPhysics(),
    itemCount: count,
    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
      crossAxisCount: 2,
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: .56,
    ),
    itemBuilder: (context, _) => const ProductCardSkeleton(),
  );
}

class ProductCardSkeleton extends StatelessWidget {
  const ProductCardSkeleton({super.key});

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(10),
    decoration: BoxDecoration(
      color: AppColors.card,
      borderRadius: BorderRadius.circular(AppRadius.lg),
      border: Border.all(color: AppColors.gray200),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: const [
        Expanded(child: Skeleton(radius: 14)),
        SizedBox(height: 10),
        Skeleton(height: 10, width: 70),
        SizedBox(height: 8),
        Skeleton(height: 13),
        SizedBox(height: 6),
        Skeleton(height: 13, width: 120),
        SizedBox(height: 10),
        Skeleton(height: 15, width: 90),
      ],
    ),
  );
}

/// Simple stacked-line placeholder for detail/list pages.
class ListSkeleton extends StatelessWidget {
  const ListSkeleton({super.key, this.rows = 6});
  final int rows;

  @override
  Widget build(BuildContext context) => ListView.separated(
    padding: const EdgeInsets.all(16),
    physics: const NeverScrollableScrollPhysics(),
    itemCount: rows,
    separatorBuilder: (_, _) => const SizedBox(height: 12),
    itemBuilder: (context, _) => Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.card,
        borderRadius: BorderRadius.circular(AppRadius.lg),
        border: Border.all(color: AppColors.gray200),
      ),
      child: Row(
        children: const [
          Skeleton(width: 64, height: 64, radius: 12),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Skeleton(height: 13, width: 160),
                SizedBox(height: 8),
                Skeleton(height: 11, width: 110),
                SizedBox(height: 8),
                Skeleton(height: 13, width: 80),
              ],
            ),
          ),
        ],
      ),
    ),
  );
}
