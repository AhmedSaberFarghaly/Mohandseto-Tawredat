import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/widgets/skeleton.dart';
import '../../core/api/customization_repository.dart';
import '../../core/theme/app_tokens.dart';

class CustomProductsScreen extends ConsumerWidget {
  const CustomProductsScreen({super.key, this.productId});
  final String? productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final templates = ref.watch(customTemplatesProvider);
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('منتجات مطبوعة ومخصصة'),
        actions: [
          IconButton(
            tooltip: 'طلباتي',
            onPressed: () => context.push('/custom-requests'),
            icon: const Icon(Icons.assignment_outlined),
          ),
        ],
      ),
      body: templates.when(
        loading: () => const ListSkeleton(),
        error: (error, _) => _StateMessage(
          icon: Icons.cloud_off_outlined,
          title: 'تعذر تحميل المنتجات',
          action: () => ref.invalidate(customTemplatesProvider),
        ),
        data: (all) {
          final items = productId == null
              ? all
              : all.where((x) => x.productId == productId).toList();
          if (items.isEmpty) {
            return const _StateMessage(
              icon: Icons.print_disabled_outlined,
              title: 'لا يتوفر قالب تخصيص لهذا المنتج حاليًا',
            );
          }
          return RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(customTemplatesProvider);
              await ref.read(customTemplatesProvider.future);
            },
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Container(
                  padding: const EdgeInsets.all(18),
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [AppColors.primary, Color(0xFF167A8B)],
                    ),
                    borderRadius: BorderRadius.circular(AppRadius.xl),
                    boxShadow: AppShadows.floating,
                  ),
                  child: const Row(
                    children: [
                      CircleAvatar(
                        radius: 28,
                        backgroundColor: Colors.white24,
                        child: Icon(
                          Icons.design_services_rounded,
                          color: Colors.white,
                          size: 30,
                        ),
                      ),
                      SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'هوية شركتك على منتجاتك',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 18,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            SizedBox(height: 4),
                            Text(
                              'اختر الخامة واللون والطباعة، واعتمد التصميم قبل الإنتاج',
                              style: TextStyle(
                                color: Colors.white70,
                                height: 1.5,
                                fontSize: 11,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 18),
                Row(
                  children: [
                    Text(
                      '${items.length} منتج قابل للتخصيص',
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    const Spacer(),
                    TextButton(
                      onPressed: () => context.push('/custom-requests'),
                      child: const Text('متابعة طلباتي'),
                    ),
                  ],
                ),
                ...items.map((item) => _TemplateCard(item: item)),
              ],
            ),
          );
        },
      ),
    );
  }
}

class _TemplateCard extends StatelessWidget {
  const _TemplateCard({required this.item});
  final CustomTemplateSummary item;
  @override
  Widget build(BuildContext context) => Card(
    margin: const EdgeInsets.only(bottom: 12),
    child: InkWell(
      onTap: () => context.push('/custom-products/${item.id}'),
      borderRadius: BorderRadius.circular(AppRadius.lg),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          children: [
            Container(
              width: 78,
              height: 78,
              decoration: BoxDecoration(
                color: AppColors.primaryTint,
                borderRadius: BorderRadius.circular(AppRadius.md),
              ),
              child: const Icon(
                Icons.print_rounded,
                color: AppColors.primary,
                size: 36,
              ),
            ),
            const SizedBox(width: 13),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.name,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      height: 1.4,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    item.sku,
                    style: const TextStyle(
                      color: AppColors.gray500,
                      fontSize: 10.5,
                    ),
                  ),
                  const SizedBox(height: 7),
                  Wrap(
                    spacing: 6,
                    children: [
                      _Chip('${item.minQuantity}+ قطعة'),
                      _Chip('${item.leadTimeDays} أيام'),
                    ],
                  ),
                  const SizedBox(height: 7),
                  Text(
                    'يبدأ من ${NumberFormat('#,##0.00', 'ar').format(item.startingPrice)} ج.م / قطعة',
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontWeight: FontWeight.w700,
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
            ),
            const Icon(
              Icons.arrow_back_ios_new_rounded,
              color: AppColors.gray400,
              size: 16,
            ),
          ],
        ),
      ),
    ),
  );
}

class _Chip extends StatelessWidget {
  const _Chip(this.text);
  final String text;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 4),
    decoration: BoxDecoration(
      color: AppColors.gray100,
      borderRadius: BorderRadius.circular(AppRadius.pill),
    ),
    child: Text(
      text,
      style: const TextStyle(fontSize: 10.5, color: AppColors.gray600),
    ),
  );
}

class _StateMessage extends StatelessWidget {
  const _StateMessage({required this.icon, required this.title, this.action});
  final IconData icon;
  final String title;
  final VoidCallback? action;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 58, color: AppColors.gray400),
          const SizedBox(height: 12),
          Text(title, textAlign: TextAlign.center),
          if (action != null)
            TextButton(onPressed: action, child: const Text('إعادة المحاولة')),
        ],
      ),
    ),
  );
}
