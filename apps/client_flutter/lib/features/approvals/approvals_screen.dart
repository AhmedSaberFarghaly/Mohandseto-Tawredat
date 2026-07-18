import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/widgets/skeleton.dart';
import '../../core/api/approval_repository.dart';
import '../../core/theme/app_tokens.dart';

class ApprovalsScreen extends ConsumerStatefulWidget {
  const ApprovalsScreen({super.key});
  @override
  ConsumerState<ApprovalsScreen> createState() => _ApprovalsScreenState();
}

class _ApprovalsScreenState extends ConsumerState<ApprovalsScreen> {
  String? _status = 'Pending';
  @override
  Widget build(BuildContext context) {
    final inbox = ref.watch(approvalInboxProvider(_status));
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('مركز الموافقات'),
        actions: [
          IconButton(
            onPressed: () => ref.invalidate(approvalInboxProvider(_status)),
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: Column(
        children: [
          SizedBox(
            height: 52,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
              children: [
                _filter('بانتظار قراري', 'Pending'),
                _filter('مطلوب تعديل', 'ChangesRequested'),
                _filter('المكتملة', 'Approved'),
                _filter('المرفوضة', 'Rejected'),
                _filter('الكل', null),
              ],
            ),
          ),
          Expanded(
            child: inbox.when(
              loading: () => const ListSkeleton(),
              error: (error, _) => Center(child: Text('$error')),
              data: (items) => items.isEmpty
                  ? const _ApprovalEmpty()
                  : RefreshIndicator(
                      onRefresh: () async {
                        ref.invalidate(approvalInboxProvider(_status));
                        await ref.read(approvalInboxProvider(_status).future);
                      },
                      child: ListView.builder(
                        padding: const EdgeInsets.fromLTRB(14, 8, 14, 24),
                        itemCount: items.length,
                        itemBuilder: (context, index) => _card(items[index]),
                      ),
                    ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _filter(String label, String? value) => Padding(
    padding: const EdgeInsetsDirectional.only(end: 7),
    child: ChoiceChip(
      label: Text(label),
      selected: _status == value,
      onSelected: (_) => setState(() => _status = value),
    ),
  );
  Widget _card(ApprovalListItem item) => Container(
    margin: const EdgeInsets.only(bottom: 10),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: InkWell(
      onTap: () => context.push('/approvals/${item.id}'),
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
                    item.orderNumber,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
                _statusChip(item.status),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              '${_money(item.total)} ج.م',
              style: const TextStyle(
                fontSize: 18,
                color: AppColors.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 5),
            Text(
              '${item.requesterName} • ${item.currentLevel}',
              style: const TextStyle(color: AppColors.gray500, fontSize: 11),
            ),
            if (item.budgetConflict)
              const Padding(
                padding: EdgeInsets.only(top: 7),
                child: Row(
                  children: [
                    Icon(
                      Icons.warning_amber_rounded,
                      color: AppColors.error,
                      size: 16,
                    ),
                    SizedBox(width: 5),
                    Expanded(
                      child: Text(
                        'تعارض مع الميزانية المتاحة',
                        style: TextStyle(
                          color: AppColors.error,
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            const Divider(height: 20),
            Row(
              children: [
                Icon(
                  item.overdue ? Icons.timer_off_outlined : Icons.schedule,
                  color: item.overdue ? AppColors.error : AppColors.warning,
                  size: 16,
                ),
                const SizedBox(width: 5),
                Expanded(
                  child: Text(
                    item.overdue
                        ? 'تجاوزت مهلة الموافقة'
                        : 'المهلة ${DateFormat('d MMM، HH:mm', 'ar').format(item.dueAt.toLocal())}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      fontSize: 10.5,
                      color: item.overdue ? AppColors.error : AppColors.gray500,
                    ),
                  ),
                ),
                const Icon(Icons.arrow_back_ios_new_rounded, size: 14),
              ],
            ),
          ],
        ),
      ),
    ),
  );
  Widget _statusChip(String status) => Chip(
    label: Text(_statusName(status), style: const TextStyle(fontSize: 9.5)),
    backgroundColor: _statusColor(status).withValues(alpha: .12),
    side: BorderSide.none,
  );
}

class ApprovalDetailScreen extends ConsumerStatefulWidget {
  const ApprovalDetailScreen({super.key, required this.id});
  final String id;
  @override
  ConsumerState<ApprovalDetailScreen> createState() =>
      _ApprovalDetailScreenState();
}

class _ApprovalDetailScreenState extends ConsumerState<ApprovalDetailScreen> {
  bool _busy = false;
  @override
  Widget build(BuildContext context) {
    final value = ref.watch(approvalDetailProvider(widget.id));
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(title: const Text('تفاصيل الموافقة')),
      body: value.when(
        loading: () => const ListSkeleton(),
        error: (error, _) => Center(child: Text('$error')),
        data: (detail) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(approvalDetailProvider(widget.id));
            await ref.read(approvalDetailProvider(widget.id).future);
          },
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 120),
            children: [
              _header(detail),
              if (detail.budgetConflict) _budget(detail),
              _section('سلسلة الموافقات', _chain(detail)),
              _section('المرفقات', _attachments(detail)),
              _section('التعليقات والسجل', _history(detail)),
              if (detail.status == 'ChangesRequested')
                FilledButton.icon(
                  onPressed: _busy ? null : () => _decision('resubmit', true),
                  icon: const Icon(Icons.refresh),
                  label: const Text('إعادة إرسال الطلب بعد التعديل'),
                ),
            ],
          ),
        ),
      ),
      bottomNavigationBar: value.value?.canAct == true
          ? _actions(value.value!)
          : null,
    );
  }

  Widget _header(ApprovalDetailModel d) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  d.orderNumber,
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              Chip(
                label: Text(
                  _statusName(d.status),
                  style: const TextStyle(fontSize: 9.5),
                ),
                side: BorderSide.none,
                backgroundColor: _statusColor(d.status).withValues(alpha: .12),
              ),
            ],
          ),
          Text(
            '${_money(d.total)} ج.م',
            style: const TextStyle(
              fontSize: 23,
              color: AppColors.primary,
              fontWeight: FontWeight.w700,
            ),
          ),
          const Divider(height: 24),
          _kv('مقدم الطلب', d.requester),
          _kv('الإدارة', d.department),
          _kv('مركز التكلفة', d.costCenter ?? '—'),
          _kv('رقم الموافقة', d.number),
          if (d.exceedsAuthority)
            Container(
              margin: const EdgeInsets.only(top: 10),
              padding: const EdgeInsets.all(10),
              color: AppColors.warningTint,
              child: const Row(
                children: [
                  Icon(Icons.gpp_maybe_outlined, color: AppColors.warning),
                  SizedBox(width: 7),
                  Expanded(
                    child: Text(
                      'قيمة الطلب أعلى من حد صلاحيتك وسيستمر للمستوى التالي بعد موافقتك.',
                      style: TextStyle(fontSize: 10.5),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    ),
  );
  Widget _budget(ApprovalDetailModel d) => Container(
    margin: const EdgeInsets.symmetric(vertical: 10),
    padding: const EdgeInsets.all(13),
    decoration: BoxDecoration(
      color: AppColors.errorTint,
      borderRadius: BorderRadius.circular(AppRadius.md),
    ),
    child: Row(
      children: [
        const Icon(
          Icons.account_balance_wallet_outlined,
          color: AppColors.error,
        ),
        const SizedBox(width: 9),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'تعارض في الميزانية',
                style: TextStyle(
                  color: AppColors.error,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                'المتاح بعد الحجز: ${_money(d.budgetAvailable ?? 0)} ج.م',
                style: const TextStyle(fontSize: 10.5),
              ),
            ],
          ),
        ),
      ],
    ),
  );
  Widget _chain(ApprovalDetailModel d) => Column(
    children: d.steps
        .map(
          (s) => Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Column(
                children: [
                  CircleAvatar(
                    radius: 15,
                    backgroundColor: _stepColor(s.status),
                    child: Icon(
                      _stepIcon(s.status),
                      size: 15,
                      color: Colors.white,
                    ),
                  ),
                  if (s.sequence < d.steps.length)
                    Container(width: 2, height: 42, color: AppColors.gray200),
                ],
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.only(top: 2),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        s.name,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      Text(
                        s.approverName,
                        style: const TextStyle(
                          fontSize: 10.5,
                          color: AppColors.gray500,
                        ),
                      ),
                      if (s.authorityLimit != null)
                        Text(
                          'حد الصلاحية ${_money(s.authorityLimit!)} ج.م',
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
          ),
        )
        .toList(),
  );
  Widget _attachments(ApprovalDetailModel d) => Column(
    children: [
      ...d.attachments.map(
        (a) => ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const Icon(Icons.attach_file),
          title: Text(a.name),
          subtitle: Text('${(a.size / 1024).toStringAsFixed(1)} KB'),
        ),
      ),
      OutlinedButton.icon(
        onPressed: _busy ? _noop : _upload,
        icon: const Icon(Icons.upload_file),
        label: const Text('إضافة مرفق'),
      ),
    ],
  );
  Widget _history(ApprovalDetailModel d) => Column(
    children: [
      ...d.actions.map(
        (a) => ListTile(
          contentPadding: EdgeInsets.zero,
          leading: CircleAvatar(
            radius: 14,
            child: Icon(_actionIcon(a.type), size: 14),
          ),
          title: Text(
            '${a.actor} — ${_actionName(a.type)}',
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
          ),
          subtitle: Text(
            '${a.comment ?? ''}\n${DateFormat('d MMM، HH:mm', 'ar').format(a.createdAt.toLocal())}',
            style: const TextStyle(fontSize: 9.5),
          ),
        ),
      ),
      OutlinedButton.icon(
        onPressed: _busy ? _noop : _comment,
        icon: const Icon(Icons.chat_bubble_outline),
        label: const Text('إضافة تعليق'),
      ),
    ],
  );
  Widget _section(String title, Widget child) => Card(
    margin: const EdgeInsets.only(top: 10),
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 12),
          child,
        ],
      ),
    ),
  );
  Widget _actions(ApprovalDetailModel d) => SafeArea(
    child: Container(
      padding: const EdgeInsets.all(10),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: AppColors.gray200)),
      ),
      child: Row(
        children: [
          IconButton(
            onPressed: _busy ? null : _delegate,
            tooltip: 'تفويض',
            icon: const Icon(Icons.forward_to_inbox_outlined),
          ),
          Expanded(
            child: OutlinedButton(
              onPressed: _busy
                  ? null
                  : () => _decision('request-changes', true),
              child: const Text('طلب تعديل'),
            ),
          ),
          const SizedBox(width: 7),
          Expanded(
            child: OutlinedButton(
              style: OutlinedButton.styleFrom(foregroundColor: AppColors.error),
              onPressed: _busy ? null : () => _decision('reject', true),
              child: const Text('رفض'),
            ),
          ),
          const SizedBox(width: 7),
          Expanded(
            child: FilledButton(
              onPressed: _busy ? null : () => _decision('approve', false),
              child: const Text('موافقة'),
            ),
          ),
        ],
      ),
    ),
  );
  Future<void> _decision(String action, bool required) async {
    final controller = TextEditingController();
    final accepted = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheetContext) => Padding(
        padding: EdgeInsets.fromLTRB(
          20,
          0,
          20,
          MediaQuery.viewInsetsOf(sheetContext).bottom + 20,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              _actionName(action),
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: controller,
              maxLines: 3,
              decoration: InputDecoration(
                labelText: required ? 'السبب *' : 'تعليق اختياري',
              ),
            ),
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: () => Navigator.pop(
                  sheetContext,
                  !required || controller.text.trim().isNotEmpty,
                ),
                child: const Text('تأكيد القرار'),
              ),
            ),
          ],
        ),
      ),
    );
    if (accepted == true) {
      await _run(
        () => ref
            .read(approvalRepositoryProvider)
            .decision(
              widget.id,
              action,
              controller.text.trim().isEmpty ? null : controller.text.trim(),
            ),
      );
    }
    controller.dispose();
  }

  Future<void> _comment() async {
    final c = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('إضافة تعليق'),
        content: TextField(controller: c, maxLines: 3),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('إلغاء'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, c.text.trim().isNotEmpty),
            child: const Text('إرسال'),
          ),
        ],
      ),
    );
    if (ok == true) {
      await _run(
        () => ref
            .read(approvalRepositoryProvider)
            .comment(widget.id, c.text.trim()),
      );
    }
    c.dispose();
  }

  Future<void> _delegate() async {
    final users = await ref.read(approvalRepositoryProvider).users();
    if (!mounted) return;
    String? selected;
    final c = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => AlertDialog(
          title: const Text('تفويض الموافقة'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<String>(
                decoration: const InputDecoration(labelText: 'المفوض إليه'),
                items: users
                    .map(
                      (u) => DropdownMenuItem(value: u.id, child: Text(u.name)),
                    )
                    .toList(),
                onChanged: (v) => setLocal(() => selected = v),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: c,
                decoration: const InputDecoration(labelText: 'سبب التفويض'),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('إلغاء'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(ctx, selected != null),
              child: const Text('تفويض'),
            ),
          ],
        ),
      ),
    );
    if (ok == true) {
      await _run(
        () => ref
            .read(approvalRepositoryProvider)
            .delegate(
              widget.id,
              selected!,
              c.text.trim().isEmpty ? null : c.text.trim(),
            ),
      );
    }
    c.dispose();
  }

  Future<void> _upload() async {
    final file = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: const ['pdf', 'png', 'jpg', 'jpeg'],
      withData: true,
    );
    if (file != null) {
      await _run(() async {
        await ref
            .read(approvalRepositoryProvider)
            .upload(widget.id, file.files.single);
        return ref.read(approvalRepositoryProvider).detail(widget.id);
      });
    }
  }

  Future<void> _run(Future<ApprovalDetailModel> Function() task) async {
    setState(() => _busy = true);
    try {
      await task();
      ref.invalidate(approvalDetailProvider(widget.id));
      ref.invalidate(approvalInboxProvider);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _noop() {}
  Widget _kv(String k, String v) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 3),
    child: Row(
      children: [
        Expanded(
          child: Text(
            k,
            style: const TextStyle(color: AppColors.gray500, fontSize: 11),
          ),
        ),
        Text(
          v,
          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
        ),
      ],
    ),
  );
}

class _ApprovalEmpty extends StatelessWidget {
  const _ApprovalEmpty();
  @override
  Widget build(BuildContext context) => ListView(
    children: const [
      Padding(
        padding: EdgeInsets.only(top: 120),
        child: Column(
          children: [
            Icon(Icons.task_alt_rounded, size: 66, color: AppColors.success),
            SizedBox(height: 12),
            Text(
              'لا توجد موافقات في هذه القائمة',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
            Text(
              'ستظهر الطلبات الجديدة هنا فور إرسالها',
              style: TextStyle(color: AppColors.gray500, fontSize: 11),
            ),
          ],
        ),
      ),
    ],
  );
}

String _money(double v) => NumberFormat('#,##0.00', 'ar').format(v);
String _statusName(String s) =>
    {
      'Pending': 'قيد الموافقة',
      'ChangesRequested': 'مطلوب تعديل',
      'Approved': 'معتمد',
      'Rejected': 'مرفوض',
    }[s] ??
    s;
Color _statusColor(String s) =>
    {
      'Pending': AppColors.warning,
      'ChangesRequested': AppColors.primary,
      'Approved': AppColors.success,
      'Rejected': AppColors.error,
    }[s] ??
    AppColors.gray500;
Color _stepColor(String s) =>
    {
      'Current': AppColors.primary,
      'Approved': AppColors.success,
      'Rejected': AppColors.error,
      'ChangesRequested': AppColors.warning,
    }[s] ??
    AppColors.gray300;
IconData _stepIcon(String s) =>
    {
      'Approved': Icons.check,
      'Rejected': Icons.close,
      'ChangesRequested': Icons.edit,
    }[s] ??
    Icons.schedule;
String _actionName(String s) =>
    {
      'approve': 'الموافقة على الطلب',
      'reject': 'رفض الطلب',
      'request-changes': 'طلب تعديل',
      'resubmit': 'إعادة إرسال الطلب',
      'Submitted': 'تم الإرسال',
      'Approved': 'موافقة',
      'Rejected': 'رفض',
      'ChangesRequested': 'طلب تعديل',
      'Delegated': 'تفويض',
      'Commented': 'تعليق',
      'Resubmitted': 'إعادة إرسال',
      'Escalated': 'تصعيد تلقائي',
    }[s] ??
    s;
IconData _actionIcon(String s) =>
    {
      'Approved': Icons.check,
      'Rejected': Icons.close,
      'Delegated': Icons.forward,
      'Commented': Icons.chat_bubble_outline,
      'Escalated': Icons.timer_off,
    }[s] ??
    Icons.circle_outlined;
