import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/widgets/skeleton.dart';
import '../../core/api/return_repository.dart';
import '../../core/theme/app_tokens.dart';

class ReturnsScreen extends ConsumerStatefulWidget {
  const ReturnsScreen({super.key});
  @override
  ConsumerState<ReturnsScreen> createState() => _ReturnsScreenState();
}

class _ReturnsScreenState extends ConsumerState<ReturnsScreen> {
  String? status;
  @override
  Widget build(BuildContext context) {
    final result = ref.watch(returnListProvider(status));
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: const Text('مركز المرتجعات')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/returns/new'),
        icon: const Icon(Icons.add),
        label: const Text('طلب إرجاع'),
      ),
      body: Column(
        children: [
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 5),
              children: [
                _filter('الكل', null),
                _filter('قيد المراجعة', 'Submitted'),
                _filter('تمت الموافقة', 'Approved'),
                _filter('قيد الاستلام', 'InTransit'),
                _filter('مكتمل', 'Completed'),
              ],
            ),
          ),
          Expanded(
            child: result.when(
              loading: () => const ListSkeleton(),
              error: (e, _) => Center(child: Text('$e')),
              data: (items) => items.isEmpty
                  ? const _EmptyReturns()
                  : RefreshIndicator(
                      onRefresh: () async {
                        ref.invalidate(returnListProvider(status));
                        await ref.read(returnListProvider(status).future);
                      },
                      child: ListView.builder(
                        padding: const EdgeInsets.fromLTRB(14, 8, 14, 100),
                        itemCount: items.length,
                        itemBuilder: (_, i) => _ReturnCard(item: items[i]),
                      ),
                    ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _filter(String text, String? value) => Padding(
    padding: const EdgeInsetsDirectional.only(end: 7),
    child: ChoiceChip(
      label: Text(text),
      selected: status == value,
      onSelected: (_) => setState(() => status = value),
    ),
  );
}

class _ReturnCard extends StatelessWidget {
  const _ReturnCard({required this.item});
  final ReturnListItem item;
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 10),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: InkWell(
      borderRadius: BorderRadius.circular(AppRadius.xl),
      onTap: () => context.push('/returns/${item.id}'),
      child: Padding(
        padding: const EdgeInsets.all(15),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.number,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      Text(
                        'الطلب ${item.orderNumber}',
                        style: const TextStyle(
                          fontSize: 10.5,
                          color: AppColors.gray500,
                        ),
                      ),
                    ],
                  ),
                ),
                ReturnStatusChip(status: item.status),
              ],
            ),
            const Divider(height: 22),
            Row(
              children: [
                Icon(
                  item.resolution == 'Refund'
                      ? Icons.currency_exchange
                      : Icons.swap_horiz,
                  size: 18,
                  color: AppColors.primary,
                ),
                const SizedBox(width: 5),
                Expanded(
                  child: Text(
                    '${resolutionAr(item.resolution)} • ${item.itemCount} أصناف',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 11),
                  ),
                ),
                Text(
                  '${money(item.approvedTotal ?? item.requestedTotal)} ج.م',
                  style: const TextStyle(
                    fontWeight: FontWeight.w700,
                    color: AppColors.primary,
                  ),
                ),
              ],
            ),
            if (item.pickupAt != null)
              Padding(
                padding: const EdgeInsets.only(top: 9),
                child: Row(
                  children: [
                    const Icon(
                      Icons.local_shipping_outlined,
                      size: 15,
                      color: AppColors.warning,
                    ),
                    Text(
                      ' الاستلام ${DateFormat('d MMM، h:mm a', 'ar').format(item.pickupAt!.toLocal())}',
                      style: const TextStyle(
                        fontSize: 10.5,
                        color: AppColors.gray600,
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    ),
  );
}

class _Selection {
  _Selection();
  int quantity = 1;
  String reason = 'Damaged', description = '';
}

class CreateReturnScreen extends ConsumerStatefulWidget {
  const CreateReturnScreen({super.key});
  @override
  ConsumerState<CreateReturnScreen> createState() => _CreateReturnScreenState();
}

class _CreateReturnScreenState extends ConsumerState<CreateReturnScreen> {
  int step = 0;
  bool busy = false;
  EligibleReturnOrder? order;
  final selected = <String, _Selection>{};
  final photos = <PlatformFile>[];
  String resolution = 'Refund', refundMethod = 'OriginalPayment';
  final address = TextEditingController();
  @override
  void dispose() {
    address.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(
      title: Text(
        [
          'اختيار الطلب',
          'اختيار المنتجات',
          'الحالة والصور',
          'طريقة التسوية',
          'مراجعة الطلب',
        ][step],
      ),
    ),
    body: Column(
      children: [
        Container(
          margin: const EdgeInsets.fromLTRB(16, 10, 16, 8),
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(AppRadius.lg),
            border: Border.all(color: AppColors.gray150),
          ),
          child: Row(
            children: List.generate(
              5,
              (i) => Expanded(
                child: Container(
                  height: 4,
                  margin: const EdgeInsets.symmetric(horizontal: 2),
                  decoration: BoxDecoration(
                    color: i <= step ? AppColors.primary : AppColors.gray200,
                    borderRadius: BorderRadius.circular(4),
                  ),
                ),
              ),
            ),
          ),
        ),
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 110),
            child: _content(),
          ),
        ),
      ],
    ),
    bottomNavigationBar: SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          children: [
            if (step > 0)
              Expanded(
                child: OutlinedButton(
                  onPressed: busy ? null : () => setState(() => step--),
                  child: const Text('السابق'),
                ),
              ),
            if (step > 0) const SizedBox(width: 8),
            Expanded(
              flex: 2,
              child: FilledButton(
                onPressed: busy || !_valid
                    ? null
                    : step == 4
                    ? _submit
                    : () => setState(() => step++),
                child: busy
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : Text(step == 4 ? 'إرسال طلب الإرجاع' : 'متابعة'),
              ),
            ),
          ],
        ),
      ),
    ),
  );
  bool get _valid => switch (step) {
    0 => order != null,
    1 => selected.isNotEmpty,
    2 => !selected.values.any(
      (x) => x.reason == 'Other' && x.description.trim().isEmpty,
    ),
    3 => address.text.trim().isNotEmpty,
    _ => true,
  };
  Widget _content() => switch (step) {
    0 => _orders(),
    1 => _items(),
    2 => _evidence(),
    3 => _resolution(),
    _ => _review(),
  };

  Widget _orders() => ref
      .watch(eligibleReturnsProvider)
      .when(
        loading: () => const ListSkeleton(),
        error: (e, _) => Center(child: Text('$e')),
        data: (orders) {
          if (orders.isEmpty) {
            return const _Hint(
              icon: Icons.event_busy,
              title: 'لا توجد طلبات مؤهلة',
              body: 'الإرجاع متاح خلال 30 يومًا من الاستلام',
            );
          }
          return Column(
            children: orders
                .map(
                  (o) => Card(
                    color: order?.id == o.id ? AppColors.primaryTint : null,
                    child: RadioListTile<String>(
                      value: o.id,
                      groupValue: order?.id,
                      onChanged: (_) => setState(() {
                        order = o;
                        address.text = o.address;
                        selected.clear();
                      }),
                      title: Text(
                        o.number,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      subtitle: Text(
                        'تم الاستلام ${DateFormat('d MMMM yyyy', 'ar').format(o.deliveredAt)} • متاح حتى ${DateFormat('d MMM').format(o.eligibleUntil)}',
                      ),
                      secondary: const Icon(Icons.receipt_long_outlined),
                    ),
                  ),
                )
                .toList(),
          );
        },
      );

  Widget _items() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      const _Hint(
        icon: Icons.inventory_2_outlined,
        title: 'اختر المنتجات والكميات',
        body: 'يمكنك إرجاع جزء من الكمية المستلمة',
      ),
      const SizedBox(height: 10),
      ...order!.items.map((item) {
        final choice = selected[item.id];
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              children: [
                CheckboxListTile(
                  contentPadding: EdgeInsets.zero,
                  value: choice != null,
                  onChanged: (v) => setState(() {
                    if (v == true) {
                      selected[item.id] = _Selection();
                    } else {
                      selected.remove(item.id);
                    }
                  }),
                  title: Text(
                    item.name,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  subtitle: Text(
                    '${item.sku} • المتاح ${item.eligible} من ${item.ordered}',
                  ),
                ),
                if (choice != null)
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      IconButton(
                        onPressed: choice.quantity > 1
                            ? () => setState(() => choice.quantity--)
                            : null,
                        icon: const Icon(Icons.remove_circle_outline),
                      ),
                      Text(
                        '${choice.quantity}',
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      IconButton(
                        onPressed: choice.quantity < item.eligible
                            ? () => setState(() => choice.quantity++)
                            : null,
                        icon: const Icon(Icons.add_circle_outline),
                      ),
                      const Spacer(),
                      Text(
                        '${money(choice.quantity * item.unitPrice)} ج.م',
                        style: const TextStyle(
                          color: AppColors.primary,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        );
      }),
    ],
  );

  Widget _evidence() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      ...selected.entries.map((entry) {
        final item = order!.items.firstWhere((x) => x.id == entry.key),
            choice = entry.value;
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(13),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  item.name,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 9),
                DropdownButtonFormField(
                  value: choice.reason,
                  decoration: const InputDecoration(labelText: 'سبب الإرجاع'),
                  items: returnReasons.entries
                      .map(
                        (x) => DropdownMenuItem(
                          value: x.key,
                          child: Text(x.value),
                        ),
                      )
                      .toList(),
                  onChanged: (v) => setState(() => choice.reason = v!),
                ),
                const SizedBox(height: 9),
                TextFormField(
                  initialValue: choice.description,
                  maxLines: 3,
                  onChanged: (v) => choice.description = v,
                  decoration: const InputDecoration(labelText: 'وصف المشكلة'),
                ),
              ],
            ),
          ),
        );
      }),
      const SizedBox(height: 10),
      OutlinedButton.icon(
        onPressed: () async {
          final result = await FilePicker.pickFiles(
            type: FileType.image,
            allowMultiple: true,
            withData: true,
          );
          if (result != null) {
            setState(() {
              photos.clear();
              photos.addAll(result.files.take(5));
            });
          }
        },
        icon: const Icon(Icons.add_a_photo_outlined),
        label: Text(
          photos.isEmpty ? 'إرفاق صور الحالة' : '${photos.length} صور مرفقة',
        ),
      ),
      const Padding(
        padding: EdgeInsets.only(top: 6),
        child: Text(
          'الصور مطلوبة عند اختيار تالف أو مشكلة جودة • حد أقصى 5 صور',
          style: TextStyle(fontSize: 10.5, color: AppColors.gray500),
        ),
      ),
      if (photos.isNotEmpty)
        Wrap(
          spacing: 7,
          children: photos
              .map(
                (p) => Chip(
                  label: Text(p.name, overflow: TextOverflow.ellipsis),
                  onDeleted: () => setState(() => photos.remove(p)),
                ),
              )
              .toList(),
        ),
    ],
  );

  Widget _resolution() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      const Text(
        'ماذا تفضل؟',
        style: TextStyle(fontWeight: FontWeight.w700, fontSize: 17),
      ),
      const SizedBox(height: 10),
      Row(
        children: [
          Expanded(
            child: _Choice(
              title: 'استرداد المبلغ',
              icon: Icons.currency_exchange,
              selected: resolution == 'Refund',
              tap: () => setState(() => resolution = 'Refund'),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: _Choice(
              title: 'استبدال المنتج',
              icon: Icons.swap_horiz,
              selected: resolution == 'Replacement',
              tap: () => setState(() => resolution = 'Replacement'),
            ),
          ),
        ],
      ),
      if (resolution == 'Refund') ...[
        const SizedBox(height: 14),
        DropdownButtonFormField(
          value: refundMethod,
          decoration: const InputDecoration(labelText: 'طريقة استرداد المبلغ'),
          items: const [
            DropdownMenuItem(
              value: 'OriginalPayment',
              child: Text('نفس وسيلة الدفع'),
            ),
            DropdownMenuItem(value: 'BankTransfer', child: Text('تحويل بنكي')),
            DropdownMenuItem(
              value: 'CreditBalance',
              child: Text('رصيد دائن للشركة'),
            ),
          ],
          onChanged: (v) => setState(() => refundMethod = v!),
        ),
      ],
      const SizedBox(height: 14),
      TextField(
        controller: address,
        maxLines: 2,
        onChanged: (_) => setState(() {}),
        decoration: const InputDecoration(
          labelText: 'عنوان استلام المرتجع',
          prefixIcon: Icon(Icons.location_on_outlined),
        ),
      ),
    ],
  );

  Widget _review() {
    final total = selected.entries.fold<double>(
      0,
      (sum, e) =>
          sum +
          order!.items.firstWhere((x) => x.id == e.key).unitPrice *
              e.value.quantity,
    );
    return Column(
      children: [
        Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: AppColors.primaryTint,
            borderRadius: BorderRadius.circular(16),
          ),
          child: Column(
            children: [
              const Icon(
                Icons.assignment_turned_in_outlined,
                size: 48,
                color: AppColors.primary,
              ),
              const SizedBox(height: 8),
              Text(
                '${selected.length} أصناف • ${selected.values.fold<int>(0, (a, b) => a + b.quantity)} قطعة',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              Text(
                '${money(total)} ج.م',
                style: const TextStyle(
                  fontSize: 25,
                  fontWeight: FontWeight.w700,
                  color: AppColors.primary,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              children: [
                InfoRow('الطلب', order!.number),
                InfoRow('التسوية', resolutionAr(resolution)),
                if (resolution == 'Refund')
                  InfoRow('طريقة الاسترداد', refundMethodAr(refundMethod)),
                InfoRow('صور الحالة', '${photos.length}'),
                InfoRow('عنوان الاستلام', address.text),
              ],
            ),
          ),
        ),
        const SizedBox(height: 10),
        const _Hint(
          icon: Icons.info_outline,
          title: 'ماذا يحدث بعد الإرسال؟',
          body:
              'يراجع الفريق الطلب ثم يحدد موعد استلام المنتجات. بعد الفحص يتم الاسترداد أو إرسال البديل.',
        ),
      ],
    );
  }

  Future<void> _submit() async {
    setState(() => busy = true);
    try {
      final repo = ref.read(returnRepositoryProvider);
      final created = await repo.create(
        order!.id,
        resolution,
        resolution == 'Refund' ? refundMethod : null,
        address.text,
        selected.entries
            .map(
              (e) => ReturnItemInput(
                e.key,
                e.value.quantity,
                e.value.reason,
                e.value.description,
              ),
            )
            .toList(),
      );
      for (final photo in photos) {
        await repo.upload(created.id, photo);
      }
      final submitted = await repo.submit(created.id);
      ref.invalidate(returnListProvider);
      ref.invalidate(eligibleReturnsProvider);
      if (mounted) context.go('/returns/${submitted.id}');
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }
}

class ReturnDetailScreen extends ConsumerWidget {
  const ReturnDetailScreen({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('تفاصيل المرتجع')),
    body: ref
        .watch(returnDetailProvider(id))
        .when(
          loading: () => const ListSkeleton(),
          error: (e, _) => Center(child: Text('$e')),
          data: (r) => RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(returnDetailProvider(id));
              await ref.read(returnDetailProvider(id).future);
            },
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
              children: [
                _ReturnHero(item: r),
                if (r.rejectionReason != null) ...[
                  const SizedBox(height: 12),
                  _Alert(
                    color: AppColors.error,
                    icon: Icons.cancel_outlined,
                    title: 'تم رفض طلب الإرجاع',
                    body: r.rejectionReason!,
                  ),
                ],
                if (r.status == 'RefundCompleted') ...[
                  const SizedBox(height: 12),
                  _Alert(
                    color: AppColors.success,
                    icon: Icons.account_balance_wallet_outlined,
                    title: 'تم استرداد المبلغ',
                    body:
                        'تم رد ${money(r.approvedTotal ?? r.requestedTotal)} ج.م عن طريق ${refundMethodAr(r.refundMethod ?? '')}',
                  ),
                ],
                const SizedBox(height: 12),
                _ReturnTimeline(item: r),
                if (r.pickupAt != null) ...[
                  const SizedBox(height: 12),
                  _Pickup(item: r),
                ],
                const SizedBox(height: 12),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(15),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Text(
                          'المنتجات المرتجعة',
                          style: TextStyle(fontWeight: FontWeight.w700),
                        ),
                        const Divider(),
                        ...r.items.map(
                          (i) => ListTile(
                            contentPadding: EdgeInsets.zero,
                            leading: CircleAvatar(
                              backgroundColor: AppColors.gray100,
                              child: const Icon(Icons.inventory_2_outlined),
                            ),
                            title: Text(
                              i.name,
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            subtitle: Text(
                              '${reasonAr(i.reason)} • الكمية ${i.quantity}${i.inspectionPassed == null
                                  ? ''
                                  : i.inspectionPassed!
                                  ? ' • اجتاز الفحص'
                                  : ' • لم يجتز الفحص'}',
                            ),
                            trailing: Text(
                              '${money(i.total)} ج.م',
                              style: const TextStyle(
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 12),
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(15),
                    child: Column(
                      children: [
                        InfoRow('نوع التسوية', resolutionAr(r.resolution)),
                        if (r.refundMethod != null)
                          InfoRow(
                            'طريقة الاسترداد',
                            refundMethodAr(r.refundMethod!),
                          ),
                        InfoRow(
                          'المبلغ المطلوب',
                          '${money(r.requestedTotal)} ج.م',
                        ),
                        if (r.approvedTotal != null)
                          InfoRow(
                            'المبلغ المعتمد',
                            '${money(r.approvedTotal!)} ج.م',
                          ),
                        InfoRow('صور الحالة', '${r.attachmentCount}'),
                        if (r.inspectionNotes != null)
                          InfoRow('نتيجة الفحص', r.inspectionNotes!),
                      ],
                    ),
                  ),
                ),
                if (r.canCancel) ...[
                  const SizedBox(height: 16),
                  TextButton.icon(
                    style: TextButton.styleFrom(
                      foregroundColor: AppColors.error,
                    ),
                    onPressed: () => _cancel(context, ref, r),
                    icon: const Icon(Icons.cancel_outlined),
                    label: const Text('إلغاء طلب الإرجاع'),
                  ),
                ],
              ],
            ),
          ),
        ),
  );
  Future<void> _cancel(
    BuildContext context,
    WidgetRef ref,
    ReturnDetailModel item,
  ) async {
    final ok =
        await showDialog<bool>(
          context: context,
          builder: (d) => AlertDialog(
            title: const Text('إلغاء طلب الإرجاع؟'),
            content: const Text('لن يتمكن مندوب الاستلام من متابعة هذا الطلب.'),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(d, false),
                child: const Text('رجوع'),
              ),
              FilledButton(
                onPressed: () => Navigator.pop(d, true),
                child: const Text('إلغاء الطلب'),
              ),
            ],
          ),
        ) ??
        false;
    if (!ok) return;
    try {
      await ref.read(returnRepositoryProvider).cancel(item.id);
      ref.invalidate(returnDetailProvider(item.id));
      ref.invalidate(returnListProvider);
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }
}

class _ReturnHero extends StatelessWidget {
  const _ReturnHero({required this.item});
  final ReturnDetailModel item;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(18),
    decoration: BoxDecoration(
      gradient: const LinearGradient(
        colors: [AppColors.primary, AppColors.primaryDark],
      ),
      borderRadius: BorderRadius.circular(20),
      boxShadow: AppShadows.floating,
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                item.number,
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: .15),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(
                returnStatusAr(item.status),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 10.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 15),
        Text(
          resolutionAr(item.resolution),
          style: const TextStyle(color: Colors.white70),
        ),
        Text(
          '${money(item.approvedTotal ?? item.requestedTotal)} ج.م',
          style: const TextStyle(
            color: Colors.white,
            fontSize: 25,
            fontWeight: FontWeight.w700,
          ),
        ),
        Text(
          'طلب الشراء ${item.orderNumber}',
          style: const TextStyle(color: Colors.white70, fontSize: 10.5),
        ),
      ],
    ),
  );
}

class _ReturnTimeline extends StatelessWidget {
  const _ReturnTimeline({required this.item});
  final ReturnDetailModel item;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'تحديثات الطلب',
            style: TextStyle(fontWeight: FontWeight.w700),
          ),
          const Divider(),
          ...item.history.asMap().entries.map((x) {
            final h = x.value, last = x.key == item.history.length - 1;
            return Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(
                  width: 24,
                  child: Column(
                    children: [
                      Container(
                        width: 15,
                        height: 15,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: last ? AppColors.primary : AppColors.success,
                        ),
                        child: const Icon(
                          Icons.check,
                          color: Colors.white,
                          size: 9,
                        ),
                      ),
                      if (!last)
                        Container(
                          width: 2,
                          height: 42,
                          color: AppColors.gray200,
                        ),
                    ],
                  ),
                ),
                const SizedBox(width: 7),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(bottom: 11),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          returnStatusAr(h.status),
                          style: const TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        if (h.note != null)
                          Text(
                            h.note!,
                            style: const TextStyle(
                              fontSize: 10.5,
                              color: AppColors.gray500,
                            ),
                          ),
                        Text(
                          DateFormat(
                            'd MMM، h:mm a',
                            'ar',
                          ).format(h.at.toLocal()),
                          style: const TextStyle(
                            fontSize: 9.5,
                            color: AppColors.gray400,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            );
          }),
        ],
      ),
    ),
  );
}

class _Pickup extends StatelessWidget {
  const _Pickup({required this.item});
  final ReturnDetailModel item;
  @override
  Widget build(BuildContext context) => Card(
    color: AppColors.primaryTint,
    child: Padding(
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'استلام المرتجع',
            style: TextStyle(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 10),
          InfoRow(
            'الموعد',
            '${DateFormat('EEEE، d MMMM، h:mm a', 'ar').format(item.pickupAt!.toLocal())} ${item.pickupWindow ?? ''}',
          ),
          InfoRow('العنوان', item.pickupAddress),
          if (item.driverName != null)
            InfoRow(
              'المندوب',
              '${item.driverName} • ${item.driverPhone ?? ''}',
            ),
          if (item.latitude != null)
            Container(
              height: 115,
              decoration: BoxDecoration(
                color: const Color(0xFFE1E9EF),
                borderRadius: BorderRadius.circular(13),
              ),
              child: Stack(
                children: [
                  const Center(
                    child: CircleAvatar(
                      backgroundColor: AppColors.primary,
                      child: Icon(Icons.local_shipping, color: Colors.white),
                    ),
                  ),
                  Positioned(
                    bottom: 7,
                    left: 7,
                    child: Container(
                      color: Colors.white,
                      padding: const EdgeInsets.all(5),
                      child: Text(
                        '${item.latitude!.toStringAsFixed(4)}, ${item.longitude!.toStringAsFixed(4)}',
                        style: const TextStyle(fontSize: 9.5),
                      ),
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

class _Choice extends StatelessWidget {
  const _Choice({
    required this.title,
    required this.icon,
    required this.selected,
    required this.tap,
  });
  final String title;
  final IconData icon;
  final bool selected;
  final VoidCallback tap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: tap,
    borderRadius: BorderRadius.circular(14),
    child: Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: selected ? AppColors.primaryTint : Colors.white,
        border: Border.all(
          color: selected ? AppColors.primary : AppColors.gray200,
          width: selected ? 2 : 1,
        ),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        children: [
          Icon(
            icon,
            color: selected ? AppColors.primary : AppColors.gray500,
            size: 30,
          ),
          const SizedBox(height: 6),
          Text(
            title,
            textAlign: TextAlign.center,
            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 11),
          ),
        ],
      ),
    ),
  );
}

class _Hint extends StatelessWidget {
  const _Hint({required this.icon, required this.title, required this.body});
  final IconData icon;
  final String title, body;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: AppColors.primaryTint,
      borderRadius: BorderRadius.circular(14),
    ),
    child: Row(
      children: [
        Icon(icon, color: AppColors.primary),
        const SizedBox(width: 9),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
              Text(
                body,
                style: const TextStyle(
                  fontSize: 10.5,
                  color: AppColors.gray600,
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

class _Alert extends StatelessWidget {
  const _Alert({
    required this.color,
    required this.icon,
    required this.title,
    required this.body,
  });
  final Color color;
  final IconData icon;
  final String title, body;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: color.withValues(alpha: .08),
      border: Border.all(color: color.withValues(alpha: .3)),
      borderRadius: BorderRadius.circular(14),
    ),
    child: Row(
      children: [
        Icon(icon, color: color),
        const SizedBox(width: 9),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: TextStyle(fontWeight: FontWeight.w700, color: color),
              ),
              Text(body, style: const TextStyle(fontSize: 11)),
            ],
          ),
        ),
      ],
    ),
  );
}

class InfoRow extends StatelessWidget {
  const InfoRow(this.label, this.value, {super.key});
  final String label, value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 5),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 105,
          child: Text(
            label,
            style: const TextStyle(fontSize: 11, color: AppColors.gray500),
          ),
        ),
        Expanded(
          child: Text(
            value,
            textAlign: TextAlign.end,
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
          ),
        ),
      ],
    ),
  );
}

class ReturnStatusChip extends StatelessWidget {
  const ReturnStatusChip({super.key, required this.status});
  final String status;
  @override
  Widget build(BuildContext context) {
    final color = returnStatusColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        returnStatusAr(status),
        style: TextStyle(
          fontSize: 9.5,
          fontWeight: FontWeight.w700,
          color: color,
        ),
      ),
    );
  }
}

class _EmptyReturns extends StatelessWidget {
  const _EmptyReturns();
  @override
  Widget build(BuildContext context) => const Center(
    child: Padding(
      padding: EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.assignment_return_outlined,
            size: 70,
            color: AppColors.gray300,
          ),
          SizedBox(height: 12),
          Text(
            'لا توجد طلبات إرجاع',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
          ),
          Text(
            'يمكنك إنشاء طلب للمنتجات المستلمة خلال فترة الإرجاع',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.gray500),
          ),
        ],
      ),
    ),
  );
}

const returnReasons = {
  'Damaged': 'المنتج تالف',
  'WrongItem': 'منتج خاطئ',
  'MissingParts': 'أجزاء ناقصة',
  'NotAsDescribed': 'غير مطابق للوصف',
  'ExcessQuantity': 'كمية زائدة',
  'QualityIssue': 'مشكلة جودة',
  'Other': 'سبب آخر',
};
String money(double value) => NumberFormat('#,##0.##', 'ar').format(value);
String reasonAr(String s) => returnReasons[s] ?? s;
String resolutionAr(String s) =>
    s == 'Refund' ? 'استرداد المبلغ' : 'استبدال المنتج';
String refundMethodAr(String s) =>
    const {
      'OriginalPayment': 'نفس وسيلة الدفع',
      'BankTransfer': 'تحويل بنكي',
      'CreditBalance': 'رصيد دائن للشركة',
    }[s] ??
    s;
String returnStatusAr(String s) =>
    const {
      'Draft': 'مسودة',
      'Submitted': 'قيد المراجعة',
      'UnderReview': 'تحت المراجعة',
      'Approved': 'تمت الموافقة',
      'Rejected': 'مرفوض',
      'PickupScheduled': 'تم تحديد الاستلام',
      'InTransit': 'المندوب في الطريق',
      'Received': 'تم الاستلام',
      'Inspecting': 'جاري الفحص',
      'RefundApproved': 'تم اعتماد الاسترداد',
      'RefundCompleted': 'تم رد المبلغ',
      'ReplacementPreparing': 'جاري تجهيز البديل',
      'ReplacementShipped': 'تم شحن البديل',
      'ReplacementDelivered': 'تم تسليم البديل',
      'Completed': 'مكتمل',
      'Cancelled': 'ملغي',
    }[s] ??
    s;
Color returnStatusColor(String s) => switch (s) {
  'Completed' ||
  'RefundCompleted' ||
  'ReplacementDelivered' => AppColors.success,
  'Rejected' || 'Cancelled' => AppColors.error,
  'Submitted' || 'UnderReview' => AppColors.warning,
  _ => AppColors.primary,
};
