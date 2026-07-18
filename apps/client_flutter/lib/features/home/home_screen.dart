import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_client.dart';
import '../../core/api/catalog_repository.dart';
import '../../core/api/cart_repository.dart';
import '../../core/api/account_repository.dart';
import '../../core/theme/app_tokens.dart';
import '../../core/widgets/skeleton.dart';
import '../catalog/catalog_widgets.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(categoriesProvider);
    final featured = ref.watch(
      productFeedProvider(const CatalogQuery(featured: true, pageSize: 10)),
    );
    final recentlyViewed = ref.watch(recentlyViewedProvider);
    final cartCount = ref.watch(cartProvider).value?.itemCount ?? 0;
    final featuredItems = featured.value?.items ?? const <CatalogProduct>[];
    return ColoredBox(
      color: Colors.white,
      child: SafeArea(
        bottom: false,
        child: RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(categoriesProvider);
            ref.invalidate(productFeedProvider);
            ref.invalidate(accountOverviewProvider);
            ref.invalidate(accountBranchesProvider);
            ref.invalidate(recentlyViewedProvider);
            await ref.read(categoriesProvider.future);
          },
          child: CustomScrollView(
            physics: const AlwaysScrollableScrollPhysics(),
            slivers: [
              SliverPadding(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
                sliver: SliverList.list(
                  children: [
                    _HomeTopBar(cartCount: cartCount),
                    const SizedBox(height: 15),
                    _HomeSearchBox(onTap: () => context.push('/search')),
                    const SizedBox(height: 14),
                    _HeroBanner(
                      products: featuredItems,
                      onTap: () => context.push('/products?featured=true'),
                    ),
                    const SizedBox(height: 18),
                    _SectionHeader(
                      title: 'الأقسام الرئيسية',
                      action: 'عرض الكل',
                      onTap: () => context.go('/categories'),
                    ),
                    const SizedBox(height: 10),
                    SizedBox(
                      height: 230,
                      child: categories.when(
                        loading: () => const _HomeCategoriesLoading(),
                        error: (error, _) => CatalogError(
                          error: error,
                          retry: () => ref.invalidate(categoriesProvider),
                        ),
                        data: (items) => _HomeCategoriesGrid(items: items),
                      ),
                    ),
                    const SizedBox(height: 15),
                    _SectionHeader(
                      title: 'عروض خاصة',
                      action: 'عرض الكل',
                      onTap: () => context.push('/products?sort=price_asc'),
                    ),
                    const SizedBox(height: 8),
                    _SpecialOffersStrip(products: featuredItems),
                    const SizedBox(height: 18),
                    _SectionHeader(
                      title: 'منتجات تطلبها كثيرًا',
                      action: 'عرض الكل',
                      onTap: () => context.push('/products?featured=true'),
                    ),
                    const SizedBox(height: 8),
                  ],
                ),
              ),
              featured.when(
                loading: () => const SliverToBoxAdapter(
                  child: Padding(
                    padding: EdgeInsets.symmetric(horizontal: 16),
                    child: SizedBox(
                      height: 232,
                      child: Row(
                        children: [
                          Expanded(child: ProductCardSkeleton()),
                          SizedBox(width: 10),
                          Expanded(child: ProductCardSkeleton()),
                        ],
                      ),
                    ),
                  ),
                ),
                error: (error, _) => SliverToBoxAdapter(
                  child: SizedBox(
                    height: 220,
                    child: CatalogError(
                      error: error,
                      retry: () => ref.invalidate(productFeedProvider),
                    ),
                  ),
                ),
                data: (page) => SliverToBoxAdapter(
                  child: SizedBox(
                    height: 235,
                    child: ListView.separated(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      scrollDirection: Axis.horizontal,
                      itemCount: page.items.length.clamp(0, 8),
                      separatorBuilder: (_, _) => const SizedBox(width: 10),
                      itemBuilder: (context, index) => SizedBox(
                        width: 148,
                        child: _CompactProductCard(
                          product: page.items[index],
                          onFavorite: () async {
                            await ref
                                .read(catalogRepositoryProvider)
                                .toggleFavorite(page.items[index].id);
                            ref.invalidate(productFeedProvider);
                          },
                          onAdd: () async {
                            await ref
                                .read(cartRepositoryProvider)
                                .add(
                                  page.items[index].id,
                                  page.items[index].minOrderQty,
                                );
                            ref.invalidate(cartProvider);
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text('تمت إضافة المنتج إلى السلة'),
                                ),
                              );
                            }
                          },
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              SliverToBoxAdapter(
                child: _RecentlyViewedSection(items: recentlyViewed),
              ),
              const SliverToBoxAdapter(child: SizedBox(height: 28)),
            ],
          ),
        ),
      ),
    );
  }
}

class _HomeTopBar extends ConsumerWidget {
  const _HomeTopBar({required this.cartCount});
  final int cartCount;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final overview = ref.watch(accountOverviewProvider);
    final firstName = overview.value?.profile.name.trim().split(' ').first;
    return Row(
      children: [
        _HeaderAction(
          tooltip: 'السلة',
          icon: Icons.shopping_cart_outlined,
          count: cartCount,
          onTap: () => context.push('/cart'),
        ),
        const SizedBox(width: 2),
        _HeaderAction(
          tooltip: 'الإشعارات',
          icon: Icons.notifications_none_rounded,
          count: 0,
          dot: true,
          onTap: () => context.push('/notifications'),
        ),
        const SizedBox(width: 7),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'مرحبًا ${firstName ?? ''} 👋',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: AppColors.gray900,
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 2),
              const Text(
                'جاهز لتلبية احتياجات شركتك',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: AppColors.gray400, fontSize: 9.5),
              ),
            ],
          ),
        ),
        const SizedBox(width: 6),
        const Expanded(flex: 2, child: _CompanySelector()),
      ],
    );
  }
}

class _HeaderAction extends StatelessWidget {
  const _HeaderAction({
    required this.tooltip,
    required this.icon,
    required this.count,
    required this.onTap,
    this.dot = false,
  });
  final String tooltip;
  final IconData icon;
  final int count;
  final VoidCallback onTap;
  final bool dot;

  @override
  Widget build(BuildContext context) => IconButton(
    tooltip: tooltip,
    onPressed: onTap,
    visualDensity: VisualDensity.compact,
    icon: Badge(
      isLabelVisible: count > 0 || dot,
      label: count > 0 ? Text('$count') : null,
      smallSize: dot ? 8 : null,
      backgroundColor: AppColors.error,
      child: Icon(icon, color: AppColors.primaryDark, size: 28),
    ),
  );
}

class _HomeSearchBox extends StatelessWidget {
  const _HomeSearchBox({required this.onTap});
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(15),
    child: Container(
      height: 56,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: AppColors.gray300),
      ),
      child: const Row(
        children: [
          Expanded(
            child: Text(
              'ابحث عن منتج أو قسم',
              style: TextStyle(color: AppColors.gray400, fontSize: 12.5),
            ),
          ),
          Icon(Icons.search_rounded, color: AppColors.gray500, size: 28),
        ],
      ),
    ),
  );
}

class _CompanySelector extends ConsumerWidget {
  const _CompanySelector();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final overview = ref.watch(accountOverviewProvider);
    final branches = ref.watch(accountBranchesProvider);
    return overview.when(
      loading: () => const Skeleton(height: 48, radius: 14),
      error: (_, _) => _selectorShell(
        context,
        title: 'تعذر تحميل بيانات الشركة',
        subtitle: 'اضغط لإعادة المحاولة',
        onTap: () => ref.invalidate(accountOverviewProvider),
      ),
      data: (account) {
        final items = branches.value ?? const <AccountBranchModel>[];
        final selected = items
            .where((branch) => branch.id == account.profile.branchId)
            .firstOrNull;
        final fallback = items.where((branch) => branch.main).firstOrNull;
        final branch = selected ?? fallback ?? items.firstOrNull;
        final clientNumber =
            account.company.registrationNo ??
            account.company.id.replaceAll('-', '').substring(0, 5);
        return _selectorShell(
          context,
          title: account.company.name,
          subtitle:
              'رقم العميل: $clientNumber${branch == null ? '' : ' • ${branch.name}'}',
          onTap: () => _showBranches(context, ref, account, items),
        );
      },
    );
  }

  Widget _selectorShell(
    BuildContext context, {
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(14),
    child: Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          const Icon(
            Icons.keyboard_arrow_down_rounded,
            color: AppColors.primary,
            size: 18,
          ),
          const SizedBox(width: 4),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: AppColors.primary,
                    fontSize: 10.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                Text(
                  subtitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 8.5,
                    color: AppColors.gray500,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(13),
              border: Border.all(color: AppColors.gray150),
              boxShadow: const [
                BoxShadow(
                  color: Color(0x0F102846),
                  blurRadius: 12,
                  offset: Offset(0, 4),
                ),
              ],
            ),
            child: const Icon(
              Icons.apartment_rounded,
              color: AppColors.primary,
              size: 24,
            ),
          ),
        ],
      ),
    ),
  );

  Future<void> _showBranches(
    BuildContext context,
    WidgetRef ref,
    AccountOverviewModel account,
    List<AccountBranchModel> branches,
  ) async {
    if (branches.isEmpty) {
      context.push('/account/branches');
      return;
    }
    final selected = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
          children: [
            ListTile(
              title: Text(
                account.company.name,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              subtitle: const Text('اختر الفرع الافتراضي للطلبات والتوصيل'),
              trailing: IconButton(
                tooltip: 'إدارة الفروع والعناوين',
                onPressed: () {
                  Navigator.pop(sheetContext);
                  context.push('/account/branches');
                },
                icon: const Icon(Icons.settings_outlined),
              ),
            ),
            ...branches.map(
              (branch) => RadioListTile<String>(
                value: branch.id,
                groupValue: account.profile.branchId,
                onChanged: (value) => Navigator.pop(sheetContext, value),
                title: Text(branch.name),
                subtitle: Text(
                  [
                        branch.governorate,
                        branch.city,
                        branch.address,
                        branch.phone,
                      ]
                      .whereType<String>()
                      .where((value) => value.isNotEmpty)
                      .join(' • '),
                ),
                secondary: Icon(
                  branch.main ? Icons.star_rounded : Icons.location_on_outlined,
                  color: branch.main ? AppColors.warning : AppColors.primary,
                ),
              ),
            ),
          ],
        ),
      ),
    );
    if (selected == null || selected == account.profile.branchId) return;
    final profile = account.profile;
    await ref.read(accountRepositoryProvider).updateProfile({
      'fullName': profile.name,
      'email': profile.email,
      'language': profile.language,
      'jobTitle': profile.jobTitle,
      'department': profile.department,
      'defaultBranchId': selected,
    });
    ref.invalidate(accountOverviewProvider);
    if (context.mounted) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('تم تحديث الفرع الافتراضي')));
    }
  }
}

class _RecentlyViewedSection extends ConsumerWidget {
  const _RecentlyViewedSection({required this.items});
  final AsyncValue<List<CatalogProduct>> items;

  @override
  Widget build(BuildContext context, WidgetRef ref) => items.when(
    loading: () => const SizedBox.shrink(),
    error: (_, _) => const SizedBox.shrink(),
    data: (products) {
      if (products.isEmpty) return const SizedBox.shrink();
      return Padding(
        padding: const EdgeInsets.fromLTRB(16, 24, 16, 0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _SectionHeader(
              title: 'شوهدت مؤخرًا',
              action: 'استكمل التصفح',
              onTap: () => context.push('/products'),
            ),
            const SizedBox(height: 12),
            SizedBox(
              height: 310,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: products.length.clamp(0, 10),
                separatorBuilder: (_, _) => const SizedBox(width: 10),
                itemBuilder: (context, index) => SizedBox(
                  width: 174,
                  child: CatalogProductCard(
                    product: products[index],
                    onFavorite: () async {
                      await ref
                          .read(catalogRepositoryProvider)
                          .toggleFavorite(products[index].id);
                      ref.invalidate(recentlyViewedProvider);
                    },
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

class _HomeCategoriesGrid extends StatelessWidget {
  const _HomeCategoriesGrid({required this.items});
  final List<CatalogCategory> items;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) {
      const spacing = 9.0;
      final width = (constraints.maxWidth - spacing * 3) / 4;
      return Wrap(
        alignment: WrapAlignment.center,
        spacing: spacing,
        runSpacing: 10,
        children: [
          for (final item in items.take(7))
            SizedBox(
              width: width,
              height: 108,
              child: _HomeCategoryTile(category: item),
            ),
        ],
      );
    },
  );
}

class _HomeCategoryTile extends StatelessWidget {
  const _HomeCategoryTile({required this.category});
  final CatalogCategory category;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => context.push(
      '/products?categoryId=${category.id}&title=${Uri.encodeComponent(category.nameAr)}',
    ),
    borderRadius: BorderRadius.circular(15),
    child: Container(
      padding: const EdgeInsets.all(7),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: AppColors.gray150),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0D102846),
            blurRadius: 12,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          Expanded(child: _CategoryArtwork(category: category)),
          const SizedBox(height: 4),
          Text(
            category.nameAr,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: AppColors.gray900,
              fontSize: 9.5,
              height: 1.25,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    ),
  );
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
        size: 36,
      ),
    );
    if (!category.hasImage) return fallback;
    final path = category.imageUrl!;
    final url = path.startsWith('http') ? path : '$apiBaseUrl$path';
    return CachedNetworkImage(
      imageUrl: url,
      fit: BoxFit.contain,
      memCacheWidth: 260,
      fadeInDuration: const Duration(milliseconds: 150),
      errorWidget: (_, _, _) => fallback,
    );
  }
}

class _HomeCategoriesLoading extends StatelessWidget {
  const _HomeCategoriesLoading();

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) {
      final width = (constraints.maxWidth - 27) / 4;
      return Wrap(
        spacing: 9,
        runSpacing: 10,
        children: List.generate(
          7,
          (_) => SizedBox(
            width: width,
            height: 108,
            child: const Skeleton(radius: 15),
          ),
        ),
      );
    },
  );
}

class _HeroBanner extends StatelessWidget {
  const _HeroBanner({required this.products, required this.onTap});
  final List<CatalogProduct> products;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) {
      final compact = constraints.maxWidth < 330;
      final artworkSize = compact ? 126.0 : 150.0;
      final copyWidth = compact ? constraints.maxWidth - 142 : 210.0;
      return InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(18),
        child: Container(
          height: 184,
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(
            gradient: const LinearGradient(
              colors: [Color(0xFF0349C8), Color(0xFF012E86)],
              begin: Alignment.topRight,
              end: Alignment.bottomLeft,
            ),
            borderRadius: BorderRadius.circular(18),
          ),
          child: Stack(
            children: [
              PositionedDirectional(
                start: 18,
                top: 19,
                bottom: 22,
                width: copyWidth,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      'كل احتياجات شركتك\nفي مكان واحد',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: compact ? 18 : 21,
                        height: 1.28,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 5),
                    const Text(
                      'توريدات مكتبية - جودة - سرعة - ثقة',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: Colors.white70, fontSize: 9.5),
                    ),
                    const Spacer(),
                    Container(
                      height: 40,
                      padding: EdgeInsets.symmetric(
                        horizontal: compact ? 12 : 18,
                      ),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(13),
                      ),
                      child: const Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Text(
                            'تسوق الآن',
                            style: TextStyle(
                              color: AppColors.primary,
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          SizedBox(width: 8),
                          Icon(
                            Icons.arrow_back_ios_new_rounded,
                            color: AppColors.primary,
                            size: 14,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
              PositionedDirectional(
                end: -4,
                bottom: 8,
                width: artworkSize,
                height: artworkSize,
                child: _HeroArtwork(product: products.firstOrNull),
              ),
              Positioned(
                bottom: 10,
                left: 0,
                right: 0,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      width: 17,
                      height: 6,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(9),
                      ),
                    ),
                    const SizedBox(width: 5),
                    for (var i = 0; i < 2; i++) ...[
                      Container(
                        width: 6,
                        height: 6,
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: .35),
                          shape: BoxShape.circle,
                        ),
                      ),
                      if (i == 0) const SizedBox(width: 5),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
      );
    },
  );
}

class _HeroArtwork extends StatelessWidget {
  const _HeroArtwork({this.product});
  final CatalogProduct? product;

  @override
  Widget build(BuildContext context) {
    final path = product?.imageUrl;
    if (path != null && !path.startsWith('asset://')) {
      final url = path.startsWith('http') ? path : '$apiBaseUrl$path';
      return CachedNetworkImage(
        imageUrl: url,
        fit: BoxFit.contain,
        memCacheWidth: 600,
        errorWidget: (_, _, _) => const _HeroFallback(),
      );
    }
    return const _HeroFallback();
  }
}

class _HeroFallback extends StatelessWidget {
  const _HeroFallback();

  @override
  Widget build(BuildContext context) => Stack(
    alignment: Alignment.bottomCenter,
    children: [
      Container(
        width: 118,
        height: 118,
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: .12),
          shape: BoxShape.circle,
        ),
      ),
      const Positioned(
        bottom: 20,
        child: Icon(Icons.inventory_2_rounded, color: Colors.white, size: 88),
      ),
      const Positioned(
        left: 7,
        bottom: 14,
        child: Icon(Icons.edit_rounded, color: Color(0xFFFFC857), size: 45),
      ),
      const Positioned(
        right: 5,
        bottom: 12,
        child: Icon(Icons.description_rounded, color: Colors.white70, size: 49),
      ),
    ],
  );
}

class _SpecialOffersStrip extends StatelessWidget {
  const _SpecialOffersStrip({required this.products});
  final List<CatalogProduct> products;

  @override
  Widget build(BuildContext context) {
    const colors = [Color(0xFFFFF4D9), Color(0xFFE9F5FF), Color(0xFFEAF7E7)];
    const badges = ['خصم 20%', 'خصم 15%', 'شحن مجاني'];
    return SizedBox(
      height: 126,
      child: Row(
        children: [
          for (var i = 0; i < 3; i++) ...[
            if (i > 0) const SizedBox(width: 8),
            Expanded(
              child: _OfferCard(
                color: colors[i],
                badge: badges[i],
                product: products.elementAtOrNull(i),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _OfferCard extends StatelessWidget {
  const _OfferCard({
    required this.color,
    required this.badge,
    required this.product,
  });
  final Color color;
  final String badge;
  final CatalogProduct? product;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => product == null
        ? context.push('/products')
        : context.push('/products/${product!.slug}'),
    borderRadius: BorderRadius.circular(14),
    child: Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
            decoration: BoxDecoration(
              color: badge.contains('شحن')
                  ? AppColors.success
                  : AppColors.error,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(
              badge,
              style: const TextStyle(
                color: Colors.white,
                fontSize: 8,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          const SizedBox(height: 4),
          Expanded(
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        product?.nameAr ?? 'عروض توريدات الشركات',
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 8.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 4),
                      const Text(
                        'تسوق الآن',
                        style: TextStyle(
                          color: AppColors.primary,
                          fontSize: 8.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
                SizedBox(
                  width: 48,
                  child: product == null
                      ? const Icon(
                          Icons.local_shipping_rounded,
                          color: AppColors.primary,
                          size: 38,
                        )
                      : ProductVisual(product: product!),
                ),
              ],
            ),
          ),
        ],
      ),
    ),
  );
}

class _CompactProductCard extends StatelessWidget {
  const _CompactProductCard({
    required this.product,
    required this.onFavorite,
    required this.onAdd,
  });
  final CatalogProduct product;
  final VoidCallback onFavorite;
  final VoidCallback onAdd;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => context.push('/products/${product.slug}'),
    borderRadius: BorderRadius.circular(15),
    child: Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: AppColors.gray150),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0D102846),
            blurRadius: 12,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Stack(
              children: [
                Positioned.fill(child: ProductVisual(product: product)),
                Positioned(
                  top: 0,
                  left: 0,
                  child: InkResponse(
                    onTap: onFavorite,
                    radius: 18,
                    child: Padding(
                      padding: const EdgeInsets.all(3),
                      child: Icon(
                        product.isFavorite
                            ? Icons.favorite_rounded
                            : Icons.favorite_border_rounded,
                        color: product.isFavorite
                            ? AppColors.error
                            : AppColors.gray400,
                        size: 20,
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 6),
          Text(
            product.nameAr,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(
              color: AppColors.gray900,
              fontSize: 10,
              height: 1.25,
              fontWeight: FontWeight.w700,
            ),
          ),
          Text(
            '${product.minOrderQty} ${product.unitName} • ${product.stockStatus}',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(color: AppColors.gray500, fontSize: 8),
          ),
          const SizedBox(height: 5),
          Row(
            children: [
              Expanded(
                child: Text(
                  '${product.price.toStringAsFixed(2)} ج.م',
                  maxLines: 1,
                  style: const TextStyle(
                    color: AppColors.gray900,
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              SizedBox(
                width: 34,
                height: 34,
                child: IconButton.filled(
                  padding: EdgeInsets.zero,
                  onPressed: onAdd,
                  icon: const Icon(Icons.shopping_cart_outlined, size: 18),
                ),
              ),
            ],
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
          style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
        ),
      ),
      TextButton(onPressed: onTap, child: Text(action)),
    ],
  );
}
