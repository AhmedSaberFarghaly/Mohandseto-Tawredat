import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import '../catalog/catalog_widgets.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(categoriesProvider);
    final featured = ref.watch(
      productFeedProvider(const CatalogQuery(featured: true, pageSize: 10)),
    );
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(categoriesProvider);
        ref.invalidate(productFeedProvider);
        await ref.read(categoriesProvider.future);
      },
      child: CustomScrollView(
        slivers: [
          SliverAppBar(
            pinned: true,
            floating: true,
            titleSpacing: 16,
            title: Row(
              children: [
                Container(
                  width: 38,
                  height: 38,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(
                    Icons.inventory_2_rounded,
                    color: Colors.white,
                    size: 21,
                  ),
                ),
                const SizedBox(width: 9),
                const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'توريدات',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    Text(
                      'كل احتياجات شركتك',
                      style: TextStyle(fontSize: 8, color: AppColors.gray500),
                    ),
                  ],
                ),
              ],
            ),
            actions: [
              IconButton(
                onPressed: () {},
                icon: const Badge(
                  smallSize: 7,
                  child: Icon(Icons.notifications_none_rounded),
                ),
              ),
              IconButton(
                onPressed: () {},
                icon: const Badge(
                  label: Text('0'),
                  child: Icon(Icons.shopping_cart_outlined),
                ),
              ),
              const SizedBox(width: 5),
            ],
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            sliver: SliverList.list(
              children: [
                InkWell(
                  onTap: () => context.push('/products'),
                  borderRadius: BorderRadius.circular(AppRadius.md),
                  child: Container(
                    height: 48,
                    padding: const EdgeInsets.symmetric(horizontal: 14),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(AppRadius.md),
                      border: Border.all(color: AppColors.gray200),
                    ),
                    child: const Row(
                      children: [
                        Icon(Icons.search_rounded, color: AppColors.gray400),
                        SizedBox(width: 9),
                        Expanded(
                          child: Text(
                            'ابحث عن منتج أو قسم أو علامة تجارية',
                            style: TextStyle(color: AppColors.gray400),
                          ),
                        ),
                        Icon(
                          Icons.tune_rounded,
                          color: AppColors.primary,
                          size: 20,
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                _CompanySelector(),
                const SizedBox(height: 16),
                _HeroBanner(
                  onTap: () => context.push('/products?featured=true'),
                ),
                const SizedBox(height: 22),
                _SectionHeader(
                  title: 'تسوق حسب القسم',
                  action: 'عرض الكل',
                  onTap: () => context.go('/categories'),
                ),
                const SizedBox(height: 12),
                SizedBox(
                  height: 104,
                  child: categories.when(
                    loading: () =>
                        const Center(child: CircularProgressIndicator()),
                    error: (error, _) => CatalogError(
                      error: error,
                      retry: () => ref.invalidate(categoriesProvider),
                    ),
                    data: (items) => ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: items.length,
                      separatorBuilder: (_, __) => const SizedBox(width: 10),
                      itemBuilder: (context, index) {
                        final item = items[index];
                        return InkWell(
                          onTap: () => context.push(
                            '/products?categoryId=${item.id}&title=${Uri.encodeComponent(item.nameAr)}',
                          ),
                          borderRadius: BorderRadius.circular(AppRadius.lg),
                          child: SizedBox(
                            width: 78,
                            child: Column(
                              children: [
                                Container(
                                  width: 66,
                                  height: 66,
                                  decoration: BoxDecoration(
                                    color: index.isEven
                                        ? AppColors.primaryTint
                                        : AppColors.successTint,
                                    borderRadius: BorderRadius.circular(
                                      AppRadius.lg,
                                    ),
                                  ),
                                  child: Icon(
                                    categoryIcon(item.iconName),
                                    color: index.isEven
                                        ? AppColors.primary
                                        : AppColors.success,
                                    size: 30,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  item.nameAr,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(
                                    fontSize: 10,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                ),
                const SizedBox(height: 18),
                _SectionHeader(
                  title: 'عروض وأسعار خاصة لشركتك',
                  action: 'عرض المزيد',
                  onTap: () => context.push('/products?sort=price_asc'),
                ),
                const SizedBox(height: 12),
              ],
            ),
          ),
          featured.when(
            loading: () => const SliverToBoxAdapter(
              child: SizedBox(height: 250, child: CatalogLoading()),
            ),
            error: (error, _) => SliverToBoxAdapter(
              child: SizedBox(
                height: 250,
                child: CatalogError(
                  error: error,
                  retry: () => ref.invalidate(productFeedProvider),
                ),
              ),
            ),
            data: (page) => SliverPadding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              sliver: SliverGrid.builder(
                itemCount: page.items.length.clamp(0, 6),
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  mainAxisSpacing: 12,
                  crossAxisSpacing: 12,
                  childAspectRatio: .63,
                ),
                itemBuilder: (context, index) => CatalogProductCard(
                  product: page.items[index],
                  onFavorite: () async {
                    await ref
                        .read(catalogRepositoryProvider)
                        .toggleFavorite(page.items[index].id);
                    ref.invalidate(productFeedProvider);
                  },
                ),
              ),
            ),
          ),
          const SliverToBoxAdapter(child: SizedBox(height: 24)),
        ],
      ),
    );
  }
}

class _CompanySelector extends StatelessWidget {
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 9),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.md),
      border: Border.all(color: AppColors.gray200),
    ),
    child: const Row(
      children: [
        CircleAvatar(
          radius: 18,
          backgroundColor: AppColors.primaryTint,
          child: Icon(
            Icons.business_rounded,
            color: AppColors.primary,
            size: 19,
          ),
        ),
        SizedBox(width: 9),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'الشركة المصرية للمقاولات',
                style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
              ),
              Text(
                'الفرع الرئيسي - القاهرة',
                style: TextStyle(fontSize: 9, color: AppColors.gray500),
              ),
            ],
          ),
        ),
        Icon(Icons.keyboard_arrow_down_rounded, color: AppColors.gray500),
      ],
    ),
  );
}

class _HeroBanner extends StatelessWidget {
  const _HeroBanner({required this.onTap});
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(AppRadius.xl),
    child: Container(
      height: 154,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF023BAA), Color(0xFF167A8B)],
        ),
        borderRadius: BorderRadius.circular(AppRadius.xl),
      ),
      child: Row(
        children: [
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  'خصم حتى 15%',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 23,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                SizedBox(height: 4),
                Text(
                  'على مستلزمات المكاتب والكميات الكبيرة',
                  style: TextStyle(color: Colors.white70, fontSize: 11),
                ),
                SizedBox(height: 13),
                Text(
                  'تسوق الآن  ←',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
          Container(
            width: 98,
            height: 98,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: .14),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.inventory_2_rounded,
              size: 58,
              color: Colors.white,
            ),
          ),
        ],
      ),
    ),
  );
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({
    required this.title,
    required this.action,
    required this.onTap,
  });
  final String title;
  final String action;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Row(
    children: [
      Expanded(
        child: Text(
          title,
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
        ),
      ),
      TextButton(onPressed: onTap, child: Text(action)),
    ],
  );
}
