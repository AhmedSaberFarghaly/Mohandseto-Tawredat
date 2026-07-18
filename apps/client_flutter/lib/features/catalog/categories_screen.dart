import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_client.dart';
import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import '../../core/widgets/skeleton.dart';
import 'catalog_widgets.dart';

class CategoriesScreen extends ConsumerWidget {
  const CategoriesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(categoriesProvider);
    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            const _CategoriesHeader(),
            Padding(
              padding: const EdgeInsets.fromLTRB(18, 12, 18, 18),
              child: _SearchBox(onTap: () => context.push('/search')),
            ),
            Expanded(
              child: categories.when(
                loading: () => const _CategoriesLoading(),
                error: (error, _) => CatalogError(
                  error: error,
                  retry: () => ref.invalidate(categoriesProvider),
                ),
                data: (items) => RefreshIndicator(
                  onRefresh: () async {
                    ref.invalidate(categoriesProvider);
                    await ref.read(categoriesProvider.future);
                  },
                  child: LayoutBuilder(
                    builder: (context, constraints) {
                      final columns = constraints.maxWidth < 350 ? 1 : 2;
                      return GridView.builder(
                        physics: const AlwaysScrollableScrollPhysics(),
                        padding: const EdgeInsets.fromLTRB(18, 12, 18, 28),
                        itemCount: items.length,
                        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: columns,
                          mainAxisSpacing: 14,
                          crossAxisSpacing: 14,
                          mainAxisExtent: columns == 1 ? 136 : 154,
                        ),
                        itemBuilder: (context, index) => _CategoryCard(
                          category: items[index],
                          onTap: () => context.push(
                            '/products?categoryId=${items[index].id}&title=${Uri.encodeComponent(items[index].nameAr)}',
                          ),
                        ),
                      );
                    },
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CategoriesHeader extends StatelessWidget {
  const _CategoriesHeader();

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 70,
    child: Stack(
      alignment: Alignment.center,
      children: [
        Text(
          'الأقسام',
          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
            fontWeight: FontWeight.w700,
            color: AppColors.gray900,
          ),
        ),
        Positioned(
          left: 8,
          child: IconButton(
            tooltip: 'العودة للرئيسية',
            onPressed: () => context.go('/home'),
            icon: const Icon(
              Icons.arrow_back_ios_new_rounded,
              color: AppColors.primary,
              size: 24,
            ),
          ),
        ),
      ],
    ),
  );
}

class _SearchBox extends StatelessWidget {
  const _SearchBox({required this.onTap});
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(16),
    child: Container(
      height: 58,
      padding: const EdgeInsets.symmetric(horizontal: 17),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.gray300),
      ),
      child: const Row(
        children: [
          Expanded(
            child: Text(
              'ابحث عن قسم أو منتج',
              style: TextStyle(color: AppColors.gray400, fontSize: 13),
            ),
          ),
          Icon(Icons.search_rounded, color: AppColors.gray500, size: 29),
        ],
      ),
    ),
  );
}

class _CategoryCard extends StatelessWidget {
  const _CategoryCard({required this.category, required this.onTap});
  final CatalogCategory category;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final subtitle = category.children.isEmpty
        ? '${category.productCount} منتج متاح'
        : category.children.take(3).map((item) => item.nameAr).join(' - ');
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(18),
      child: Container(
        padding: const EdgeInsetsDirectional.fromSTEB(10, 10, 12, 10),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: AppColors.gray150),
          boxShadow: const [
            BoxShadow(
              color: Color(0x100F2F57),
              blurRadius: 18,
              offset: Offset(0, 5),
            ),
          ],
        ),
        child: Row(
          children: [
            Expanded(flex: 11, child: _CategoryArtwork(category: category)),
            const SizedBox(width: 8),
            Expanded(
              flex: 12,
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    category.nameAr,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: AppColors.gray900,
                      fontSize: 14,
                      height: 1.35,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    subtitle,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: AppColors.gray500,
                      fontSize: 10.5,
                      height: 1.45,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CategoryArtwork extends StatelessWidget {
  const _CategoryArtwork({required this.category});
  final CatalogCategory category;

  @override
  Widget build(BuildContext context) {
    final fallback = Center(
      child: Icon(
        categoryIcon(category.iconName),
        color: AppColors.primary,
        size: 58,
      ),
    );
    if (!category.hasImage) return fallback;
    final path = category.imageUrl!;
    final url = path.startsWith('http') ? path : '$apiBaseUrl$path';
    return CachedNetworkImage(
      imageUrl: url,
      fit: BoxFit.contain,
      memCacheWidth: 420,
      fadeInDuration: const Duration(milliseconds: 150),
      placeholder: (_, _) => const Skeleton(radius: 12),
      errorWidget: (_, _, _) => fallback,
    );
  }
}

class _CategoriesLoading extends StatelessWidget {
  const _CategoriesLoading();

  @override
  Widget build(BuildContext context) => GridView.builder(
    padding: const EdgeInsets.fromLTRB(18, 12, 18, 28),
    itemCount: 8,
    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
      crossAxisCount: 2,
      mainAxisSpacing: 14,
      crossAxisSpacing: 14,
      mainAxisExtent: 154,
    ),
    itemBuilder: (_, _) => const Skeleton(radius: 18),
  );
}
