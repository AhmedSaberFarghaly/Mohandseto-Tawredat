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
          IconButton(
            tooltip: 'السلال المحفوظة',
            icon: const Icon(Icons.inventory_2_outlined),
            onPressed: () => _showSavedCarts(context, ref),
          ),
          if (cart.value?.items.isNotEmpty == true)
            IconButton(
              tooltip: 'حفظ السلة',
              icon: const Icon(Icons.bookmark_add_outlined),
              onPressed: () => _saveCart(context, ref),
            ),
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

  Widget _content(
    BuildContext context,
    WidgetRef ref,
    CartModel cart,
  ) => RefreshIndicator(
    onRefresh: () async {
      ref.invalidate(cartProvider);
      await ref.read(cartProvider.future);
    },
    child: ListView(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 130),
      children: [
        if (cart.items.isNotEmpty)
          ...cart.items.map((item) => _itemCard(context, ref, item)),
        if (cart.hasPriceChanges)
          _warning(
            'تغيّر سعر صنف أو أكثر منذ إضافته. راجع السعر الجديد ثم أكّده للمتابعة.',
            Icons.price_change_outlined,
            action: TextButton(
              onPressed: () => _mutate(
                context,
                ref,
                () => ref.read(cartRepositoryProvider).acknowledgePrices(),
              ),
              child: const Text('تأكيد الأسعار'),
            ),
          ),
        if (cart.hasAvailabilityIssues)
          _warning(
            'بعض الكميات لم تعد متاحة. عدّل الكمية أو احذف الصنف قبل إتمام الطلب.',
            Icons.inventory_outlined,
          ),
        if (cart.items.isNotEmpty) _coupon(context, ref, cart),
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

  Widget _itemCard(
    BuildContext context,
    WidgetRef ref,
    CartItemModel item,
  ) => Card(
    margin: const EdgeInsets.only(bottom: 10),
    child: Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        children: [
          InkWell(
            onTap: () => context.push(
              item.customProductRequestId == null
                  ? '/products/${item.slug}'
                  : '/custom-requests/${item.customProductRequestId}',
            ),
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
                  child: Icon(
                    item.customProductRequestId == null
                        ? Icons.inventory_2_outlined
                        : Icons.print_outlined,
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
                      if (item.customProductRequestId != null)
                        const Text(
                          'منتج مخصص — السعر والكمية حسب العرض',
                          style: TextStyle(
                            color: AppColors.success,
                            fontSize: 9,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      Text(
                        item.sku,
                        style: const TextStyle(
                          color: AppColors.gray400,
                          fontSize: 9,
                        ),
                      ),
                      if (item.priceChanged)
                        Text(
                          'كان ${_money(item.previousUnitPrice ?? 0)} وأصبح ${_money(item.unitPrice)} ج.م',
                          style: const TextStyle(
                            color: AppColors.warning,
                            fontSize: 9,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      if (item.hasAvailabilityIssue)
                        Text(
                          'المتاح الآن ${item.availableQty} فقط',
                          style: const TextStyle(
                            color: AppColors.error,
                            fontSize: 9,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      if (item.customerNote != null)
                        Text(
                          'ملاحظة: ${item.customerNote}',
                          style: const TextStyle(
                            color: AppColors.gray500,
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
              if (!item.saved && item.customProductRequestId == null)
                _quantity(context, ref, item)
              else if (!item.saved)
                Text(
                  '${item.quantity} قطعة',
                  style: const TextStyle(fontWeight: FontWeight.w800),
                )
              else
                const Spacer(),
              TextButton(
                onPressed: () => _itemNote(context, ref, item),
                child: const Text('ملاحظة'),
              ),
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
          if (cart.savings - cart.couponDiscount > 0)
            _line(
              'وفر أسعار الكميات',
              -(cart.savings - cart.couponDiscount),
              color: AppColors.primary,
            ),
          if (cart.couponDiscount > 0)
            _line(
              'خصم الكوبون ${cart.couponCode ?? ''}',
              -cart.couponDiscount,
              color: AppColors.success,
            ),
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
              onPressed: cart.hasPriceChanges || cart.hasAvailabilityIssues
                  ? null
                  : () => context.push('/checkout'),
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

  Widget _warning(String message, IconData icon, {Widget? action}) => Container(
    margin: const EdgeInsets.only(bottom: 10),
    padding: const EdgeInsets.all(11),
    decoration: BoxDecoration(
      color: AppColors.warningTint,
      borderRadius: BorderRadius.circular(AppRadius.md),
      border: Border.all(color: AppColors.warning.withValues(alpha: .25)),
    ),
    child: Row(
      children: [
        Icon(icon, color: AppColors.warning),
        const SizedBox(width: 8),
        Expanded(child: Text(message, style: const TextStyle(fontSize: 10))),
        if (action != null) action,
      ],
    ),
  );

  Widget _coupon(BuildContext context, WidgetRef ref, CartModel cart) {
    var couponCode = cart.couponCode ?? '';
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            Expanded(
              child: TextFormField(
                initialValue: couponCode,
                onChanged: (value) => couponCode = value,
                textCapitalization: TextCapitalization.characters,
                decoration: InputDecoration(
                  labelText: cart.couponCode == null
                      ? 'كود الخصم'
                      : 'تم تطبيق ${cart.couponCode}',
                  prefixIcon: const Icon(Icons.local_offer_outlined),
                ),
              ),
            ),
            const SizedBox(width: 8),
            FilledButton(
              onPressed: () => _mutate(
                context,
                ref,
                () => cart.couponCode == null
                    ? ref
                          .read(cartRepositoryProvider)
                          .applyCoupon(couponCode.trim())
                    : ref.read(cartRepositoryProvider).removeCoupon(),
              ),
              child: Text(cart.couponCode == null ? 'تطبيق' : 'إزالة'),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _itemNote(
    BuildContext context,
    WidgetRef ref,
    CartItemModel item,
  ) async {
    final controller = TextEditingController(text: item.customerNote ?? '');
    final save = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text('ملاحظة على ${item.name}'),
        content: TextField(
          controller: controller,
          maxLength: 500,
          maxLines: 4,
          decoration: const InputDecoration(
            hintText: 'مثال: لون التغليف أو تعليمات خاصة بالصنف',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text('إلغاء'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: const Text('حفظ'),
          ),
        ],
      ),
    );
    if (save == true && context.mounted) {
      await _mutate(
        context,
        ref,
        () => ref
            .read(cartRepositoryProvider)
            .itemNote(
              item.id,
              controller.text.trim().isEmpty ? null : controller.text.trim(),
            ),
      );
    }
    controller.dispose();
  }

  Future<void> _saveCart(BuildContext context, WidgetRef ref) async {
    final controller = TextEditingController();
    final save = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('حفظ السلة الحالية'),
        content: TextField(
          controller: controller,
          maxLength: 100,
          decoration: const InputDecoration(
            labelText: 'اسم السلة (اختياري)',
            hintText: 'مثال: احتياجات فرع المعادي - يوليو',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialogContext, false),
            child: const Text('إلغاء'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(dialogContext, true),
            child: const Text('حفظ'),
          ),
        ],
      ),
    );
    if (save == true) {
      try {
        await ref
            .read(cartRepositoryProvider)
            .saveCart(
              controller.text.trim().isEmpty ? null : controller.text.trim(),
            );
        ref.invalidate(cartProvider);
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('تم حفظ السلة ويمكن استعادتها في أي وقت'),
            ),
          );
        }
      } catch (error) {
        if (context.mounted) {
          ScaffoldMessenger.of(
            context,
          ).showSnackBar(SnackBar(content: Text('$error')));
        }
      }
    }
    controller.dispose();
  }

  Future<void> _showSavedCarts(BuildContext context, WidgetRef ref) async {
    try {
      final carts = await ref.read(cartRepositoryProvider).savedCarts();
      if (!context.mounted) return;
      await showModalBottomSheet<void>(
        context: context,
        showDragHandle: true,
        builder: (sheetContext) => SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'السلال المحفوظة',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w900),
                ),
                const SizedBox(height: 10),
                if (carts.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 30),
                    child: Center(child: Text('لا توجد سلال محفوظة بعد')),
                  )
                else
                  ...carts.map(
                    (saved) => ListTile(
                      leading: const CircleAvatar(
                        child: Icon(Icons.shopping_basket_outlined),
                      ),
                      title: Text(saved.name),
                      subtitle: Text(
                        '${saved.itemCount} أصناف — ${_money(saved.estimatedTotal)} ج.م\n${DateFormat('d MMM yyyy، HH:mm', 'ar').format(saved.savedAt.toLocal())}',
                      ),
                      isThreeLine: true,
                      trailing: FilledButton.tonal(
                        onPressed: () async {
                          await ref
                              .read(cartRepositoryProvider)
                              .restoreCart(saved.id);
                          ref.invalidate(cartProvider);
                          if (sheetContext.mounted) Navigator.pop(sheetContext);
                        },
                        child: const Text('استعادة'),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ),
      );
    } catch (error) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$error')));
      }
    }
  }

  String _money(double value) => NumberFormat('#,##0.00', 'ar').format(value);
}
