import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/api/cart_repository.dart';
import '../../core/api/account_repository.dart';
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
    final recentlyViewed = ref.watch(recentlyViewedProvider);
    final cartCount = ref.watch(cartProvider).value?.itemCount ?? 0;
    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(categoriesProvider);
        ref.invalidate(productFeedProvider);
        ref.invalidate(accountOverviewProvider);
        ref.invalidate(accountBranchesProvider);
        ref.invalidate(recentlyViewedProvider);
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
                tooltip: 'طلبات عروض الأسعار',
                onPressed: () => context.push('/rfqs'),
                icon: const Icon(Icons.request_quote_outlined),
              ),
              IconButton(
                tooltip: 'مركز الموافقات',
                onPressed: () => context.push('/approvals'),
                icon: const Icon(Icons.approval_outlined),
              ),
              IconButton(
                tooltip: 'الإشعارات',
                onPressed: () => context.push('/notifications'),
                icon: const Badge(
                  smallSize: 7,
                  child: Icon(Icons.notifications_none_rounded),
                ),
              ),
              IconButton(
                onPressed: () => context.push('/cart'),
                icon: Badge(
                  isLabelVisible: cartCount > 0,
                  label: Text('$cartCount'),
                  child: const Icon(Icons.shopping_cart_outlined),
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
                  onTap: () => context.push('/search'),
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
                const _CompanySelector(),
                const SizedBox(height: 16),
                _HeroBanner(
                  onTap: () => context.push('/products?featured=true'),
                ),
                const SizedBox(height: 12),
                _CustomPrintingBanner(
                  onTap: () => context.push('/custom-products'),
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
          SliverToBoxAdapter(
            child: _RecentlyViewedSection(items: recentlyViewed),
          ),
          const SliverToBoxAdapter(child: SizedBox(height: 24)),
        ],
      ),
    );
  }
}

class _CompanySelector extends ConsumerWidget {
  const _CompanySelector();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final overview = ref.watch(accountOverviewProvider);
    final branches = ref.watch(accountBranchesProvider);
    return overview.when(
      loading: () => const LinearProgressIndicator(minHeight: 2),
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
        return _selectorShell(
          context,
          title: account.company.name,
          subtitle: branch == null
              ? 'لا يوجد فرع افتراضي'
              : '${branch.name}${branch.city == null ? '' : ' - ${branch.city}'}',
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
    borderRadius: BorderRadius.circular(AppRadius.md),
    child: Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 9),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(AppRadius.md),
        border: Border.all(color: AppColors.gray200),
      ),
      child: Row(
        children: [
          const CircleAvatar(
            radius: 18,
            backgroundColor: AppColors.primaryTint,
            child: Icon(
              Icons.business_rounded,
              color: AppColors.primary,
              size: 19,
            ),
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                Text(
                  subtitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 9, color: AppColors.gray500),
                ),
              ],
            ),
          ),
          const Icon(
            Icons.keyboard_arrow_down_rounded,
            color: AppColors.gray500,
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
                style: const TextStyle(fontWeight: FontWeight.w800),
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
              height: 286,
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

class _CustomPrintingBanner extends StatelessWidget {
  const _CustomPrintingBanner({required this.onTap});
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(AppRadius.lg),
    child: Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF4E6),
        borderRadius: BorderRadius.circular(AppRadius.lg),
        border: Border.all(color: const Color(0xFFFFD7A1)),
      ),
      child: const Row(
        children: [
          CircleAvatar(
            backgroundColor: Color(0xFFFF9D34),
            child: Icon(Icons.print_rounded, color: Colors.white),
          ),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'طباعة وتخصيص للشركات',
                  style: TextStyle(fontWeight: FontWeight.w800),
                ),
                Text(
                  'ارفع شعارك واعتمد التصميم قبل الإنتاج',
                  style: TextStyle(color: AppColors.gray600, fontSize: 10),
                ),
              ],
            ),
          ),
          Icon(
            Icons.arrow_back_ios_new_rounded,
            color: Color(0xFFFF9D34),
            size: 16,
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
