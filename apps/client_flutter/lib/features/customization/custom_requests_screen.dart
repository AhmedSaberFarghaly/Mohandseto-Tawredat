import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/widgets/skeleton.dart';
import '../../core/api/api_client.dart';
import '../../core/api/customization_repository.dart';
import '../../core/api/cart_repository.dart';
import '../../core/theme/app_tokens.dart';

const _statusLabels = <String, String>{
  'Draft': 'مسودة',
  'AwaitingQuote': 'بانتظار عرض السعر',
  'Quoted': 'عرض السعر جاهز',
  'DesignInProgress': 'التصميم قيد التنفيذ',
  'AwaitingDesignApproval': 'بانتظار اعتماد التصميم',
  'DesignApproved': 'التصميم معتمد',
  'AwaitingCheckout': 'بانتظار إتمام الطلب',
  'AwaitingOrderApproval': 'بانتظار موافقة الشركة',
  'AwaitingSampleApproval': 'بانتظار اعتماد العينة',
  'InProduction': 'قيد الإنتاج',
  'QualityCheck': 'فحص الجودة',
  'Ready': 'جاهز',
  'Completed': 'مكتمل',
  'Rejected': 'مرفوض',
  'Cancelled': 'ملغي',
};

class CustomRequestsScreen extends ConsumerWidget {
  const CustomRequestsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final requests = ref.watch(customRequestsProvider);
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('طلبات الطباعة والتخصيص'),
        actions: [
          IconButton(
            onPressed: () => context.push('/custom-products'),
            icon: const Icon(Icons.add_rounded),
          ),
        ],
      ),
      body: requests.when(
        loading: () => const ListSkeleton(),
        error: (error, _) => Center(child: Text('$error')),
        data: (items) => items.isEmpty
            ? _empty(context)
            : RefreshIndicator(
                onRefresh: () async {
                  ref.invalidate(customRequestsProvider);
                  await ref.read(customRequestsProvider.future);
                },
                child: ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: items.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 10),
                  itemBuilder: (context, index) =>
                      _RequestCard(request: items[index]),
                ),
              ),
      ),
    );
  }

  Widget _empty(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(30),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.print_outlined, color: AppColors.gray400, size: 64),
          const SizedBox(height: 12),
          const Text(
            'لا توجد طلبات تخصيص بعد',
            style: TextStyle(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 6),
          const Text(
            'اختر منتجًا وارفع شعار شركتك لبدء أول طلب.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.gray500),
          ),
          const SizedBox(height: 18),
          FilledButton.icon(
            onPressed: () => context.push('/custom-products'),
            icon: const Icon(Icons.add_rounded),
            label: const Text('إنشاء طلب'),
          ),
        ],
      ),
    ),
  );
}

class _RequestCard extends StatelessWidget {
  const _RequestCard({required this.request});
  final CustomRequestSummary request;
  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: InkWell(
      onTap: () => context.push('/custom-requests/${request.id}'),
      borderRadius: BorderRadius.circular(AppRadius.lg),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    request.productName,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
                _StatusChip(request.status),
              ],
            ),
            const SizedBox(height: 4),
            Text(
              '${request.number}  •  ${DateFormat('d MMM yyyy', 'ar').format(request.createdAt.toLocal())}',
              style: const TextStyle(color: AppColors.gray500, fontSize: 10.5),
            ),
            const SizedBox(height: 12),
            ClipRRect(
              borderRadius: BorderRadius.circular(5),
              child: LinearProgressIndicator(
                value: request.progress / 100,
                minHeight: 7,
                backgroundColor: AppColors.gray150,
                color: request.status == 'Rejected'
                    ? AppColors.error
                    : AppColors.primary,
              ),
            ),
            const SizedBox(height: 9),
            Row(
              children: [
                Text(
                  '${request.quantity} قطعة',
                  style: const TextStyle(fontSize: 11),
                ),
                const Spacer(),
                Text(
                  '${NumberFormat('#,##0.00', 'ar').format(request.total)} ج.م',
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
    ),
  );
}

class CustomRequestDetailScreen extends ConsumerStatefulWidget {
  const CustomRequestDetailScreen({super.key, required this.requestId});
  final String requestId;
  @override
  ConsumerState<CustomRequestDetailScreen> createState() =>
      _CustomRequestDetailScreenState();
}

class _CustomRequestDetailScreenState
    extends ConsumerState<CustomRequestDetailScreen> {
  final _comment = TextEditingController();
  bool _busy = false;
  @override
  void dispose() {
    _comment.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final request = ref.watch(customRequestProvider(widget.requestId));
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: const Text('تفاصيل طلب التخصيص')),
      body: request.when(
        loading: () => const ListSkeleton(),
        error: (error, _) => Center(child: Text('$error')),
        data: (r) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(customRequestProvider(widget.requestId));
            await ref.read(customRequestProvider(widget.requestId).future);
          },
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _summary(r),
              if (r.status == 'DesignApproved') ...[
                const SizedBox(height: 14),
                FilledButton.icon(
                  onPressed: _busy ? null : _addToCart,
                  icon: const Icon(Icons.add_shopping_cart_rounded),
                  label: const Text('إضافة المنتج المخصص للسلة'),
                ),
              ],
              if (r.status == 'Quoted') ...[
                const SizedBox(height: 14),
                _quote(r),
              ],
              if (r.versions.isNotEmpty) ...[
                const SizedBox(height: 18),
                _design(r),
              ],
              if (r.productionSamples.isNotEmpty) ...[
                const SizedBox(height: 18),
                _sample(r),
              ],
              if (r.productionStages.isNotEmpty) ...[
                const SizedBox(height: 18),
                _production(r),
              ],
              const SizedBox(height: 18),
              _comments(r),
              const SizedBox(height: 30),
            ],
          ),
        ),
      ),
    );
  }

  Widget _summary(CustomRequestDetail r) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
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
                      r.productName,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      '${r.number} • ${r.sku}',
                      style: const TextStyle(
                        color: AppColors.gray500,
                        fontSize: 10.5,
                      ),
                    ),
                  ],
                ),
              ),
              _StatusChip(r.status),
            ],
          ),
          const Divider(height: 26),
          _Row('الكمية', '${r.quantity} قطعة'),
          _Row('الخامة واللون', '${r.material} — ${r.color}'),
          _Row('المقاس', r.size),
          _Row('طريقة الطباعة', r.printMethod),
          _Row('موضع الطباعة', r.placement),
          _Row(
            'مساحة الطباعة',
            '${r.printWidthCm} × ${r.printHeightCm} سم — ${r.printColorCount} لون',
          ),
          const Divider(height: 22),
          _Row(
            'التقدير المبدئي',
            '${NumberFormat('#,##0.00', 'ar').format(r.estimatedTotal)} ج.م',
          ),
          if (r.quotedTotal != null)
            _Row(
              'عرض السعر النهائي',
              '${NumberFormat('#,##0.00', 'ar').format(r.quotedTotal)} ج.م',
              primary: true,
            ),
        ],
      ),
    ),
  );

  Widget _quote(CustomRequestDetail r) => Container(
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: AppColors.primaryTint,
      borderRadius: BorderRadius.circular(AppRadius.lg),
      border: Border.all(color: AppColors.primary.withValues(alpha: .25)),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Row(
          children: [
            Icon(Icons.request_quote_outlined, color: AppColors.primary),
            SizedBox(width: 8),
            Text(
              'عرض السعر جاهز',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
          ],
        ),
        const SizedBox(height: 8),
        Text(
          '${NumberFormat('#,##0.00', 'ar').format(r.quotedTotal)} ج.م',
          style: const TextStyle(
            color: AppColors.primary,
            fontSize: 22,
            fontWeight: FontWeight.w700,
          ),
        ),
        if (r.quoteExpiresAt != null)
          Text(
            'صالح حتى ${DateFormat('d MMM yyyy', 'ar').format(r.quoteExpiresAt!.toLocal())}',
            style: const TextStyle(color: AppColors.gray600, fontSize: 10.5),
          ),
        const SizedBox(height: 13),
        Row(
          children: [
            Expanded(
              child: FilledButton(
                onPressed: _busy ? null : () => _quoteAction(true),
                child: const Text('قبول العرض'),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: OutlinedButton(
                onPressed: _busy ? null : () => _quoteAction(false),
                child: const Text('رفض'),
              ),
            ),
          ],
        ),
      ],
    ),
  );

  Widget _design(CustomRequestDetail r) {
    final version = r.versions.first;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'التصميم والمعاينة',
          style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 10),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Text(
                      'النسخة ${version.number}',
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    const Spacer(),
                    Text(
                      version.title,
                      style: const TextStyle(
                        color: AppColors.gray600,
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
                if (version.mockups.isNotEmpty) ...[
                  const SizedBox(height: 12),
                  _MockupPreview(
                    url: version.mockups.first['downloadUrl'] as String,
                  ),
                ],
                if (version.changeSummary != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      version.changeSummary!,
                      style: const TextStyle(
                        color: AppColors.gray600,
                        fontSize: 11,
                      ),
                    ),
                  ),
                if (r.status == 'AwaitingDesignApproval') ...[
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    onPressed: _busy
                        ? null
                        : () => _designAction(version.id, 'Approved'),
                    icon: const Icon(Icons.verified_outlined),
                    label: const Text('اعتماد التصميم'),
                  ),
                  Row(
                    children: [
                      Expanded(
                        child: TextButton(
                          onPressed: _busy
                              ? null
                              : () => _designAction(
                                  version.id,
                                  'RevisionRequested',
                                ),
                          child: const Text('طلب تعديل'),
                        ),
                      ),
                      Expanded(
                        child: TextButton(
                          onPressed: _busy
                              ? null
                              : () => _designAction(version.id, 'Rejected'),
                          child: const Text(
                            'رفض التصميم',
                            style: TextStyle(color: AppColors.error),
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _production(CustomRequestDetail r) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const Text(
        'متابعة الإنتاج',
        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
      ),
      const SizedBox(height: 10),
      ...r.productionStages.map(
        (s) => Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Column(
              children: [
                Icon(
                  s.status == 'Completed'
                      ? Icons.check_circle_rounded
                      : s.status == 'InProgress'
                      ? Icons.timelapse_rounded
                      : Icons.radio_button_unchecked_rounded,
                  color: s.status == 'Completed'
                      ? AppColors.success
                      : s.status == 'InProgress'
                      ? AppColors.primary
                      : AppColors.gray300,
                ),
                if (s != r.productionStages.last)
                  Container(width: 2, height: 34, color: AppColors.gray200),
              ],
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.only(top: 2),
                child: Text(
                  s.name,
                  style: TextStyle(
                    fontWeight: s.status == 'InProgress'
                        ? FontWeight.w700
                        : FontWeight.w600,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    ],
  );

  Widget _sample(CustomRequestDetail r) {
    final sample = r.productionSamples.first;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'عينة ما قبل الإنتاج',
          style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 10),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Text(
                      'العينة ${sample.version}',
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    const Spacer(),
                    _StatusChip(sample.decision),
                  ],
                ),
                const SizedBox(height: 12),
                _MockupPreview(url: sample.downloadUrl),
                if (sample.note != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      sample.note!,
                      style: const TextStyle(
                        color: AppColors.gray600,
                        fontSize: 11,
                      ),
                    ),
                  ),
                if (r.status == 'AwaitingSampleApproval') ...[
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    onPressed: _busy
                        ? null
                        : () => _sampleAction(sample.id, 'Approved'),
                    icon: const Icon(Icons.check_circle_outline),
                    label: const Text('اعتماد العينة وبدء الإنتاج'),
                  ),
                  Row(
                    children: [
                      Expanded(
                        child: TextButton(
                          onPressed: _busy
                              ? null
                              : () => _sampleAction(
                                  sample.id,
                                  'RevisionRequested',
                                ),
                          child: const Text('طلب عينة معدلة'),
                        ),
                      ),
                      Expanded(
                        child: TextButton(
                          onPressed: _busy
                              ? null
                              : () => _sampleAction(sample.id, 'Rejected'),
                          child: const Text(
                            'رفض العينة',
                            style: TextStyle(color: AppColors.error),
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _comments(CustomRequestDetail r) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const Text(
        'الملاحظات',
        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
      ),
      const SizedBox(height: 8),
      ...r.comments.map(
        (c) => Container(
          margin: const EdgeInsets.only(bottom: 7),
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: AppColors.gray100,
            borderRadius: BorderRadius.circular(AppRadius.md),
          ),
          child: Text(c['body'] as String),
        ),
      ),
      Row(
        children: [
          Expanded(
            child: TextField(
              controller: _comment,
              decoration: const InputDecoration(
                hintText: 'أضف ملاحظة للمصمم...',
              ),
            ),
          ),
          const SizedBox(width: 7),
          IconButton.filled(
            onPressed: _busy ? null : _addComment,
            icon: const Icon(Icons.send_rounded),
          ),
        ],
      ),
    ],
  );

  Future<void> _quoteAction(bool accept) async => _act(
    () => ref
        .read(customizationRepositoryProvider)
        .respondQuote(widget.requestId, accept),
  );
  Future<void> _designAction(String version, String decision) async {
    String? note;
    if (decision != 'Approved') note = await _askNote();
    if (decision != 'Approved' && (note == null || note.trim().isEmpty)) return;
    await _act(
      () => ref
          .read(customizationRepositoryProvider)
          .decideDesign(widget.requestId, version, decision, note),
    );
  }

  Future<void> _addComment() async {
    final text = _comment.text.trim();
    if (text.isEmpty) return;
    await _act(
      () => ref
          .read(customizationRepositoryProvider)
          .comment(widget.requestId, text),
    );
    _comment.clear();
  }

  Future<void> _sampleAction(String sampleId, String decision) async {
    String? note;
    if (decision != 'Approved') note = await _askNote();
    if (decision != 'Approved' && (note == null || note.trim().isEmpty)) return;
    await _act(
      () => ref
          .read(customizationRepositoryProvider)
          .decideSample(widget.requestId, sampleId, decision, note),
    );
  }

  Future<void> _addToCart() async {
    setState(() => _busy = true);
    try {
      await ref
          .read(customizationRepositoryProvider)
          .addToCart(widget.requestId);
      ref.invalidate(cartProvider);
      ref.invalidate(customRequestProvider(widget.requestId));
      ref.invalidate(customRequestsProvider);
      if (mounted) context.push('/cart');
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$error')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _act(Future<CustomRequestDetail> Function() operation) async {
    setState(() => _busy = true);
    try {
      await operation();
      ref.invalidate(customRequestProvider(widget.requestId));
      ref.invalidate(customRequestsProvider);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$error')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<String?> _askNote() async {
    final controller = TextEditingController();
    final result = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('ملاحظات التعديل'),
        content: TextField(
          controller: controller,
          autofocus: true,
          maxLines: 4,
          decoration: const InputDecoration(
            hintText: 'اكتب التعديلات المطلوبة بوضوح',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('إلغاء'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, controller.text),
            child: const Text('إرسال'),
          ),
        ],
      ),
    );
    controller.dispose();
    return result;
  }
}

class _MockupPreview extends ConsumerWidget {
  const _MockupPreview({required this.url});
  final String url;
  @override
  Widget build(BuildContext context, WidgetRef ref) =>
      FutureBuilder<Response<List<int>>>(
        future: ref
            .read(apiClientProvider)
            .dio
            .get<List<int>>(
              url,
              options: Options(responseType: ResponseType.bytes),
            ),
        builder: (context, snapshot) => Container(
          height: 220,
          clipBehavior: Clip.antiAlias,
          decoration: BoxDecoration(
            color: AppColors.gray100,
            borderRadius: BorderRadius.circular(AppRadius.md),
          ),
          child: snapshot.hasData && snapshot.data!.data != null
              ? InteractiveViewer(
                  child: Image.memory(
                    Uint8List.fromList(snapshot.data!.data!),
                    fit: BoxFit.contain,
                  ),
                )
              : snapshot.hasError
              ? const Center(
                  child: Icon(
                    Icons.broken_image_outlined,
                    color: AppColors.gray400,
                  ),
                )
              : const Center(child: CircularProgressIndicator()),
        ),
      );
}

class _StatusChip extends StatelessWidget {
  const _StatusChip(this.status);
  final String status;
  @override
  Widget build(BuildContext context) {
    final danger = status == 'Rejected' || status == 'Cancelled';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
      decoration: BoxDecoration(
        color: danger ? AppColors.errorTint : AppColors.primaryTint,
        borderRadius: BorderRadius.circular(AppRadius.pill),
      ),
      child: Text(
        _statusLabels[status] ?? status,
        style: TextStyle(
          color: danger ? AppColors.error : AppColors.primary,
          fontSize: 10.5,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row(this.label, this.value, {this.primary = false});
  final String label, value;
  final bool primary;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 5),
    child: Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: const TextStyle(color: AppColors.gray500, fontSize: 11),
          ),
        ),
        Text(
          value,
          style: TextStyle(
            color: primary ? AppColors.primary : AppColors.gray800,
            fontWeight: FontWeight.w700,
            fontSize: 11,
          ),
        ),
      ],
    ),
  );
}
