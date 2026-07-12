import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/api/cart_repository.dart';
import '../../core/theme/app_tokens.dart';

class CartScreen extends ConsumerWidget {
  const CartScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cart = ref.watch(cartProvider);
    return Scaffold(
      appBar: AppBar(
        title: Text('السلة (${cart.value?.itemCount ?? 0})'),
        actions: [
          if (cart.value?.items.isNotEmpty == true)
            IconButton(
              tooltip: 'إفراغ السلة',
              icon: const Icon(Icons.delete_outline_rounded),
              onPressed: () async {
                if (await _confirm(context, 'هل تريد حذف كل عناصر السلة؟') !=
                    true) {
                  return;
                }
                await ref.read(cartRepositoryProvider).clear();
                ref.invalidate(cartProvider);
              },
            ),
        ],
      ),
      body: cart.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(
                  Icons.cloud_off_rounded,
                  size: 52,
                  color: AppColors.gray400,
                ),
                const SizedBox(height: 12),
                Text('$error', textAlign: TextAlign.center),
                const SizedBox(height: 12),
                OutlinedButton(
                  onPressed: () => ref.invalidate(cartProvider),
                  child: const Text('إعادة المحاولة'),
                ),
              ],
            ),
          ),
        ),
        data: (data) => data.items.isEmpty && data.savedItems.isEmpty
            ? _empty(context)
            : _content(context, ref, data),
      ),
      bottomNavigationBar: cart.value?.items.isNotEmpty == true
          ? _bottom(context, cart.value!)
          : null,
    );
  }

  Widget _empty(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 90,
            height: 90,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              color: AppColors.primaryTint,
            ),
            child: const Icon(
              Icons.shopping_cart_outlined,
              size: 45,
              color: AppColors.primary,
            ),
          ),
          const SizedBox(height: 18),
          const Text(
            'سلتك فارغة',
            style: TextStyle(fontSize: 21, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 7),
          const Text(
            'ابدأ من الأقسام أو من طلباتك السابقة وأضف احتياجات شركتك',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.gray500, height: 1.6),
          ),
          const SizedBox(height: 18),
          FilledButton.icon(
            onPressed: () => context.go('/categories'),
            icon: const Icon(Icons.grid_view_rounded),
            label: const Text('تصفح الأقسام'),
          ),
        ],
      ),
    ),
  );

  Widget _content(BuildContext context, WidgetRef ref, CartModel cart) =>
      RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(cartProvider);
          await ref.read(cartProvider.future);
        },
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 10, 16, 130),
          children: [
            if (cart.items.isNotEmpty)
              ...cart.items.map((item) => _itemCard(context, ref, item)),
            if (cart.savings > 0)
              Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.warningTint,
                  borderRadius: BorderRadius.circular(AppRadius.md),
                ),
                child: Text(
                  'وفّرت ${_money(cart.savings)} ج.م بفضل أسعار الكميات والعقد',
                  style: const TextStyle(
                    color: AppColors.warning,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
            if (cart.items.isNotEmpty) _summary(cart),
            if (cart.savedItems.isNotEmpty) ...[
              const Padding(
                padding: EdgeInsets.only(top: 22, bottom: 10),
                child: Text(
                  'محفوظ لوقت لاحق',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
                ),
              ),
              ...cart.savedItems.map((item) => _itemCard(context, ref, item)),
            ],
          ],
        ),
      );

  Widget _itemCard(BuildContext context, WidgetRef ref, CartItemModel item) =>
      Card(
        margin: const EdgeInsets.only(bottom: 10),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            children: [
              InkWell(
                onTap: () => context.push('/products/${item.slug}'),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      width: 58,
                      height: 58,
                      decoration: BoxDecoration(
                        color: AppColors.primaryTint,
                        borderRadius: BorderRadius.circular(AppRadius.md),
                      ),
                      child: const Icon(
                        Icons.inventory_2_outlined,
                        color: AppColors.primary,
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            item.name,
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontWeight: FontWeight.w800),
                          ),
                          if (item.variantName != null)
                            Text(
                              item.variantName!,
                              style: const TextStyle(
                                color: AppColors.primary,
                                fontSize: 10,
                              ),
                            ),
                          Text(
                            item.sku,
                            style: const TextStyle(
                              color: AppColors.gray400,
                              fontSize: 9,
                            ),
                          ),
                        ],
                      ),
                    ),
                    Text(
                      '${_money(item.lineTotal)} ج.م',
                      style: const TextStyle(
                        color: AppColors.primary,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ],
                ),
              ),
              const Divider(height: 20),
              Row(
                children: [
                  if (!item.saved)
                    _quantity(context, ref, item)
                  else
                    const Spacer(),
                  TextButton(
                    onPressed: () => _mutate(
                      context,
                      ref,
                      () => ref
                          .read(cartRepositoryProvider)
                          .save(item.id, !item.saved),
                    ),
                    child: Text(item.saved ? 'إعادة للسلة' : 'حفظ لوقت لاحق'),
                  ),
                  IconButton(
                    tooltip: 'حذف',
                    icon: const Icon(
                      Icons.delete_outline_rounded,
                      color: AppColors.error,
                    ),
                    onPressed: () => _mutate(
                      context,
                      ref,
                      () => ref.read(cartRepositoryProvider).remove(item.id),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      );

  Widget _quantity(BuildContext context, WidgetRef ref, CartItemModel item) =>
      Container(
        decoration: BoxDecoration(
          border: Border.all(color: AppColors.gray200),
          borderRadius: BorderRadius.circular(AppRadius.sm),
        ),
        child: Row(
          children: [
            IconButton(
              iconSize: 18,
              onPressed: item.quantity > item.minOrderQty
                  ? () => _mutate(
                      context,
                      ref,
                      () => ref
                          .read(cartRepositoryProvider)
                          .update(item.id, item.quantity - 1),
                    )
                  : null,
              icon: const Icon(Icons.remove),
            ),
            Text(
              '${item.quantity}',
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
            IconButton(
              iconSize: 18,
              onPressed: item.quantity < item.availableQty
                  ? () => _mutate(
                      context,
                      ref,
                      () => ref
                          .read(cartRepositoryProvider)
                          .update(item.id, item.quantity + 1),
                    )
                  : null,
              icon: const Icon(Icons.add),
            ),
          ],
        ),
      );

  Widget _summary(CartModel cart) => Card(
    child: Padding(
      padding: const EdgeInsets.all(15),
      child: Column(
        children: [
          _line(
            'إجمالي الأصناف (${cart.totalQuantity} قطعة)',
            cart.subtotalBeforeSavings,
          ),
          if (cart.savings > 0)
            _line('وفر أسعار الكميات', -cart.savings, color: AppColors.primary),
          _line(
            'ضريبة القيمة المضافة (مشمولة)',
            cart.taxIncluded,
            subdued: true,
          ),
          _line('الشحن', cart.shipping, free: cart.shipping == 0),
          const Divider(height: 22),
          _line('إجمالي الطلب', cart.total, strong: true),
          if (!cart.eligibleForFreeShipping && cart.subtotal > 0)
            Padding(
              padding: const EdgeInsets.only(top: 10),
              child: Text(
                'أضف مشتريات بقيمة ${_money(2000 - cart.subtotal)} ج.م للحصول على شحن مجاني',
                style: const TextStyle(color: AppColors.primary, fontSize: 10),
              ),
            ),
        ],
      ),
    ),
  );

  Widget _line(
    String label,
    double value, {
    Color? color,
    bool subdued = false,
    bool strong = false,
    bool free = false,
  }) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: TextStyle(
              color: subdued ? AppColors.gray500 : AppColors.gray800,
              fontWeight: strong ? FontWeight.w800 : FontWeight.w400,
            ),
          ),
        ),
        Text(
          free ? 'مجاني' : '${value < 0 ? '-' : ''}${_money(value.abs())} ج.م',
          style: TextStyle(
            color: free ? AppColors.primary : color,
            fontSize: strong ? 16 : 13,
            fontWeight: strong ? FontWeight.w900 : FontWeight.w700,
          ),
        ),
      ],
    ),
  );

  Widget _bottom(BuildContext context, CartModel cart) => SafeArea(
    child: Container(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: AppColors.gray200)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'الإجمالي',
                  style: TextStyle(color: AppColors.gray500, fontSize: 9),
                ),
                Text(
                  '${_money(cart.total)} ج.م',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: FilledButton(
              onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('جاري تنفيذ خطوة التوصيل التالية'),
                ),
              ),
              child: const Text('متابعة إتمام الطلب'),
            ),
          ),
        ],
      ),
    ),
  );

  Future<void> _mutate(
    BuildContext context,
    WidgetRef ref,
    Future<CartModel> Function() action,
  ) async {
    try {
      await action();
      ref.invalidate(cartProvider);
    } catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$error')));
      }
    }
  }

  Future<bool?> _confirm(BuildContext context, String message) =>
      showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('تأكيد'),
          content: Text(message),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('إلغاء'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('تأكيد'),
            ),
          ],
        ),
      );
  String _money(double value) => NumberFormat('#,##0.00', 'ar').format(value);
}
