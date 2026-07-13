import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../core/api/finance_repository.dart';
import '../../core/theme/app_tokens.dart';

class FinanceScreen extends ConsumerStatefulWidget {
  const FinanceScreen({super.key});
  @override
  ConsumerState<FinanceScreen> createState() => _FinanceScreenState();
}

class _FinanceScreenState extends ConsumerState<FinanceScreen>
    with SingleTickerProviderStateMixin {
  late final TabController tabs = TabController(length: 2, vsync: this);
  String? status;
  @override
  void dispose() {
    tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('الفواتير والمدفوعات'),
      actions: [
        IconButton(
          onPressed: () => context.push('/budgets'),
          icon: const Icon(Icons.pie_chart_outline),
          tooltip: 'الميزانيات ومراكز التكلفة',
        ),
        IconButton(
          onPressed: _export,
          icon: const Icon(Icons.file_download_outlined),
          tooltip: 'تصدير Excel',
        ),
      ],
      bottom: TabBar(
        controller: tabs,
        tabs: const [
          Tab(text: 'الملخص المالي'),
          Tab(text: 'الفواتير'),
        ],
      ),
    ),
    body: TabBarView(controller: tabs, children: [_summary(), _invoices()]),
  );
  Widget _summary() => ref
      .watch(financeSummaryProvider)
      .when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('$e')),
        data: (s) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(financeSummaryProvider);
            await ref.read(financeSummaryProvider.future);
          },
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 100),
            children: [
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [AppColors.primary, AppColors.primaryDark],
                  ),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'إجمالي المبالغ المستحقة',
                      style: TextStyle(color: Colors.white70),
                    ),
                    Text(
                      '${money(s.outstanding)} ج.م',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 30,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        Expanded(
                          child: HeroStat(
                            label: 'متأخر',
                            value: s.overdue,
                            color: Colors.orangeAccent,
                          ),
                        ),
                        Expanded(
                          child: HeroStat(
                            label: 'خلال 7 أيام',
                            value: s.dueSoon,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 14),
              CreditCard(summary: s, request: () => _credit(s)),
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(
                    child: Metric(
                      title: 'فواتير مفتوحة',
                      value: '${s.openInvoices}',
                      icon: Icons.receipt_long_outlined,
                      color: AppColors.primary,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Metric(
                      title: 'مدفوعات سابقة',
                      value: '${s.payments.length}',
                      icon: Icons.payments_outlined,
                      color: AppColors.success,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 14),
              FinanceSection(
                title: 'مواعيد السداد القادمة',
                icon: Icons.calendar_month_outlined,
                child: s.upcoming.isEmpty
                    ? const Text('لا توجد مستحقات قادمة')
                    : Column(
                        children: s.upcoming
                            .map(
                              (i) => ListTile(
                                contentPadding: EdgeInsets.zero,
                                onTap: () =>
                                    context.push('/finance/invoices/${i.id}'),
                                leading: CircleAvatar(
                                  backgroundColor: i.overdue
                                      ? AppColors.errorTint
                                      : AppColors.warningTint,
                                  child: Icon(
                                    i.overdue
                                        ? Icons.warning_amber
                                        : Icons.event,
                                    color: i.overdue
                                        ? AppColors.error
                                        : AppColors.warning,
                                  ),
                                ),
                                title: Text(
                                  i.number,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w800,
                                    fontSize: 11,
                                  ),
                                ),
                                subtitle: Text(
                                  i.overdue
                                      ? 'متأخرة منذ ${DateTime.now().difference(i.dueAt).inDays} يوم'
                                      : 'تستحق ${DateFormat('d MMMM', 'ar').format(i.dueAt)}',
                                ),
                                trailing: Text(
                                  '${money(i.outstanding)} ج.م',
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w900,
                                    fontSize: 10,
                                  ),
                                ),
                              ),
                            )
                            .toList(),
                      ),
              ),
              if (s.payments.isNotEmpty) ...[
                const SizedBox(height: 14),
                FinanceSection(
                  title: 'المدفوعات السابقة',
                  icon: Icons.history,
                  child: Column(
                    children: s.payments
                        .map(
                          (p) => ListTile(
                            contentPadding: EdgeInsets.zero,
                            leading: const CircleAvatar(
                              backgroundColor: AppColors.successTint,
                              child: Icon(
                                Icons.check,
                                color: AppColors.success,
                              ),
                            ),
                            title: Text(
                              '${money(p.amount)} ج.م',
                              style: const TextStyle(
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                            subtitle: Text(
                              '${p.reference} • ${DateFormat('d MMM yyyy', 'ar').format(p.createdAt)}',
                            ),
                            trailing: Text(
                              paymentStatusAr(p.status),
                              style: const TextStyle(
                                fontSize: 9,
                                color: AppColors.success,
                              ),
                            ),
                          ),
                        )
                        .toList(),
                  ),
                ),
              ],
            ],
          ),
        ),
      );
  Widget _invoices() {
    final result = ref.watch(invoiceListProvider(status));
    return Column(
      children: [
        SizedBox(
          height: 48,
          child: ListView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 5),
            children: [
              _filter('الكل', null),
              _filter('مستحقة', 'Issued'),
              _filter('جزئيًا', 'PartiallyPaid'),
              _filter('متأخرة', 'Overdue'),
              _filter('مدفوعة', 'Paid'),
            ],
          ),
        ),
        Expanded(
          child: result.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => Center(child: Text('$e')),
            data: (items) => items.isEmpty
                ? const Center(child: Text('لا توجد فواتير'))
                : RefreshIndicator(
                    onRefresh: () async {
                      ref.invalidate(invoiceListProvider(status));
                      await ref.read(invoiceListProvider(status).future);
                    },
                    child: ListView.builder(
                      padding: const EdgeInsets.fromLTRB(14, 8, 14, 100),
                      itemCount: items.length,
                      itemBuilder: (_, i) => InvoiceCard(item: items[i]),
                    ),
                  ),
          ),
        ),
      ],
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
  Future<void> _export() async {
    try {
      final bytes = await ref.read(financeRepositoryProvider).export();
      final path = await FilePicker.saveFile(
        dialogTitle: 'حفظ كشف الفواتير',
        fileName: 'invoices-${DateTime.now().year}.xlsx',
        type: FileType.custom,
        allowedExtensions: const ['xlsx'],
        bytes: bytes,
      );
      if (path != null && mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('تم حفظ كشف الفواتير')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  Future<void> _credit(FinanceSummaryModel s) async {
    final amount = TextEditingController(
          text: (s.creditLimit <= 0 ? 50000 : s.creditLimit * 1.5)
              .round()
              .toString(),
        ),
        reason = TextEditingController();
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheet) => Padding(
        padding: EdgeInsets.fromLTRB(
          18,
          0,
          18,
          MediaQuery.viewInsetsOf(sheet).bottom + 24,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'طلب زيادة الحد الائتماني',
              style: TextStyle(fontSize: 19, fontWeight: FontWeight.w900),
            ),
            Text(
              'الحد الحالي ${money(s.creditLimit)} ج.م',
              style: const TextStyle(color: AppColors.gray500),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: amount,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'الحد المطلوب'),
            ),
            const SizedBox(height: 9),
            TextField(
              controller: reason,
              maxLines: 3,
              decoration: const InputDecoration(
                labelText: 'سبب الطلب وخطة الاستخدام',
              ),
            ),
            const SizedBox(height: 12),
            FilledButton(
              onPressed: () async {
                try {
                  await ref
                      .read(financeRepositoryProvider)
                      .requestCredit(double.parse(amount.text), reason.text);
                  if (!sheet.mounted) return;
                  ScaffoldMessenger.of(sheet).showSnackBar(
                    const SnackBar(content: Text('تم إرسال الطلب للمراجعة')),
                  );
                  Navigator.pop(sheet);
                } catch (e) {
                  if (sheet.mounted) {
                    ScaffoldMessenger.of(
                      sheet,
                    ).showSnackBar(SnackBar(content: Text('$e')));
                  }
                }
              },
              child: const Text('إرسال الطلب'),
            ),
          ],
        ),
      ),
    );
    amount.dispose();
    reason.dispose();
  }
}

class InvoiceCard extends StatelessWidget {
  const InvoiceCard({super.key, required this.item});
  final InvoiceListModel item;
  @override
  Widget build(BuildContext context) => Card(
    margin: const EdgeInsets.only(bottom: 10),
    child: InkWell(
      onTap: () => context.push('/finance/invoices/${item.id}'),
      borderRadius: BorderRadius.circular(16),
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
                        style: const TextStyle(fontWeight: FontWeight.w900),
                      ),
                      Text(
                        'طلب ${item.orderNumber}',
                        style: const TextStyle(
                          fontSize: 9,
                          color: AppColors.gray500,
                        ),
                      ),
                    ],
                  ),
                ),
                InvoiceStatusChip(status: item.status),
              ],
            ),
            const Divider(height: 22),
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'الإجمالي',
                        style: TextStyle(fontSize: 9, color: AppColors.gray500),
                      ),
                      Text(
                        '${money(item.total)} ج.م',
                        style: const TextStyle(
                          fontWeight: FontWeight.w900,
                          color: AppColors.primary,
                        ),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        item.overdue ? 'متأخرة' : 'الاستحقاق',
                        style: TextStyle(
                          fontSize: 9,
                          color: item.overdue
                              ? AppColors.error
                              : AppColors.gray500,
                        ),
                      ),
                      Text(
                        DateFormat('d MMM yyyy', 'ar').format(item.dueAt),
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w800,
                          color: item.overdue ? AppColors.error : null,
                        ),
                      ),
                    ],
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

class InvoiceDetailScreen extends ConsumerWidget {
  const InvoiceDetailScreen({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    appBar: AppBar(
      title: const Text('تفاصيل الفاتورة'),
      actions: [
        IconButton(
          onPressed: () => _share(context, ref),
          icon: const Icon(Icons.share_outlined),
        ),
        IconButton(
          onPressed: () => _pdf(context, ref),
          icon: const Icon(Icons.picture_as_pdf_outlined),
        ),
      ],
    ),
    body: ref
        .watch(invoiceDetailProvider(id))
        .when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Text('$e')),
          data: (i) => RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(invoiceDetailProvider(id));
              await ref.read(invoiceDetailProvider(id).future);
            },
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 110),
              children: [
                Container(
                  padding: const EdgeInsets.all(18),
                  decoration: BoxDecoration(
                    color: i.status == 'Overdue'
                        ? AppColors.errorTint
                        : AppColors.primaryTint,
                    borderRadius: BorderRadius.circular(18),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              i.number,
                              style: const TextStyle(
                                fontWeight: FontWeight.w900,
                                fontSize: 17,
                              ),
                            ),
                          ),
                          InvoiceStatusChip(status: i.status),
                        ],
                      ),
                      Text(
                        'فاتورة ضريبية • ${i.orderNumber}',
                        style: const TextStyle(
                          fontSize: 9,
                          color: AppColors.gray500,
                        ),
                      ),
                      const SizedBox(height: 14),
                      Text(
                        '${money(i.outstanding)} ج.م',
                        style: TextStyle(
                          fontSize: 27,
                          fontWeight: FontWeight.w900,
                          color: i.status == 'Overdue'
                              ? AppColors.error
                              : AppColors.primary,
                        ),
                      ),
                      Text(
                        i.outstanding > 0
                            ? 'المتبقي • الاستحقاق ${DateFormat('d MMMM yyyy', 'ar').format(i.dueAt)}'
                            : 'مدفوعة بالكامل',
                        style: const TextStyle(fontSize: 10),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                FinanceSection(
                  title: 'الفاتورة الإلكترونية',
                  icon: Icons.qr_code_2,
                  child: Row(
                    children: [
                      Container(
                        width: 86,
                        height: 86,
                        color: Colors.white,
                        child: CustomPaint(painter: QrPainter(i.qrPayload)),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'الرقم الضريبي: ${i.sellerTaxNumber}',
                              style: const TextStyle(
                                fontSize: 10,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                            if (i.etaUuid != null)
                              Text(
                                'UUID: ${i.etaUuid}',
                                style: const TextStyle(fontSize: 8),
                              ),
                            const Text(
                              'امسح الرمز للتحقق من الفاتورة',
                              style: TextStyle(
                                fontSize: 9,
                                color: AppColors.gray500,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                FinanceSection(
                  title: 'بنود الفاتورة',
                  icon: Icons.list_alt_outlined,
                  child: Column(
                    children: i.lines
                        .map(
                          (x) => ListTile(
                            contentPadding: EdgeInsets.zero,
                            title: Text(
                              x.description,
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                            subtitle: Text(
                              '${x.sku} • ${x.quantity} × ${money(x.unitPrice)}',
                            ),
                            trailing: Text(
                              '${money(x.total)} ج.م',
                              style: const TextStyle(
                                fontWeight: FontWeight.w900,
                                fontSize: 10,
                              ),
                            ),
                          ),
                        )
                        .toList(),
                  ),
                ),
                const SizedBox(height: 12),
                FinanceSection(
                  title: 'الإجماليات',
                  icon: Icons.calculate_outlined,
                  child: Column(
                    children: [
                      AmountRow('قبل الخصم', i.subtotal),
                      if (i.discount > 0)
                        AmountRow('الخصم', -i.discount, green: true),
                      AmountRow('الضريبة', i.tax),
                      AmountRow('الشحن', i.shipping),
                      const Divider(),
                      AmountRow('إجمالي الفاتورة', i.total, total: true),
                      if (i.paid > 0)
                        AmountRow('المدفوع', -i.paid, green: true),
                      AmountRow('المتبقي', i.outstanding, total: true),
                    ],
                  ),
                ),
                if (i.payments.isNotEmpty) ...[
                  const SizedBox(height: 12),
                  FinanceSection(
                    title: 'تتبع المدفوعات',
                    icon: Icons.account_balance_outlined,
                    child: Column(
                      children: i.payments
                          .map(
                            (p) => ListTile(
                              contentPadding: EdgeInsets.zero,
                              leading: CircleAvatar(
                                backgroundColor: paymentColor(
                                  p.status,
                                ).withValues(alpha: .1),
                                child: Icon(
                                  paymentIcon(p.status),
                                  color: paymentColor(p.status),
                                ),
                              ),
                              title: Text(
                                '${money(p.amount)} ج.م',
                                style: const TextStyle(
                                  fontWeight: FontWeight.w900,
                                ),
                              ),
                              subtitle: Text(
                                '${p.reference} • ${paymentStatusAr(p.status)}',
                              ),
                              trailing: Text(
                                DateFormat('d MMM', 'ar').format(p.createdAt),
                              ),
                            ),
                          )
                          .toList(),
                    ),
                  ),
                ],
                if (i.outstanding > 0 && i.status != 'Draft') ...[
                  const SizedBox(height: 16),
                  FilledButton.icon(
                    onPressed: () => _pay(context, ref, i),
                    icon: const Icon(Icons.account_balance_outlined),
                    label: const Text('الدفع بتحويل بنكي'),
                  ),
                ],
              ],
            ),
          ),
        ),
  );
  Future<void> _pdf(BuildContext context, WidgetRef ref) async {
    try {
      final bytes = await ref.read(financeRepositoryProvider).pdf(id);
      final path = await FilePicker.saveFile(
        dialogTitle: 'حفظ الفاتورة',
        fileName: 'invoice-$id.pdf',
        type: FileType.custom,
        allowedExtensions: const ['pdf'],
        bytes: bytes,
      );
      if (path != null && context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('تم حفظ الفاتورة')));
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  Future<void> _share(BuildContext context, WidgetRef ref) async {
    try {
      final i = await ref.read(invoiceDetailProvider(id).future);
      await Clipboard.setData(
        ClipboardData(
          text:
              'فاتورة ${i.number}\nطلب ${i.orderNumber}\nالإجمالي ${money(i.total)} ج.م\nالمتبقي ${money(i.outstanding)} ج.م\nالاستحقاق ${DateFormat('yyyy-MM-dd').format(i.dueAt)}',
        ),
      );
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('تم نسخ ملخص الفاتورة')));
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  Future<void> _pay(
    BuildContext context,
    WidgetRef ref,
    InvoiceDetailModel invoice,
  ) async {
    final amount = TextEditingController(
          text: invoice.outstanding.toStringAsFixed(0),
        ),
        bank = TextEditingController();
    final start = await showDialog<PaymentStartedModel>(
      context: context,
      builder: (dialog) => AlertDialog(
        title: const Text('بدء تحويل بنكي'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: amount,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'قيمة الدفعة'),
            ),
            const SizedBox(height: 9),
            TextField(
              controller: bank,
              decoration: const InputDecoration(
                labelText: 'مرجع البنك (اختياري)',
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialog),
            child: const Text('إلغاء'),
          ),
          FilledButton(
            onPressed: () async {
              try {
                final result = await ref
                    .read(financeRepositoryProvider)
                    .startPayment(
                      invoice.id,
                      double.parse(amount.text),
                      bank.text,
                    );
                if (!dialog.mounted) return;
                Navigator.pop(dialog, result);
              } catch (e) {
                if (dialog.mounted) {
                  ScaffoldMessenger.of(
                    dialog,
                  ).showSnackBar(SnackBar(content: Text('$e')));
                }
              }
            },
            child: const Text('متابعة'),
          ),
        ],
      ),
    );
    amount.dispose();
    bank.dispose();
    if (start == null || !context.mounted) return;
    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheet) => Padding(
        padding: const EdgeInsets.fromLTRB(18, 0, 18, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'بيانات التحويل',
              style: TextStyle(fontSize: 19, fontWeight: FontWeight.w900),
            ),
            const SizedBox(height: 10),
            InfoRow('البنك', start.bankName),
            InfoRow('اسم الحساب', start.accountName),
            InfoRow('IBAN', start.iban),
            InfoRow('مرجع الدفع', start.reference),
            InfoRow('المبلغ', '${money(start.amount)} ج.م'),
            const SizedBox(height: 10),
            FilledButton.icon(
              onPressed: () async {
                final picked = await FilePicker.pickFiles(
                  type: FileType.custom,
                  allowedExtensions: ['jpg', 'jpeg', 'png', 'pdf'],
                  withData: true,
                );
                if (picked == null) return;
                try {
                  await ref
                      .read(financeRepositoryProvider)
                      .uploadReceipt(start.id, picked.files.single);
                  ref.invalidate(invoiceDetailProvider(invoice.id));
                  ref.invalidate(financeSummaryProvider);
                  if (!sheet.mounted) return;
                  ScaffoldMessenger.of(sheet).showSnackBar(
                    const SnackBar(
                      content: Text('تم رفع الإيصال وجارٍ التحقق'),
                    ),
                  );
                  Navigator.pop(sheet);
                } catch (e) {
                  if (sheet.mounted) {
                    ScaffoldMessenger.of(
                      sheet,
                    ).showSnackBar(SnackBar(content: Text('$e')));
                  }
                }
              },
              icon: const Icon(Icons.upload_file),
              label: const Text('رفع إيصال التحويل'),
            ),
          ],
        ),
      ),
    );
  }
}

class QrPainter extends CustomPainter {
  QrPainter(this.value);
  final String value;
  @override
  void paint(Canvas canvas, Size size) {
    final p = Paint()..color = Colors.black;
    const cells = 17;
    final unit = size.width / cells;
    for (var y = 0; y < cells; y++) {
      for (var x = 0; x < cells; x++) {
        final index = (x + y * cells) % value.length;
        if ((value.codeUnitAt(index) + x * 3 + y * 7) % 3 == 0) {
          canvas.drawRect(Rect.fromLTWH(x * unit, y * unit, unit, unit), p);
        }
      }
    }
  }

  @override
  bool shouldRepaint(covariant QrPainter oldDelegate) =>
      oldDelegate.value != value;
}

class CreditCard extends StatelessWidget {
  const CreditCard({super.key, required this.summary, required this.request});
  final FinanceSummaryModel summary;
  final VoidCallback request;
  @override
  Widget build(BuildContext context) => FinanceSection(
    title: 'الحد الائتماني',
    icon: Icons.credit_score_outlined,
    child: Column(
      children: [
        Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'المتاح',
                    style: TextStyle(fontSize: 9, color: AppColors.gray500),
                  ),
                  Text(
                    '${money(summary.creditAvailable)} ج.م',
                    style: const TextStyle(
                      fontSize: 21,
                      color: AppColors.primary,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ],
              ),
            ),
            SizedBox(
              width: 66,
              height: 66,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  CircularProgressIndicator(
                    value: (summary.utilization / 100).clamp(0, 1),
                    strokeWidth: 7,
                    backgroundColor: AppColors.gray100,
                  ),
                  Text(
                    '${summary.utilization.round()}%',
                    style: const TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        LinearProgressIndicator(
          value: (summary.utilization / 100).clamp(0, 1),
          minHeight: 7,
          borderRadius: BorderRadius.circular(8),
        ),
        const SizedBox(height: 6),
        Row(
          children: [
            Text(
              'مستخدم ${money(summary.creditUsed)}',
              style: const TextStyle(fontSize: 9),
            ),
            const Spacer(),
            Text(
              'الإجمالي ${money(summary.creditLimit)}',
              style: const TextStyle(fontSize: 9),
            ),
          ],
        ),
        const SizedBox(height: 10),
        SizedBox(
          width: double.infinity,
          child: OutlinedButton(
            onPressed: request,
            child: Text(
              summary.creditLimit > 0 ? 'طلب زيادة الحد' : 'التقدم لحد ائتماني',
            ),
          ),
        ),
      ],
    ),
  );
}

class HeroStat extends StatelessWidget {
  const HeroStat({
    super.key,
    required this.label,
    required this.value,
    required this.color,
  });
  final String label;
  final double value;
  final Color color;
  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(label, style: const TextStyle(color: Colors.white60, fontSize: 9)),
      Text(
        '${money(value)} ج.م',
        style: TextStyle(color: color, fontWeight: FontWeight.w900),
      ),
    ],
  );
}

class Metric extends StatelessWidget {
  const Metric({
    super.key,
    required this.title,
    required this.value,
    required this.icon,
    required this.color,
  });
  final String title, value;
  final IconData icon;
  final Color color;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color),
          const SizedBox(height: 8),
          Text(
            value,
            style: TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w900,
              color: color,
            ),
          ),
          Text(
            title,
            style: const TextStyle(fontSize: 9, color: AppColors.gray500),
          ),
        ],
      ),
    ),
  );
}

class FinanceSection extends StatelessWidget {
  const FinanceSection({
    super.key,
    required this.title,
    required this.icon,
    required this.child,
  });
  final String title;
  final IconData icon;
  final Widget child;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, size: 19, color: AppColors.primary),
              const SizedBox(width: 7),
              Text(title, style: const TextStyle(fontWeight: FontWeight.w900)),
            ],
          ),
          const Divider(height: 22),
          child,
        ],
      ),
    ),
  );
}

class AmountRow extends StatelessWidget {
  const AmountRow(
    this.label,
    this.value, {
    this.green = false,
    this.total = false,
    super.key,
  });
  final String label;
  final double value;
  final bool green, total;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: TextStyle(
              fontWeight: total ? FontWeight.w900 : FontWeight.normal,
            ),
          ),
        ),
        Text(
          '${value < 0 ? '-' : ''}${money(value.abs())} ج.م',
          style: TextStyle(
            fontSize: total ? 16 : 10,
            fontWeight: FontWeight.w900,
            color: green
                ? AppColors.success
                : total
                ? AppColors.primary
                : null,
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
      children: [
        SizedBox(
          width: 95,
          child: Text(
            label,
            style: const TextStyle(fontSize: 9, color: AppColors.gray500),
          ),
        ),
        Expanded(
          child: SelectableText(
            value,
            textAlign: TextAlign.end,
            style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w800),
          ),
        ),
      ],
    ),
  );
}

class InvoiceStatusChip extends StatelessWidget {
  const InvoiceStatusChip({super.key, required this.status});
  final String status;
  @override
  Widget build(BuildContext context) {
    final c = invoiceColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
      decoration: BoxDecoration(
        color: c.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        invoiceStatusAr(status),
        style: TextStyle(fontSize: 8, color: c, fontWeight: FontWeight.w800),
      ),
    );
  }
}

String money(double value) => NumberFormat('#,##0.##', 'ar').format(value);
String invoiceStatusAr(String s) =>
    const {
      'Draft': 'مسودة',
      'Issued': 'مستحقة',
      'PartiallyPaid': 'مدفوعة جزئيًا',
      'Paid': 'مدفوعة',
      'Overdue': 'متأخرة',
      'Cancelled': 'ملغاة',
    }[s] ??
    s;
Color invoiceColor(String s) => switch (s) {
  'Paid' => AppColors.success,
  'Overdue' => AppColors.error,
  'PartiallyPaid' => AppColors.warning,
  _ => AppColors.primary,
};
String paymentStatusAr(String s) =>
    const {
      'Initiated': 'بانتظار الإيصال',
      'PendingVerification': 'جارٍ التحقق',
      'Completed': 'تم التأكيد',
      'Rejected': 'يحتاج إعادة الرفع',
      'Failed': 'فشل',
    }[s] ??
    s;
Color paymentColor(String s) => switch (s) {
  'Completed' => AppColors.success,
  'Rejected' || 'Failed' => AppColors.error,
  'PendingVerification' => AppColors.warning,
  _ => AppColors.primary,
};
IconData paymentIcon(String s) => switch (s) {
  'Completed' => Icons.check,
  'Rejected' || 'Failed' => Icons.close,
  'PendingVerification' => Icons.hourglass_top,
  _ => Icons.upload_file,
};
