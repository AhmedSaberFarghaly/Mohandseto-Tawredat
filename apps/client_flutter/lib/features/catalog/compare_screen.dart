import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'catalog_widgets.dart';

class CompareScreen extends ConsumerWidget {
  const CompareScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final compare = ref.watch(compareProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('مقارنة المنتجات'),
        actions: [
          if (compare.value?.isNotEmpty == true)
            TextButton(
              onPressed: () async {
                await ref.read(catalogRepositoryProvider).clearCompare();
                ref.invalidate(compareProvider);
              },
              child: const Text('مسح الكل'),
            ),
        ],
      ),
      body: compare.when(
        loading: () => const CatalogLoading(message: 'جاري إعداد المقارنة...'),
        error: (error, _) => CatalogError(
          error: error,
          retry: () => ref.invalidate(compareProvider),
        ),
        data: (items) {
          if (items.isEmpty) return const _EmptyCompare();
          final attributeNames = items
              .expand((item) => item.attributes.keys)
              .toSet()
              .toList();
          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SizedBox(width: 120),
                      ...items.map(
                        (item) => _CompareHeader(
                          item: item,
                          remove: () async {
                            await ref
                                .read(catalogRepositoryProvider)
                                .toggleCompare(item.summary.id);
                            ref.invalidate(compareProvider);
                          },
                        ),
                      ),
                    ],
                  ),
                  _ComparisonRow(
                    label: 'السعر',
                    values: items
                        .map(
                          (item) =>
                              '${NumberFormat('#,##0.00', 'ar').format(item.summary.price)} ج.م',
                        )
                        .toList(),
                    highlighted: true,
                  ),
                  _ComparisonRow(
                    label: 'العلامة',
                    values: items
                        .map((item) => item.summary.brandName ?? '-')
                        .toList(),
                  ),
                  _ComparisonRow(
                    label: 'التوفر',
                    values: items
                        .map(
                          (item) => item.summary.stockStatus == 'OutOfStock'
                              ? 'غير متوفر'
                              : '${item.summary.stockQty} متاح',
                        )
                        .toList(),
                  ),
                  _ComparisonRow(
                    label: 'التقييم',
                    values: items
                        .map(
                          (item) =>
                              '${item.summary.rating.toStringAsFixed(1)} ★',
                        )
                        .toList(),
                  ),
                  ...attributeNames.map(
                    (name) => _ComparisonRow(
                      label: name,
                      values: items
                          .map((item) => item.attributes[name] ?? '-')
                          .toList(),
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

class _CompareHeader extends StatelessWidget {
  const _CompareHeader({required this.item, required this.remove});
  final CompareProduct item;
  final VoidCallback remove;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: 190,
    child: Padding(
      padding: const EdgeInsets.all(8),
      child: Column(
        children: [
          Stack(
            children: [
              SizedBox(
                height: 130,
                width: 180,
                child: ProductVisual(product: item.summary),
              ),
              Positioned(
                top: 3,
                left: 3,
                child: IconButton.filledTonal(
                  onPressed: remove,
                  icon: const Icon(Icons.close_rounded, size: 18),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            item.summary.nameAr,
            maxLines: 2,
            textAlign: TextAlign.center,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
        ],
      ),
    ),
  );
}

class _ComparisonRow extends StatelessWidget {
  const _ComparisonRow({
    required this.label,
    required this.values,
    this.highlighted = false,
  });
  final String label;
  final List<String> values;
  final bool highlighted;
  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: highlighted ? AppColors.primaryTint : Colors.white,
      border: const Border(bottom: BorderSide(color: AppColors.gray200)),
    ),
    child: Row(
      children: [
        SizedBox(
          width: 120,
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Text(
              label,
              style: const TextStyle(
                color: AppColors.gray600,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ),
        ...values.map(
          (value) => SizedBox(
            width: 190,
            child: Padding(
              padding: const EdgeInsets.all(12),
              child: Text(
                value,
                textAlign: TextAlign.center,
                style: TextStyle(
                  color: highlighted ? AppColors.primary : AppColors.gray800,
                  fontWeight: highlighted ? FontWeight.w800 : FontWeight.normal,
                ),
              ),
            ),
          ),
        ),
      ],
    ),
  );
}

class _EmptyCompare extends StatelessWidget {
  const _EmptyCompare();
  @override
  Widget build(BuildContext context) => const Center(
    child: Padding(
      padding: EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.compare_arrows_rounded,
            size: 78,
            color: AppColors.gray300,
          ),
          SizedBox(height: 14),
          Text(
            'لا توجد منتجات للمقارنة',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
          ),
          SizedBox(height: 6),
          Text(
            'اختر حتى أربعة منتجات من صفحة التفاصيل لمقارنتها جنبًا إلى جنب',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.gray500, height: 1.6),
          ),
        ],
      ),
    ),
  );
}
