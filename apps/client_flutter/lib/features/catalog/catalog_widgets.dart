import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/api/api_client.dart';
import '../../core/theme/app_tokens.dart';
import '../../core/widgets/skeleton.dart';

IconData categoryIcon(String? name) => switch (name) {
  'business_center' => Icons.business_center_outlined,
  'edit' => Icons.edit_outlined,
  'description' => Icons.description_outlined,
  'print' => Icons.print_outlined,
  'cleaning_services' => Icons.cleaning_services_outlined,
  'inventory_2' => Icons.inventory_2_outlined,
  'health_and_safety' => Icons.health_and_safety_outlined,
  'electrical_services' => Icons.electrical_services_outlined,
  'handyman' => Icons.handyman_outlined,
  'local_cafe' => Icons.local_cafe_outlined,
  'devices' => Icons.devices_outlined,
  'chair' => Icons.chair_outlined,
  _ => Icons.category_outlined,
};

class ProductVisual extends StatelessWidget {
  const ProductVisual({super.key, required this.product, this.large = false});
  final CatalogProduct product;
  final bool large;

  @override
  Widget build(BuildContext context) {
    final colors = <List<Color>>[
      [const Color(0xFFEAF2FF), const Color(0xFFDCE9FF)],
      [const Color(0xFFEAF8F7), const Color(0xFFD8F0ED)],
      [const Color(0xFFFFF4E8), const Color(0xFFFFE8CE)],
      [const Color(0xFFF4F0FF), const Color(0xFFE8E0FF)],
    ][product.id.hashCode.abs() % 4];
    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: colors,
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.circular(AppRadius.lg),
      ),
      child: Stack(
        children: [
          Positioned(
            left: -18,
            bottom: -18,
            child: Container(
              width: large ? 120 : 70,
              height: large ? 120 : 70,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: .38),
              ),
            ),
          ),
          Positioned.fill(child: _visualContent()),
          if (product.isPrintable)
            Positioned(
              top: 9,
              right: 9,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(AppRadius.pill),
                ),
                child: const Text(
                  'قابل للطباعة',
                  style: TextStyle(color: Colors.white, fontSize: 9.5),
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _visualContent() {
    final path = product.imageUrl;
    final canLoad = path != null && !path.startsWith('asset://');
    if (!canLoad) return _fallbackIcon();
    final url = path.startsWith('http') ? path : '$apiBaseUrl$path';
    return ClipRRect(
      borderRadius: BorderRadius.circular(AppRadius.lg),
      child: CachedNetworkImage(
        imageUrl: url,
        width: double.infinity,
        height: double.infinity,
        fit: large ? BoxFit.contain : BoxFit.cover,
        fadeInDuration: const Duration(milliseconds: 180),
        fadeOutDuration: const Duration(milliseconds: 120),
        memCacheWidth: large ? 1024 : 480,
        errorWidget: (_, _, _) => _fallbackIcon(),
        placeholder: (_, _) => const Skeleton(radius: 0),
      ),
    );
  }

  Widget _fallbackIcon() => Center(
    child: Icon(
      _productIcon(product.categoryName),
      size: large ? 112 : 62,
      color: AppColors.primary.withValues(alpha: .83),
    ),
  );

  IconData _productIcon(String category) {
    if (category.contains('ورق') || category.contains('دفاتر')) {
      return Icons.description_rounded;
    }
    if (category.contains('أقلام')) return Icons.edit_rounded;
    if (category.contains('أحبار') || category.contains('طباعة')) {
      return Icons.print_rounded;
    }
    if (category.contains('تنظيف') || category.contains('منظفات')) {
      return Icons.cleaning_services_rounded;
    }
    if (category.contains('كراسي')) return Icons.chair_rounded;
    if (category.contains('كمبيوتر') || category.contains('أجهزة')) {
      return Icons.devices_rounded;
    }
    return Icons.inventory_2_rounded;
  }
}

class CatalogProductCard extends StatelessWidget {
  const CatalogProductCard({
    super.key,
    required this.product,
    this.onFavorite,
    this.list = false,
  });
  final CatalogProduct product;
  final VoidCallback? onFavorite;
  final bool list;

  @override
  Widget build(BuildContext context) {
    final number = NumberFormat('#,##0.00', 'ar');
    final content = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          product.brandName ?? product.categoryName,
          maxLines: 1,
          style: const TextStyle(color: AppColors.gray500, fontSize: 11),
        ),
        const SizedBox(height: 3),
        Text(
          product.nameAr,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(
            color: AppColors.gray900,
            fontWeight: FontWeight.w700,
            fontSize: 13.5,
            height: 1.35,
          ),
        ),
        const SizedBox(height: 5),
        Row(
          children: [
            const Icon(Icons.star_rounded, color: Color(0xFFF5A623), size: 15),
            Text(
              ' ${product.rating.toStringAsFixed(1)}',
              style: const TextStyle(fontSize: 11),
            ),
            Text(
              ' (${product.ratingCount})',
              style: const TextStyle(fontSize: 10.5, color: AppColors.gray400),
            ),
          ],
        ),
        const Spacer(),
        if (product.hasContractPrice)
          const Text(
            'سعر شركتك',
            style: TextStyle(
              fontSize: 10.5,
              color: AppColors.success,
              fontWeight: FontWeight.w700,
            ),
          ),
        Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              number.format(product.price),
              style: const TextStyle(
                color: AppColors.primary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
              ),
            ),
            const Text(
              ' ج.م',
              style: TextStyle(color: AppColors.primary, fontSize: 10.5),
            ),
            if (product.compareAtPrice != null) ...[
              const SizedBox(width: 5),
              Text(
                number.format(product.compareAtPrice),
                style: const TextStyle(
                  color: AppColors.gray400,
                  fontSize: 10.5,
                  decoration: TextDecoration.lineThrough,
                ),
              ),
            ],
          ],
        ),
      ],
    );

    return InkWell(
      borderRadius: BorderRadius.circular(AppRadius.xl),
      onTap: () => context.push('/products/${product.slug}'),
      child: Container(
        padding: const EdgeInsets.all(9),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(AppRadius.xl),
          border: Border.all(color: AppColors.gray150),
          boxShadow: AppShadows.soft,
        ),
        child: list
            ? SizedBox(
                height: 142,
                child: Row(
                  children: [
                    SizedBox(
                      width: 118,
                      height: 118,
                      child: ProductVisual(product: product),
                    ),
                    const SizedBox(width: 12),
                    Expanded(child: content),
                    IconButton(
                      onPressed: onFavorite,
                      icon: Icon(
                        product.isFavorite
                            ? Icons.favorite_rounded
                            : Icons.favorite_border_rounded,
                      ),
                      color: product.isFavorite
                          ? AppColors.error
                          : AppColors.gray400,
                    ),
                  ],
                ),
              )
            : Column(
                children: [
                  Stack(
                    children: [
                      AspectRatio(
                        aspectRatio: 1.22,
                        child: ProductVisual(product: product),
                      ),
                      Positioned(
                        top: 2,
                        left: 2,
                        child: IconButton.filledTonal(
                          visualDensity: VisualDensity.compact,
                          onPressed: onFavorite,
                          iconSize: 18,
                          icon: Icon(
                            product.isFavorite
                                ? Icons.favorite_rounded
                                : Icons.favorite_border_rounded,
                          ),
                          color: product.isFavorite
                              ? AppColors.error
                              : AppColors.gray500,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Expanded(child: content),
                ],
              ),
      ),
    );
  }
}

class CatalogLoading extends StatelessWidget {
  const CatalogLoading({super.key, this.message = 'جاري تحميل المنتجات...'});
  final String message;
  @override
  Widget build(BuildContext context) => const ProductGridSkeleton();
}

class CatalogError extends StatelessWidget {
  const CatalogError({super.key, required this.error, required this.retry});
  final Object error;
  final VoidCallback retry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_rounded,
            size: 64,
            color: AppColors.gray400,
          ),
          const SizedBox(height: 12),
          const Text(
            'تعذر تحميل البيانات',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 6),
          Text(
            error.toString(),
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.gray500),
          ),
          const SizedBox(height: 18),
          OutlinedButton.icon(
            onPressed: retry,
            icon: const Icon(Icons.refresh_rounded),
            label: const Text('إعادة المحاولة'),
          ),
        ],
      ),
    ),
  );
}
