import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/api/finance_repository.dart';
import '../../core/theme/app_tokens.dart';

class InvoiceExportScreen extends ConsumerStatefulWidget {
  const InvoiceExportScreen({super.key});

  @override
  ConsumerState<InvoiceExportScreen> createState() =>
      _InvoiceExportScreenState();
}

class _InvoiceExportScreenState extends ConsumerState<InvoiceExportScreen> {
  DateTimeRange? range;
  String? status;
  String format = 'pdf';
  bool exporting = false;

  static const formats = <({String value, String label, IconData icon})>[
    (value: 'pdf', label: 'PDF', icon: Icons.picture_as_pdf_outlined),
    (value: 'xlsx', label: 'Excel', icon: Icons.table_chart_outlined),
    (value: 'csv', label: 'CSV', icon: Icons.description_outlined),
  ];

  static const statuses = <String?, String>{
    null: 'كل الحالات',
    'Draft': 'مسودة',
    'Issued': 'صادرة',
    'PartiallyPaid': 'مدفوعة جزئيًا',
    'Paid': 'مدفوعة',
    'Overdue': 'متأخرة',
    'Cancelled': 'ملغاة',
  };

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('تصدير الفواتير')),
    body: ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
      children: [
        const _ExportHeading(
          icon: Icons.date_range_outlined,
          title: 'الفترة الزمنية',
          subtitle: 'اختر تاريخ إصدار الفواتير المطلوب تصديرها',
        ),
        const SizedBox(height: 12),
        InkWell(
          borderRadius: BorderRadius.circular(16),
          onTap: _pickRange,
          child: Ink(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              border: Border.all(color: AppColors.gray200),
              borderRadius: BorderRadius.circular(AppRadius.lg),
              boxShadow: AppShadows.soft,
            ),
            child: Row(
              children: [
                const CircleAvatar(
                  backgroundColor: AppColors.primaryTint,
                  child: Icon(
                    Icons.calendar_month_outlined,
                    color: AppColors.primary,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        range == null ? 'كل التواريخ' : _rangeLabel(),
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(height: 3),
                      Text(
                        range == null
                            ? 'اضغط لاختيار تاريخ البداية والنهاية'
                            : '${range!.duration.inDays + 1} يوم',
                        style: const TextStyle(
                          color: AppColors.gray500,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
                if (range != null)
                  IconButton(
                    tooltip: 'إلغاء الفترة',
                    onPressed: () => setState(() => range = null),
                    icon: const Icon(Icons.close),
                  )
                else
                  const Icon(Icons.chevron_left),
              ],
            ),
          ),
        ),
        const SizedBox(height: 24),
        const _ExportHeading(
          icon: Icons.filter_alt_outlined,
          title: 'حالة الفاتورة',
          subtitle: 'يمكنك تصدير كل الحالات أو حالة محددة',
        ),
        const SizedBox(height: 12),
        DropdownButtonFormField<String?>(
          value: status,
          decoration: const InputDecoration(
            prefixIcon: Icon(Icons.receipt_long_outlined),
            labelText: 'الحالة',
          ),
          items: statuses.entries
              .map(
                (entry) => DropdownMenuItem<String?>(
                  value: entry.key,
                  child: Text(entry.value),
                ),
              )
              .toList(),
          onChanged: (value) => setState(() => status = value),
        ),
        const SizedBox(height: 24),
        const _ExportHeading(
          icon: Icons.file_download_outlined,
          title: 'صيغة الملف',
          subtitle: 'اختر الصيغة المناسبة لاستخدامك',
        ),
        const SizedBox(height: 12),
        Row(
          children: formats
              .map(
                (item) => Expanded(
                  child: Padding(
                    padding: EdgeInsetsDirectional.only(
                      end: item == formats.last ? 0 : 8,
                    ),
                    child: _FormatCard(
                      label: item.label,
                      icon: item.icon,
                      selected: format == item.value,
                      onTap: () => setState(() => format = item.value),
                    ),
                  ),
                ),
              )
              .toList(),
        ),
        const SizedBox(height: 28),
        FilledButton.icon(
          onPressed: exporting ? null : _export,
          icon: exporting
              ? const SizedBox.square(
                  dimension: 20,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.white,
                  ),
                )
              : const Icon(Icons.download_rounded),
          label: Text(exporting ? 'جاري تجهيز الملف...' : 'تصدير وحفظ الملف'),
        ),
        const SizedBox(height: 10),
        const Text(
          'سيحتوي الملف على الفواتير المطابقة للفترة والحالة المختارتين فقط.',
          textAlign: TextAlign.center,
          style: TextStyle(color: AppColors.gray500, fontSize: 11),
        ),
      ],
    ),
  );

  String _rangeLabel() {
    final formatter = DateFormat('d MMM yyyy', 'ar');
    return '${formatter.format(range!.start)} — ${formatter.format(range!.end)}';
  }

  Future<void> _pickRange() async {
    final now = DateTime.now();
    final selected = await showDateRangePicker(
      context: context,
      firstDate: DateTime(now.year - 10),
      lastDate: DateTime(now.year + 1, 12, 31),
      initialDateRange: range,
      helpText: 'حدد فترة الفواتير',
      cancelText: 'إلغاء',
      confirmText: 'اختيار',
      saveText: 'حفظ',
    );
    if (selected != null && mounted) setState(() => range = selected);
  }

  Future<void> _export() async {
    setState(() => exporting = true);
    try {
      final bytes = await ref
          .read(financeRepositoryProvider)
          .export(
            format: format,
            status: status,
            from: range?.start,
            to: range?.end,
          );
      final stamp = DateFormat('yyyyMMdd-HHmm').format(DateTime.now());
      final path = await FilePicker.saveFile(
        dialogTitle: 'حفظ ملف الفواتير',
        fileName: 'invoices-$stamp.$format',
        type: FileType.custom,
        allowedExtensions: [format],
        bytes: bytes,
      );
      if (path != null && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('تم تصدير الفواتير وحفظ الملف بنجاح')),
        );
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$error')));
      }
    } finally {
      if (mounted) setState(() => exporting = false);
    }
  }
}

class _ExportHeading extends StatelessWidget {
  const _ExportHeading({
    required this.icon,
    required this.title,
    required this.subtitle,
  });

  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Icon(icon, color: AppColors.primary),
      const SizedBox(width: 10),
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
            ),
            const SizedBox(height: 2),
            Text(
              subtitle,
              style: const TextStyle(color: AppColors.gray500, fontSize: 11),
            ),
          ],
        ),
      ),
    ],
  );
}

class _FormatCard extends StatelessWidget {
  const _FormatCard({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    borderRadius: BorderRadius.circular(16),
    onTap: onTap,
    child: AnimatedContainer(
      duration: const Duration(milliseconds: 160),
      padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 8),
      decoration: BoxDecoration(
        color: selected ? AppColors.primaryTint : Colors.white,
        border: Border.all(
          color: selected ? AppColors.primary : AppColors.gray200,
          width: selected ? 1.5 : 1,
        ),
        borderRadius: BorderRadius.circular(AppRadius.lg),
        boxShadow: selected ? AppShadows.soft : const [],
      ),
      child: Column(
        children: [
          Icon(
            icon,
            size: 30,
            color: selected ? AppColors.primary : AppColors.gray500,
          ),
          const SizedBox(height: 8),
          Text(label, style: const TextStyle(fontWeight: FontWeight.w700)),
          const SizedBox(height: 6),
          Icon(
            selected ? Icons.check_circle : Icons.circle_outlined,
            size: 18,
            color: selected ? AppColors.primary : AppColors.gray200,
          ),
        ],
      ),
    ),
  );
}
