import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'catalog_widgets.dart';

class ProductDetailScreen extends ConsumerStatefulWidget {
  const ProductDetailScreen({super.key, required this.idOrSlug});
  final String idOrSlug;

  @override
  ConsumerState<ProductDetailScreen> createState() =>
      _ProductDetailScreenState();
}

class _ProductDetailScreenState extends ConsumerState<ProductDetailScreen> {
  int _quantity = 1;
  String? _variantId;

  @override
  Widget build(BuildContext context) {
    final detail = ref.watch(productDetailProvider(widget.idOrSlug));
    return Scaffold(
      appBar: AppBar(
        title: const Text('تفاصيل المنتج'),
        actions: [
          IconButton(
            tooltip: 'إضافة للمقارنة',
            onPressed: _toggleCompare,
            icon: const Icon(Icons.compare_arrows_rounded),
          ),
          IconButton(onPressed: () {}, icon: const Icon(Icons.share_outlined)),
          IconButton(
            onPressed: () => _toggleFavorite(),
            icon: detail.value?.summary.isFavorite == true
                ? const Icon(Icons.favorite_rounded, color: AppColors.error)
                : const Icon(Icons.favorite_border_rounded),
          ),
        ],
      ),
      body: detail.when(
        loading: () => const CatalogLoading(message: 'جاري تحميل المنتج...'),
        error: (error, _) => CatalogError(
          error: error,
          retry: () => ref.invalidate(productDetailProvider(widget.idOrSlug)),
        ),
        data: (product) => _content(product),
      ),
      bottomNavigationBar: detail.value == null
          ? null
          : SafeArea(
              child: Container(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
                decoration: const BoxDecoration(
                  color: Colors.white,
                  border: Border(top: BorderSide(color: AppColors.gray200)),
                ),
                child: Row(
                  children: [
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: detail.value!.summary.stockQty <= 0
                            ? null
                            : () => ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text(
                                    'سيتم ربط الإضافة بالسلة في مرحلة Checkout',
                                  ),
                                ),
                              ),
                        icon: const Icon(Icons.shopping_cart_outlined),
                        label: Text(
                          detail.value!.summary.stockQty <= 0
                              ? 'غير متوفر حاليًا'
                              : 'إضافة للسلة',
                        ),
                      ),
                    ),
                    const SizedBox(width: 10),
                    OutlinedButton(
                      onPressed: _toggleCompare,
                      child: const Icon(Icons.compare_arrows_rounded),
                    ),
                  ],
                ),
              ),
            ),
    );
  }

  Widget _content(ProductDetail detail) {
    final product = detail.summary;
    if (_quantity < product.minOrderQty) _quantity = product.minOrderQty;
    final selectedVariant = detail.variants
        .where((variant) => variant.id == _variantId)
        .firstOrNull;
    final unitPrice = _tierPrice(
      detail,
      selectedVariant?.price ?? product.price,
    );
    final formatter = NumberFormat('#,##0.00', 'ar');
    return ListView(
      padding: const EdgeInsets.only(bottom: 20),
      children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: AspectRatio(
            aspectRatio: 1.3,
            child: ProductVisual(product: product, large: true),
          ),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  _StockChip(status: product.stockStatus),
                  const SizedBox(width: 7),
                  if (product.hasContractPrice)
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.successTint,
                        borderRadius: BorderRadius.circular(AppRadius.pill),
                      ),
                      child: const Text(
                        'سعر خاص لشركتك',
                        style: TextStyle(
                          color: AppColors.success,
                          fontSize: 9,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  const Spacer(),
                  Text(
                    product.sku,
                    style: const TextStyle(
                      color: AppColors.gray400,
                      fontSize: 9,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                product.nameAr,
                style: const TextStyle(
                  fontSize: 21,
                  height: 1.35,
                  color: AppColors.gray900,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 7),
              Row(
                children: [
                  const Icon(
                    Icons.star_rounded,
                    color: Color(0xFFF5A623),
                    size: 18,
                  ),
                  Text(
                    ' ${product.rating.toStringAsFixed(1)}',
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  Text(
                    ' (${product.ratingCount} تقييم)',
                    style: const TextStyle(
                      color: AppColors.gray500,
                      fontSize: 10,
                    ),
                  ),
                  const Spacer(),
                  Text(
                    product.brandName ?? product.categoryName,
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
              const Divider(height: 28),
              Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    formatter.format(unitPrice),
                    style: const TextStyle(
                      color: AppColors.primary,
                      fontSize: 27,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  const Padding(
                    padding: EdgeInsets.only(bottom: 5),
                    child: Text(
                      ' ج.م / للوحدة',
                      style: TextStyle(color: AppColors.primary, fontSize: 10),
                    ),
                  ),
                  if (product.compareAtPrice != null) ...[
                    const SizedBox(width: 9),
                    Padding(
                      padding: const EdgeInsets.only(bottom: 5),
                      child: Text(
                        formatter.format(product.compareAtPrice),
                        style: const TextStyle(
                          color: AppColors.gray400,
                          decoration: TextDecoration.lineThrough,
                        ),
                      ),
                    ),
                  ],
                ],
              ),
              const SizedBox(height: 4),
              Text(
                'الإجمالي: ${formatter.format(unitPrice * _quantity)} ج.م',
                style: const TextStyle(
                  color: AppColors.gray600,
                  fontWeight: FontWeight.w600,
                ),
              ),
              if (detail.variants.isNotEmpty) ...[
                const SizedBox(height: 22),
                const Text(
                  'اختر النوع / اللون',
                  style: TextStyle(fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 9),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: detail.variants
                      .map(
                        (variant) => ChoiceChip(
                          label: Text(variant.name),
                          selected: _variantId == variant.id,
                          onSelected: variant.stockQty <= 0
                              ? null
                              : (_) => setState(() => _variantId = variant.id),
                        ),
                      )
                      .toList(),
                ),
              ],
              const SizedBox(height: 22),
              Row(
                children: [
                  const Expanded(
                    child: Text(
                      'الكمية',
                      style: TextStyle(fontWeight: FontWeight.w800),
                    ),
                  ),
                  Container(
                    decoration: BoxDecoration(
                      border: Border.all(color: AppColors.gray200),
                      borderRadius: BorderRadius.circular(AppRadius.md),
                    ),
                    child: Row(
                      children: [
                        IconButton(
                          onPressed: _quantity > product.minOrderQty
                              ? () => setState(() => _quantity--)
                              : null,
                          icon: const Icon(Icons.remove_rounded),
                        ),
                        SizedBox(
                          width: 42,
                          child: Text(
                            '$_quantity',
                            textAlign: TextAlign.center,
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                        IconButton(
                          onPressed: product.stockQty > _quantity
                              ? () => setState(() => _quantity++)
                              : null,
                          icon: const Icon(Icons.add_rounded),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              if (detail.priceTiers.length > 1) ...[
                const SizedBox(height: 20),
                _PriceTiers(tiers: detail.priceTiers, quantity: _quantity),
              ],
              const SizedBox(height: 18),
              _InfoRow(
                icon: Icons.local_shipping_outlined,
                title: 'موعد التوصيل المتوقع',
                value: 'خلال ${detail.deliveryDays} أيام عمل',
              ),
              _InfoRow(
                icon: Icons.verified_user_outlined,
                title: 'الضمان والجودة',
                value: detail.warranty ?? 'ضمان جودة المنتج',
              ),
              const SizedBox(height: 20),
              _ExpandableSection(
                title: 'وصف المنتج',
                child: Text(
                  detail.description ?? 'لا يوجد وصف متاح',
                  style: const TextStyle(color: AppColors.gray600, height: 1.7),
                ),
              ),
              if (detail.documents.isNotEmpty)
                _ExpandableSection(
                  title: 'مستندات المنتج',
                  child: Column(
                    children: detail.documents
                        .map(
                          (document) => ListTile(
                            contentPadding: EdgeInsets.zero,
                            leading: Container(
                              width: 40,
                              height: 40,
                              decoration: BoxDecoration(
                                color: AppColors.errorTint,
                                borderRadius: BorderRadius.circular(
                                  AppRadius.sm,
                                ),
                              ),
                              child: const Icon(
                                Icons.picture_as_pdf_outlined,
                                color: AppColors.error,
                              ),
                            ),
                            title: Text(document.name),
                            subtitle: const Text(
                              'PDF - ورقة مواصفات فنية',
                              style: TextStyle(
                                color: AppColors.gray500,
                                fontSize: 9,
                              ),
                            ),
                            trailing: const Icon(
                              Icons.download_rounded,
                              color: AppColors.primary,
                            ),
                            onTap: () => ScaffoldMessenger.of(context).showSnackBar(
                              const SnackBar(
                                content: Text(
                                  'المستند التجريبي مسجل وسيُربط بالتخزين عند توفير الملف الأصلي',
                                ),
                              ),
                            ),
                          ),
                        )
                        .toList(),
                  ),
                ),
              _ExpandableSection(
                title: 'المواصفات الفنية',
                child: Column(
                  children: detail.attributes
                      .map(
                        (attribute) => Container(
                          padding: const EdgeInsets.symmetric(vertical: 9),
                          decoration: const BoxDecoration(
                            border: Border(
                              bottom: BorderSide(color: AppColors.gray150),
                            ),
                          ),
                          child: Row(
                            children: [
                              Expanded(
                                child: Text(
                                  attribute.name,
                                  style: const TextStyle(
                                    color: AppColors.gray500,
                                  ),
                                ),
                              ),
                              Expanded(
                                child: Text(
                                  attribute.value,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      )
                      .toList(),
                ),
              ),
              if (detail.related.isNotEmpty) ...[
                const SizedBox(height: 24),
                const Text(
                  'منتجات مشابهة',
                  style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 12),
                SizedBox(
                  height: 300,
                  child: ListView.separated(
                    scrollDirection: Axis.horizontal,
                    itemCount: detail.related.length,
                    separatorBuilder: (_, __) => const SizedBox(width: 10),
                    itemBuilder: (context, index) => SizedBox(
                      width: 180,
                      child: CatalogProductCard(product: detail.related[index]),
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }

  double _tierPrice(ProductDetail detail, double base) {
    var price = base;
    for (final tier in detail.priceTiers) {
      if (_quantity >= tier.minQty) price = tier.unitPrice;
    }
    return price;
  }

  Future<void> _toggleFavorite() async {
    try {
      await ref
          .read(catalogRepositoryProvider)
          .toggleFavorite(
            ref.read(productDetailProvider(widget.idOrSlug)).value!.summary.id,
          );
      ref.invalidate(productDetailProvider(widget.idOrSlug));
      ref.invalidate(productFeedProvider);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.toString())));
      }
    }
  }

  Future<void> _toggleCompare() async {
    final product = ref.read(productDetailProvider(widget.idOrSlug)).value;
    if (product == null) return;
    try {
      final added = await ref
          .read(catalogRepositoryProvider)
          .toggleCompare(product.summary.id);
      ref.invalidate(compareProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              added
                  ? 'تمت إضافة المنتج للمقارنة'
                  : 'تمت إزالة المنتج من المقارنة',
            ),
            action: added
                ? SnackBarAction(
                    label: 'عرض المقارنة',
                    onPressed: () => context.push('/compare'),
                  )
                : null,
          ),
        );
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.toString())));
      }
    }
  }
}

class _StockChip extends StatelessWidget {
  const _StockChip({required this.status});
  final String status;
  @override
  Widget build(BuildContext context) {
    final available = status != 'OutOfStock';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
      decoration: BoxDecoration(
        color: available ? AppColors.successTint : AppColors.errorTint,
        borderRadius: BorderRadius.circular(AppRadius.pill),
      ),
      child: Text(
        status == 'OutOfStock'
            ? 'غير متوفر'
            : status == 'LowStock'
            ? 'كمية محدودة'
            : 'متوفر',
        style: TextStyle(
          color: available ? AppColors.success : AppColors.error,
          fontSize: 9,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _PriceTiers extends StatelessWidget {
  const _PriceTiers({required this.tiers, required this.quantity});
  final List<PriceTier> tiers;
  final int quantity;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: AppColors.primaryTint,
      borderRadius: BorderRadius.circular(AppRadius.lg),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Row(
          children: [
            Icon(Icons.savings_outlined, color: AppColors.primary, size: 20),
            SizedBox(width: 7),
            Text(
              'وفر أكثر مع الكميات',
              style: TextStyle(
                color: AppColors.primary,
                fontWeight: FontWeight.w800,
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        ...tiers.map(
          (tier) => Padding(
            padding: const EdgeInsets.symmetric(vertical: 3),
            child: Row(
              children: [
                Icon(
                  quantity >= tier.minQty
                      ? Icons.check_circle_rounded
                      : Icons.circle_outlined,
                  color: quantity >= tier.minQty
                      ? AppColors.success
                      : AppColors.gray400,
                  size: 16,
                ),
                const SizedBox(width: 7),
                Expanded(child: Text('من ${tier.minQty} قطعة')),
                Text(
                  '${tier.unitPrice.toStringAsFixed(2)} ج.م',
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
              ],
            ),
          ),
        ),
      ],
    ),
  );
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({
    required this.icon,
    required this.title,
    required this.value,
  });
  final IconData icon;
  final String title;
  final String value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 8),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 38,
          height: 38,
          decoration: BoxDecoration(
            color: AppColors.gray50,
            borderRadius: BorderRadius.circular(AppRadius.sm),
          ),
          child: Icon(icon, size: 20, color: AppColors.primary),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                value,
                style: const TextStyle(fontSize: 10, color: AppColors.gray500),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

class _ExpandableSection extends StatelessWidget {
  const _ExpandableSection({required this.title, required this.child});
  final String title;
  final Widget child;
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 9),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.md),
      border: Border.all(color: AppColors.gray200),
    ),
    child: ExpansionTile(
      initiallyExpanded: true,
      shape: const Border(),
      title: Text(title, style: const TextStyle(fontWeight: FontWeight.w800)),
      children: [Padding(padding: const EdgeInsets.all(14), child: child)],
    ),
  );
}
