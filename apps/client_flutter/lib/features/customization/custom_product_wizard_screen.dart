import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/api/customization_repository.dart';
import '../../core/theme/app_tokens.dart';

class CustomProductWizardScreen extends ConsumerStatefulWidget {
  const CustomProductWizardScreen({super.key, required this.templateId});
  final String templateId;
  @override
  ConsumerState<CustomProductWizardScreen> createState() =>
      _CustomProductWizardScreenState();
}

class _CustomProductWizardScreenState
    extends ConsumerState<CustomProductWizardScreen> {
  int _step = 0, _quantity = 100;
  int _printColorCount = 1;
  String? _placement, _method, _material, _color, _size;
  bool _designService = false, _submitting = false;
  PlatformFile? _logo, _design;
  SavedLogo? _savedLogo;
  final _customText = TextEditingController(),
      _note = TextEditingController(),
      _objective = TextEditingController(),
      _audience = TextEditingController(),
      _colors = TextEditingController(),
      _requiredText = TextEditingController(),
      _printWidth = TextEditingController(text: '5'),
      _printHeight = TextEditingController(text: '5');

  @override
  void dispose() {
    for (final c in [
      _customText,
      _note,
      _objective,
      _audience,
      _colors,
      _requiredText,
      _printWidth,
      _printHeight,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final template = ref.watch(customTemplateProvider(widget.templateId));
    return Scaffold(
      appBar: AppBar(title: const Text('تخصيص المنتج')),
      body: template.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(child: Text('$error')),
        data: (t) => Stepper(
          currentStep: _step,
          type: StepperType.horizontal,
          onStepTapped: (value) => setState(() => _step = value),
          controlsBuilder: (context, details) => Padding(
            padding: const EdgeInsets.only(top: 18),
            child: Row(
              children: [
                Expanded(
                  child: FilledButton(
                    onPressed: _submitting ? null : () => _continue(t),
                    child: Text(_step == 3 ? 'إرسال طلب التسعير' : 'التالي'),
                  ),
                ),
                if (_step > 0) ...[
                  const SizedBox(width: 8),
                  OutlinedButton(
                    onPressed: () => setState(() => _step--),
                    child: const Text('السابق'),
                  ),
                ],
              ],
            ),
          ),
          steps: [
            Step(
              title: const Text('المنتج'),
              isActive: _step >= 0,
              content: _productStep(t),
            ),
            Step(
              title: const Text('الطباعة'),
              isActive: _step >= 1,
              content: _printStep(t),
            ),
            Step(
              title: const Text('الملفات'),
              isActive: _step >= 2,
              content: _filesStep(),
            ),
            Step(
              title: const Text('المراجعة'),
              isActive: _step >= 3,
              content: _review(t),
            ),
          ],
        ),
      ),
    );
  }

  Widget _productStep(CustomTemplate t) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      _Header(
        icon: Icons.inventory_2_outlined,
        title: t.name,
        subtitle: t.description ?? t.sku,
      ),
      const SizedBox(height: 14),
      const Text(
        'الكمية المطلوبة',
        style: TextStyle(fontWeight: FontWeight.w700),
      ),
      const SizedBox(height: 8),
      Row(
        children: [
          IconButton(
            onPressed: _quantity > t.minQuantity
                ? () => setState(() => _quantity -= 25)
                : null,
            icon: const Icon(Icons.remove_circle_outline),
          ),
          Expanded(
            child: Container(
              padding: const EdgeInsets.symmetric(vertical: 12),
              alignment: Alignment.center,
              decoration: BoxDecoration(
                border: Border.all(color: AppColors.gray200),
                borderRadius: BorderRadius.circular(AppRadius.md),
              ),
              child: Text(
                '$_quantity قطعة',
                style: const TextStyle(fontWeight: FontWeight.w800),
              ),
            ),
          ),
          IconButton(
            onPressed: () => setState(() => _quantity += 25),
            icon: const Icon(Icons.add_circle_outline),
          ),
        ],
      ),
      Text(
        'الحد الأدنى ${t.minQuantity} قطعة',
        style: const TextStyle(color: AppColors.gray500, fontSize: 9),
      ),
      const SizedBox(height: 14),
      _ChoiceField(
        label: 'الخامة',
        value: _material ?? t.materials.first.id,
        items: t.materials,
        onChanged: (v) => setState(() => _material = v),
      ),
      _ChoiceField(
        label: 'اللون',
        value: _color ?? t.colors.first.id,
        items: t.colors,
        onChanged: (v) => setState(() => _color = v),
      ),
      _ChoiceField(
        label: 'المقاس',
        value: _size ?? t.sizes.first.id,
        items: t.sizes,
        onChanged: (v) => setState(() => _size = v),
      ),
    ],
  );

  Widget _printStep(CustomTemplate t) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      _ChoiceField(
        label: 'موضع الطباعة',
        value: _placement ?? t.placements.first.id,
        items: t.placements,
        onChanged: (v) => setState(() => _placement = v),
      ),
      _ChoiceField(
        label: 'نوع الطباعة',
        value: _method ?? t.printMethods.first.id,
        items: t.printMethods,
        onChanged: (v) => setState(() => _method = v),
      ),
      Row(
        children: [
          Expanded(
            child: TextField(
              controller: _printWidth,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(labelText: 'عرض الطباعة (سم)'),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: TextField(
              controller: _printHeight,
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              decoration: const InputDecoration(
                labelText: 'ارتفاع الطباعة (سم)',
              ),
            ),
          ),
        ],
      ),
      const SizedBox(height: 12),
      Text(
        'عدد ألوان الطباعة: $_printColorCount',
        style: const TextStyle(fontWeight: FontWeight.w700),
      ),
      Slider(
        value: _printColorCount.toDouble(),
        min: 1,
        max: 8,
        divisions: 7,
        label: '$_printColorCount',
        onChanged: (value) => setState(() => _printColorCount = value.round()),
      ),
      TextField(
        controller: _customText,
        maxLength: 300,
        maxLines: 2,
        decoration: const InputDecoration(
          labelText: 'نص مطبوع (اختياري)',
          hintText: 'مثال: اسم الشركة أو عبارة الحملة',
        ),
      ),
      const SizedBox(height: 10),
      TextField(
        controller: _note,
        maxLines: 3,
        decoration: const InputDecoration(
          labelText: 'ملاحظات على التنفيذ (اختياري)',
        ),
      ),
    ],
  );

  Widget _filesStep() => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      _UploadTile(
        title: 'شعار الشركة *',
        subtitle:
            _logo?.name ??
            _savedLogo?.name ??
            'PNG، JPG، SVG، PDF أو AI — حتى 15MB',
        selected: _logo != null || _savedLogo != null,
        onTap: () => _pick(true),
      ),
      if (ref.watch(savedLogosProvider).value?.isNotEmpty == true) ...[
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: _chooseSavedLogo,
          icon: const Icon(Icons.collections_bookmark_outlined),
          label: const Text('اختيار شعار محفوظ مسبقًا'),
        ),
      ],
      const SizedBox(height: 12),
      SegmentedButton<bool>(
        segments: const [
          ButtonSegment(
            value: false,
            label: Text('لدي تصميم'),
            icon: Icon(Icons.upload_file_outlined),
          ),
          ButtonSegment(
            value: true,
            label: Text('أطلب مصممًا'),
            icon: Icon(Icons.design_services_outlined),
          ),
        ],
        selected: {_designService},
        onSelectionChanged: (v) => setState(() => _designService = v.first),
      ),
      const SizedBox(height: 12),
      if (!_designService)
        _UploadTile(
          title: 'ملف التصميم *',
          subtitle: _design?.name ?? 'ارفع التصميم الجاهز للطباعة',
          selected: _design != null,
          onTap: () => _pick(false),
        )
      else ...[
        TextField(
          controller: _objective,
          maxLines: 2,
          decoration: const InputDecoration(
            labelText: 'هدف التصميم *',
            hintText: 'ما المناسبة أو الاستخدام؟',
          ),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: _audience,
          decoration: const InputDecoration(labelText: 'الجمهور المستهدف'),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: _colors,
          decoration: const InputDecoration(labelText: 'الألوان المفضلة'),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: _requiredText,
          maxLines: 2,
          decoration: const InputDecoration(
            labelText: 'النص المطلوب في التصميم',
          ),
        ),
      ],
    ],
  );

  Widget _review(CustomTemplate t) {
    final adjustment =
        _find(t.placements, _placement).priceAdjustment +
        _find(t.printMethods, _method).priceAdjustment +
        _find(t.materials, _material).priceAdjustment +
        _find(t.colors, _color).priceAdjustment +
        _find(t.sizes, _size).priceAdjustment +
        (_printColorCount - 1) * 2;
    final estimate = (t.startingPrice + adjustment) * _quantity + t.setupFee;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _Header(
          icon: Icons.fact_check_outlined,
          title: 'راجع طلب التخصيص',
          subtitle: 'سيتم تأكيد السعر النهائي وموعد التسليم قبل الإنتاج',
        ),
        const SizedBox(height: 14),
        _ReviewRow('المنتج', t.name),
        _ReviewRow('الكمية', '$_quantity قطعة'),
        _ReviewRow(
          'الخامة / اللون / المقاس',
          '${_find(t.materials, _material).name} / ${_find(t.colors, _color).name} / ${_find(t.sizes, _size).name}',
        ),
        _ReviewRow(
          'الطباعة',
          '${_find(t.printMethods, _method).name} — ${_find(t.placements, _placement).name}',
        ),
        _ReviewRow(
          'مساحة الطباعة',
          '${_printWidth.text} × ${_printHeight.text} سم — $_printColorCount لون',
        ),
        _ReviewRow(
          'خدمة التصميم',
          _designService ? 'مطلوبة من مهندسيتو' : 'تصميم جاهز مرفوع',
        ),
        const Divider(height: 24),
        Text(
          'التقدير المبدئي: ${NumberFormat('#,##0.00', 'ar').format(estimate)} ج.م',
          style: const TextStyle(
            color: AppColors.primary,
            fontSize: 17,
            fontWeight: FontWeight.w800,
          ),
        ),
        const SizedBox(height: 5),
        const Text(
          'السعر تقديري ويخضع لمراجعة الملفات والخامات والكمية.',
          style: TextStyle(color: AppColors.gray500, fontSize: 9),
        ),
        if (_submitting)
          const Padding(
            padding: EdgeInsets.only(top: 16),
            child: LinearProgressIndicator(),
          ),
      ],
    );
  }

  CustomChoice _find(List<CustomChoice> list, String? id) =>
      list.firstWhere((x) => x.id == (id ?? list.first.id));
  Future<void> _pick(bool logo) async {
    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: const [
        'png',
        'jpg',
        'jpeg',
        'webp',
        'svg',
        'pdf',
        'ai',
      ],
      withData: true,
    );
    if (result != null && mounted) {
      setState(() {
        if (logo) {
          _logo = result.files.single;
          _savedLogo = null;
        } else {
          _design = result.files.single;
        }
      });
    }
  }

  Future<void> _continue(CustomTemplate t) async {
    if (_step < 2) {
      setState(() => _step++);
      return;
    }
    final printWidth = double.tryParse(_printWidth.text);
    final printHeight = double.tryParse(_printHeight.text);
    if (printWidth == null ||
        printHeight == null ||
        printWidth <= 0 ||
        printHeight <= 0) {
      _message('أدخل أبعاد طباعة صحيحة');
      setState(() => _step = 1);
      return;
    }
    if (_step == 2) {
      if ((_logo == null && _savedLogo == null) ||
          (!_designService && _design == null) ||
          (_designService && _objective.text.trim().isEmpty)) {
        _message('أكمل الشعار ومصدر التصميم والبيانات المطلوبة');
        return;
      }
      setState(() => _step++);
      return;
    }
    setState(() => _submitting = true);
    try {
      final created = await ref
          .read(customizationRepositoryProvider)
          .create(
            CreateCustomRequestInput(
              templateId: t.id,
              quantity: _quantity,
              placementId: _find(t.placements, _placement).id,
              printMethodId: _find(t.printMethods, _method).id,
              materialId: _find(t.materials, _material).id,
              colorId: _find(t.colors, _color).id,
              sizeId: _find(t.sizes, _size).id,
              designServiceRequested: _designService,
              logo: _logo,
              existingLogoAssetId: _savedLogo?.id,
              printWidthCm: printWidth,
              printHeightCm: printHeight,
              printColorCount: _printColorCount,
              designFile: _design,
              customText: _customText.text.trim(),
              customerNote: _note.text.trim(),
              objective: _objective.text.trim(),
              audience: _audience.text.trim(),
              preferredColors: _colors.text.trim(),
              requiredText: _requiredText.text.trim(),
            ),
          );
      ref.invalidate(customRequestsProvider);
      if (mounted) context.go('/custom-requests/${created.id}');
    } catch (error) {
      if (mounted) _message('$error');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  void _message(String text) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));

  Future<void> _chooseSavedLogo() async {
    final logos = ref.read(savedLogosProvider).value ?? const <SavedLogo>[];
    final chosen = await showModalBottomSheet<SavedLogo>(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: ListView(
          shrinkWrap: true,
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 20),
          children: [
            const Text(
              'شعارات الشركة المحفوظة',
              style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 8),
            ...logos.map(
              (logo) => ListTile(
                leading: const CircleAvatar(child: Icon(Icons.image_outlined)),
                title: Text(logo.name),
                subtitle: Text(
                  '${(logo.sizeBytes / 1024).toStringAsFixed(0)} كيلوبايت',
                ),
                onTap: () => Navigator.pop(context, logo),
              ),
            ),
          ],
        ),
      ),
    );
    if (chosen != null && mounted) {
      setState(() {
        _savedLogo = chosen;
        _logo = null;
      });
    }
  }
}

class _ChoiceField extends StatelessWidget {
  const _ChoiceField({
    required this.label,
    required this.value,
    required this.items,
    required this.onChanged,
  });
  final String label, value;
  final List<CustomChoice> items;
  final ValueChanged<String?> onChanged;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 12),
    child: DropdownButtonFormField<String>(
      value: value,
      decoration: InputDecoration(labelText: label),
      items: items
          .map(
            (x) => DropdownMenuItem(
              value: x.id,
              child: Text(
                '${x.name}${x.priceAdjustment > 0 ? ' (+${x.priceAdjustment.toStringAsFixed(0)} ج.م)' : ''}',
              ),
            ),
          )
          .toList(),
      onChanged: onChanged,
    ),
  );
}

class _UploadTile extends StatelessWidget {
  const _UploadTile({
    required this.title,
    required this.subtitle,
    required this.selected,
    required this.onTap,
  });
  final String title, subtitle;
  final bool selected;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(AppRadius.md),
    child: Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: selected ? AppColors.successTint : Colors.white,
        borderRadius: BorderRadius.circular(AppRadius.md),
        border: Border.all(
          color: selected ? AppColors.success : AppColors.gray200,
        ),
      ),
      child: Row(
        children: [
          Icon(
            selected ? Icons.check_circle_rounded : Icons.cloud_upload_outlined,
            color: selected ? AppColors.success : AppColors.primary,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                Text(
                  subtitle,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: AppColors.gray500, fontSize: 9),
                ),
              ],
            ),
          ),
        ],
      ),
    ),
  );
}

class _Header extends StatelessWidget {
  const _Header({
    required this.icon,
    required this.title,
    required this.subtitle,
  });
  final IconData icon;
  final String title, subtitle;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: AppColors.primaryTint,
      borderRadius: BorderRadius.circular(AppRadius.md),
    ),
    child: Row(
      children: [
        Icon(icon, color: AppColors.primary, size: 30),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: const TextStyle(fontWeight: FontWeight.w800)),
              Text(
                subtitle,
                style: const TextStyle(
                  color: AppColors.gray600,
                  fontSize: 10,
                  height: 1.5,
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

class _ReviewRow extends StatelessWidget {
  const _ReviewRow(this.label, this.value);
  final String label, value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 7),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 105,
          child: Text(
            label,
            style: const TextStyle(color: AppColors.gray500, fontSize: 10),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 11),
          ),
        ),
      ],
    ),
  );
}
