import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:file_picker/file_picker.dart';

import '../../core/api/cart_repository.dart';
import '../../core/api/checkout_repository.dart';
import '../../core/theme/app_tokens.dart';

class CheckoutScreen extends ConsumerStatefulWidget {
  const CheckoutScreen({super.key});
  @override
  ConsumerState<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends ConsumerState<CheckoutScreen> {
  int _step = 0;
  bool _busy = false;
  bool _terms = false;
  CheckoutOptions? _options;
  CheckoutReview? _review;
  String? _error;
  String? _branchId;
  String? _payment;
  String? _costCenterId;
  String? _projectId;
  CheckoutPaymentAttempt? _paymentAttempt;
  CheckoutAttachment? _poAttachment;
  bool _allowSplit = false;
  String _shipping = 'Standard';
  String _slot = '09:00-12:00';
  DateTime _date = DateTime.now().add(const Duration(days: 2));
  final _receiver = TextEditingController();
  final _phone = TextEditingController();
  final _po = TextEditingController();
  final _reference = TextEditingController();
  final _department = TextEditingController();
  final _orderNote = TextEditingController();
  final _creditPortion = TextEditingController();
  final _cardPortion = TextEditingController();

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _receiver.dispose();
    _phone.dispose();
    _po.dispose();
    _reference.dispose();
    _department.dispose();
    _orderNote.dispose();
    _creditPortion.dispose();
    _cardPortion.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final value = await ref.read(checkoutRepositoryProvider).options();
      if (!mounted) return;
      setState(() {
        _options = value;
        _branchId =
            value.branchId ??
            (value.branches.isEmpty ? null : value.branches.first.id);
        _receiver.text = value.receiverName ?? '';
        _phone.text = value.receiverPhone ?? '';
        _shipping = value.shippingMethod;
        _payment = value.paymentMethod;
        _costCenterId =
            value.costCenterId ??
            (value.costCenters.isEmpty ? null : value.costCenters.first.id);
        _projectId = value.projectId;
        _department.text = value.requestingDepartment ?? '';
        _orderNote.text = value.orderNote ?? '';
        _allowSplit = value.allowSplitDelivery;
        _poAttachment = value.purchaseOrderAttachment;
        _po.text = value.purchaseOrderNumber ?? '';
        _reference.text = value.internalReference ?? '';
      });
    } catch (error) {
      if (mounted) setState(() => _error = '$error');
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(
        _step == 0
            ? 'بيانات التوصيل'
            : _step == 1
            ? 'طريقة الدفع'
            : 'مراجعة الطلب',
      ),
    ),
    body: _error != null
        ? _errorView()
        : _options == null
        ? const Center(child: CircularProgressIndicator())
        : Column(
            children: [
              _steps(),
              Expanded(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: _step == 0
                      ? _delivery()
                      : _step == 1
                      ? _paymentStep()
                      : _reviewStep(),
                ),
              ),
            ],
          ),
    bottomNavigationBar: _options == null || _error != null ? null : _bottom(),
  );

  Widget _steps() => Padding(
    padding: const EdgeInsets.fromLTRB(28, 12, 28, 4),
    child: Row(
      children: [
        _dot(0, 'التوصيل'),
        Expanded(
          child: Divider(
            color: _step >= 1 ? AppColors.primary : AppColors.gray200,
            thickness: 2,
          ),
        ),
        _dot(1, 'الدفع'),
        Expanded(
          child: Divider(
            color: _step >= 2 ? AppColors.primary : AppColors.gray200,
            thickness: 2,
          ),
        ),
        _dot(2, 'المراجعة'),
      ],
    ),
  );

  Widget _dot(int index, String label) => Column(
    children: [
      CircleAvatar(
        radius: 14,
        backgroundColor: _step >= index ? AppColors.primary : AppColors.gray200,
        child: _step > index
            ? const Icon(Icons.check, size: 15, color: Colors.white)
            : Text(
                '${index + 1}',
                style: TextStyle(
                  color: _step >= index ? Colors.white : AppColors.gray500,
                  fontSize: 10,
                ),
              ),
      ),
      const SizedBox(height: 3),
      Text(
        label,
        style: TextStyle(
          fontSize: 8,
          fontWeight: _step == index ? FontWeight.w800 : FontWeight.w400,
        ),
      ),
    ],
  );

  Widget _delivery() => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const Text(
        'الجهة الطالبة والميزانية',
        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 10),
      DropdownButtonFormField<String>(
        value: _costCenterId,
        decoration: const InputDecoration(labelText: 'مركز التكلفة *'),
        items: _options!.costCenters
            .map(
              (center) => DropdownMenuItem(
                value: center.id,
                child: Text('${center.code} — ${center.name}'),
              ),
            )
            .toList(),
        onChanged: (value) => setState(() => _costCenterId = value),
      ),
      if (_costCenterId != null) ...[
        const SizedBox(height: 5),
        Text(
          'المتاح: ${_money(_options!.costCenters.firstWhere((x) => x.id == _costCenterId).available)} ج.م',
          style: const TextStyle(
            color: AppColors.success,
            fontSize: 10,
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
      const SizedBox(height: 10),
      DropdownButtonFormField<String?>(
        value: _projectId,
        decoration: const InputDecoration(labelText: 'المشروع (اختياري)'),
        items: [
          const DropdownMenuItem<String?>(
            value: null,
            child: Text('بدون مشروع محدد'),
          ),
          ..._options!.projects.map(
            (project) => DropdownMenuItem<String?>(
              value: project.id,
              child: Text('${project.code} — ${project.name}'),
            ),
          ),
        ],
        onChanged: (value) => setState(() => _projectId = value),
      ),
      const SizedBox(height: 10),
      TextField(
        controller: _department,
        decoration: const InputDecoration(
          labelText: 'الإدارة الطالبة *',
          prefixIcon: Icon(Icons.apartment_outlined),
        ),
      ),
      const SizedBox(height: 10),
      TextField(
        controller: _orderNote,
        maxLines: 2,
        decoration: const InputDecoration(
          labelText: 'ملاحظات على الطلب',
          prefixIcon: Icon(Icons.notes_rounded),
        ),
      ),
      const Divider(height: 30),
      const Text(
        'عناوين شركتكم',
        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 10),
      ..._options!.branches.map(
        (branch) => Card(
          child: RadioListTile<String>(
            value: branch.id,
            groupValue: _branchId,
            onChanged: (value) => setState(() => _branchId = value),
            title: Text(
              branch.name,
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
            subtitle: Text(branch.address),
            secondary: branch.isMain
                ? const Chip(
                    label: Text('الرئيسي', style: TextStyle(fontSize: 8)),
                  )
                : null,
          ),
        ),
      ),
      OutlinedButton.icon(
        onPressed: _showAddAddress,
        icon: const Icon(Icons.add_location_alt_outlined),
        label: const Text('إضافة عنوان جديد وتحديد الموقع'),
      ),
      const SizedBox(height: 14),
      TextField(
        controller: _receiver,
        decoration: const InputDecoration(
          labelText: 'مسؤول الاستلام',
          prefixIcon: Icon(Icons.person_outline),
        ),
      ),
      const SizedBox(height: 10),
      TextField(
        controller: _phone,
        keyboardType: TextInputType.phone,
        decoration: const InputDecoration(
          labelText: 'رقم هاتف المستلم',
          prefixIcon: Icon(Icons.phone_outlined),
        ),
      ),
      const SizedBox(height: 16),
      ListTile(
        contentPadding: EdgeInsets.zero,
        title: const Text(
          'موعد التوصيل',
          style: TextStyle(fontWeight: FontWeight.w800),
        ),
        subtitle: Text(DateFormat('EEEE، d MMMM yyyy', 'ar').format(_date)),
        trailing: const Icon(Icons.calendar_month_outlined),
        onTap: _pickDate,
      ),
      const Text(
        'الفترة الزمنية',
        style: TextStyle(fontWeight: FontWeight.w800),
      ),
      ...['09:00-12:00', '12:00-15:00', '15:00-18:00'].map(
        (slot) => RadioListTile<String>(
          value: slot,
          groupValue: _slot,
          onChanged: (value) => setState(() => _slot = value!),
          title: Text(slot),
          dense: true,
        ),
      ),
      const Divider(height: 28),
      const Text(
        'طريقة الشحن',
        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
      ),
      ...[
        _choice('Standard', 'التوصيل القياسي', 'مجاني فوق 2,000 ج.م'),
        _choice('Express', 'التوصيل السريع', '150 ج.م'),
        _choice('Pickup', 'الاستلام من المخزن', 'مجاني'),
      ].map(
        (choice) => RadioListTile<String>(
          value: choice.$1,
          groupValue: _shipping,
          onChanged: (value) => setState(() => _shipping = value!),
          title: Text(choice.$2),
          subtitle: Text(choice.$3),
          secondary: Icon(
            choice.$1 == 'Express'
                ? Icons.bolt_rounded
                : choice.$1 == 'Pickup'
                ? Icons.warehouse_outlined
                : Icons.local_shipping_outlined,
            color: AppColors.primary,
          ),
        ),
      ),
      SwitchListTile(
        contentPadding: EdgeInsets.zero,
        value: _allowSplit,
        onChanged: (value) => setState(() => _allowSplit = value),
        title: const Text('السماح بتقسيم التوصيل'),
        subtitle: const Text(
          'يمكن شحن الأصناف المتاحة أولًا عند اختلاف مواعيد التوريد',
        ),
      ),
    ],
  );

  Widget _paymentStep() => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      const Text(
        'اختر طريقة الدفع',
        style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 10),
      ..._options!.payments.map(
        (payment) => Card(
          color: payment.enabled ? Colors.white : AppColors.gray100,
          child: RadioListTile<String>(
            value: payment.code,
            groupValue: _payment,
            onChanged: payment.enabled
                ? (value) {
                    setState(() {
                      _payment = value;
                      _paymentAttempt = null;
                      if (value == 'Partial') {
                        final center = _selectedCostCenter;
                        final credit = center == null
                            ? 0.0
                            : center.available.clamp(0, _payable).toDouble();
                        _creditPortion.text = credit.toStringAsFixed(2);
                        _cardPortion.text = (_payable - credit).toStringAsFixed(
                          2,
                        );
                      }
                    });
                  }
                : null,
            title: Text(
              payment.name,
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
            subtitle: payment.reason == null
                ? null
                : Text(
                    payment.reason!,
                    style: const TextStyle(color: AppColors.error),
                  ),
            secondary: Icon(
              _paymentIcon(payment.code),
              color: payment.enabled ? AppColors.primary : AppColors.gray400,
            ),
          ),
        ),
      ),
      if (_payment == 'Card' || _payment == 'Partial') ...[
        const SizedBox(height: 10),
        if (_payment == 'Partial') ...[
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _creditPortion,
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  decoration: const InputDecoration(
                    labelText: 'من الحد الائتماني',
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextField(
                  controller: _cardPortion,
                  keyboardType: const TextInputType.numberWithOptions(
                    decimal: true,
                  ),
                  decoration: const InputDecoration(labelText: 'بالبطاقة'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
        ],
        Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: _paymentAttempt?.status == 'Succeeded'
                ? AppColors.successTint
                : AppColors.primaryTint,
            borderRadius: BorderRadius.circular(AppRadius.md),
          ),
          child: Row(
            children: [
              Icon(
                _paymentAttempt?.status == 'Succeeded'
                    ? Icons.verified_rounded
                    : Icons.credit_card_rounded,
                color: _paymentAttempt?.status == 'Succeeded'
                    ? AppColors.success
                    : AppColors.primary,
              ),
              const SizedBox(width: 9),
              Expanded(
                child: Text(
                  _paymentAttempt?.status == 'Succeeded'
                      ? 'تم اعتماد الدفع الإلكتروني — ${_paymentAttempt!.reference}'
                      : 'بيانات البطاقة تُعالج عبر رمز آمن ولا تُحفظ على خوادمنا.',
                  style: const TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              TextButton(
                onPressed: _busy ? null : _preparePayment,
                child: Text(
                  _paymentAttempt?.status == 'Succeeded'
                      ? 'إعادة'
                      : 'إتمام الدفع',
                ),
              ),
            ],
          ),
        ),
      ],
      const SizedBox(height: 18),
      TextField(
        controller: _po,
        decoration: const InputDecoration(
          labelText: 'رقم أمر الشراء الداخلي PO (اختياري)',
          prefixIcon: Icon(Icons.description_outlined),
        ),
      ),
      const SizedBox(height: 8),
      OutlinedButton.icon(
        onPressed: _pickPurchaseOrder,
        icon: Icon(
          _poAttachment == null
              ? Icons.upload_file_outlined
              : Icons.check_circle_outline,
        ),
        label: Text(_poAttachment?.name ?? 'رفع ملف أمر الشراء PDF أو صورة'),
      ),
      const SizedBox(height: 10),
      TextField(
        controller: _reference,
        decoration: const InputDecoration(
          labelText: 'مرجع داخلي / مركز التكلفة (اختياري)',
          prefixIcon: Icon(Icons.account_balance_wallet_outlined),
        ),
      ),
    ],
  );

  Widget _reviewStep() {
    if (_review == null) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(40),
          child: CircularProgressIndicator(),
        ),
      );
    }
    final review = _review!;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _section(
          'الأصناف (${review.items.length})',
          Column(
            children: review.items
                .map(
                  (item) => Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Expanded(child: Text(item.name)),
                        Text('${item.quantity} × ${_money(item.unitPrice)}'),
                      ],
                    ),
                  ),
                )
                .toList(),
          ),
        ),
        _section(
          'التوصيل',
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                review.branchName,
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              Text(review.address),
              Text(
                '${DateFormat('d MMMM', 'ar').format(review.requiredDate)} - ${review.timeSlot}',
              ),
            ],
          ),
        ),
        _section(
          'الدفع',
          Text(
            '${_paymentName(review.paymentMethod)}${review.poNumber == null ? '' : '\nPO: ${review.poNumber}'}',
          ),
        ),
        _section(
          'الجهة الطالبة',
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                review.costCenterName ?? '—',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              if (review.projectName != null)
                Text('المشروع: ${review.projectName}'),
              if (review.requestingDepartment != null)
                Text('الإدارة: ${review.requestingDepartment}'),
              if (review.orderNote?.isNotEmpty == true)
                Text('ملاحظة: ${review.orderNote}'),
              if (review.purchaseOrderAttachment != null)
                Text('أمر الشراء: ${review.purchaseOrderAttachment!.name}'),
              if (review.allowSplitDelivery)
                const Text(
                  'مسموح بتقسيم التوصيل',
                  style: TextStyle(color: AppColors.primary),
                ),
            ],
          ),
        ),
        _section(
          'الملخص',
          Column(
            children: [
              _sum('الأصناف', review.subtotal),
              if (review.savings > 0) _sum('وفر الكميات', -review.savings),
              _sum('الضريبة (مشمولة)', review.tax),
              _sum('الشحن', review.shipping),
              const Divider(),
              _sum('الإجمالي', review.total, strong: true),
            ],
          ),
        ),
        if (review.requiresApproval)
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.warningTint,
              borderRadius: BorderRadius.circular(AppRadius.md),
            ),
            child: Row(
              children: [
                const Icon(Icons.info_outline, color: AppColors.warning),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    review.budgetExceeded
                        ? 'إجمالي الطلب يتجاوز المتاح في الميزانية وسيُرسل للموافقة الداخلية.'
                        : 'سيُرسل هذا الطلب للموافقة الداخلية قبل التنفيذ.',
                  ),
                ),
              ],
            ),
          ),
        CheckboxListTile(
          value: _terms,
          onChanged: (value) => setState(() => _terms = value ?? false),
          controlAffinity: ListTileControlAffinity.leading,
          title: const Text('أوافق على تفاصيل الطلب والعنوان وسياسة الاسترجاع'),
        ),
      ],
    );
  }

  Widget _section(String title, Widget child) => Card(
    margin: const EdgeInsets.only(bottom: 10),
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: const TextStyle(fontWeight: FontWeight.w800)),
          const Divider(),
          child,
        ],
      ),
    ),
  );
  Widget _sum(String name, double value, {bool strong = false}) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      children: [
        Expanded(
          child: Text(
            name,
            style: TextStyle(
              fontWeight: strong ? FontWeight.w800 : FontWeight.w400,
            ),
          ),
        ),
        Text(
          '${value < 0 ? '-' : ''}${_money(value.abs())} ج.م',
          style: TextStyle(
            fontSize: strong ? 17 : 13,
            fontWeight: FontWeight.w800,
            color: strong ? AppColors.primary : null,
          ),
        ),
      ],
    ),
  );

  Widget _bottom() => SafeArea(
    child: Container(
      padding: const EdgeInsets.all(12),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: AppColors.gray200)),
      ),
      child: Row(
        children: [
          Expanded(
            child: Text(
              '${_money(_review?.total ?? _options!.cart.total)} ج.م',
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w900),
            ),
          ),
          Expanded(
            child: FilledButton(
              onPressed: _busy || (_step == 2 && !_terms) ? null : _continue,
              child: Text(
                _busy
                    ? 'جاري الحفظ...'
                    : _step == 2
                    ? 'تأكيد الطلب'
                    : 'متابعة',
              ),
            ),
          ),
        ],
      ),
    ),
  );

  Future<void> _continue() async {
    setState(() => _busy = true);
    try {
      final repository = ref.read(checkoutRepositoryProvider);
      if (_step == 0) {
        if (_branchId == null ||
            _costCenterId == null ||
            _department.text.trim().isEmpty ||
            _receiver.text.trim().isEmpty ||
            _phone.text.trim().isEmpty) {
          throw StateError('أكمل بيانات التوصيل');
        }
        _options = await repository.context(
          costCenterId: _costCenterId!,
          projectId: _projectId,
          department: _department.text.trim(),
          orderNote: _orderNote.text.trim().isEmpty
              ? null
              : _orderNote.text.trim(),
          internalReference: _reference.text.trim().isEmpty
              ? null
              : _reference.text.trim(),
        );
        _options = await repository.delivery(
          branchId: _branchId!,
          receiverName: _receiver.text,
          receiverPhone: _phone.text,
          requiredDate: _date,
          timeSlot: _slot,
          shippingMethod: _shipping,
          allowSplitDelivery: _allowSplit,
        );
        setState(() => _step = 1);
      } else if (_step == 1) {
        if (_payment == null) throw StateError('اختر طريقة الدفع');
        if ((_payment == 'Card' || _payment == 'Partial') &&
            _paymentAttempt?.status != 'Succeeded') {
          throw StateError('أكمل عملية الدفع الإلكتروني أولًا');
        }
        _options = await repository.payment(
          method: _payment!,
          poNumber: _po.text.trim().isEmpty ? null : _po.text,
          internalReference: _reference.text.trim().isEmpty
              ? null
              : _reference.text,
          paymentAttemptId: _paymentAttempt?.id,
          creditPortion: double.tryParse(_creditPortion.text),
          cardPortion: _payment == 'Card'
              ? _payable
              : double.tryParse(_cardPortion.text),
        );
        _review = await repository.review();
        setState(() => _step = 2);
      } else {
        final confirmed = await showDialog<bool>(
          context: context,
          builder: (dialogContext) => AlertDialog(
            icon: const Icon(
              Icons.shopping_cart_checkout_rounded,
              color: AppColors.warning,
              size: 38,
            ),
            title: const Text('تأكيد إرسال الطلب؟'),
            content: Text(
              '${_review!.items.length} أصناف - ${_money(_review!.total)} ج.م\n${_review!.requiresApproval ? 'سيُرسل للموافقة الداخلية أولًا.' : 'سيبدأ تجهيز الطلب بعد التأكيد.'}',
              textAlign: TextAlign.center,
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(dialogContext, false),
                child: const Text('مراجعة مرة أخرى'),
              ),
              FilledButton(
                onPressed: () => Navigator.pop(dialogContext, true),
                child: const Text('تأكيد وإرسال'),
              ),
            ],
          ),
        );
        if (confirmed != true) return;
        final order = await repository.submit();
        ref.invalidate(cartProvider);
        if (mounted) {
          context.go('/checkout/success', extra: order);
        }
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$error')));
      }
    } finally {
      if (mounted) {
        setState(() => _busy = false);
      }
    }
  }

  CheckoutCostCenter? get _selectedCostCenter {
    if (_costCenterId == null) return null;
    for (final center in _options!.costCenters) {
      if (center.id == _costCenterId) return center;
    }
    return null;
  }

  double get _payable {
    final subtotal = _options!.cart.subtotal;
    final shipping = switch (_shipping) {
      'Pickup' => 0.0,
      'Express' => 150.0,
      _ => subtotal >= 2000 ? 0.0 : 150.0,
    };
    return subtotal + shipping;
  }

  Future<void> _showAddAddress() async {
    final name = TextEditingController();
    final governorate = TextEditingController(text: 'القاهرة');
    final city = TextEditingController();
    final address = TextEditingController();
    final phone = TextEditingController();
    double? latitude;
    double? longitude;
    final result = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('إضافة عنوان توصيل'),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextField(
                    controller: name,
                    decoration: const InputDecoration(labelText: 'اسم الفرع *'),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: governorate,
                          decoration: const InputDecoration(
                            labelText: 'المحافظة *',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextField(
                          controller: city,
                          decoration: const InputDecoration(
                            labelText: 'المدينة *',
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  TextField(
                    controller: address,
                    maxLines: 2,
                    decoration: const InputDecoration(
                      labelText: 'العنوان بالتفصيل *',
                    ),
                  ),
                  const SizedBox(height: 8),
                  TextField(
                    controller: phone,
                    keyboardType: TextInputType.phone,
                    decoration: const InputDecoration(labelText: 'هاتف الفرع'),
                  ),
                  const SizedBox(height: 12),
                  GestureDetector(
                    onTapDown: (details) {
                      final box = context.findRenderObject() as RenderBox?;
                      final width = box?.size.width ?? 300;
                      setDialogState(() {
                        longitude =
                            29.8 + (details.localPosition.dx / width) * 1.8;
                        latitude =
                            31.0 - (details.localPosition.dy / 140) * 1.5;
                      });
                    },
                    child: Container(
                      height: 140,
                      width: double.infinity,
                      decoration: BoxDecoration(
                        color: AppColors.primaryTint,
                        borderRadius: BorderRadius.circular(AppRadius.md),
                        border: Border.all(
                          color: AppColors.primary.withValues(alpha: .3),
                        ),
                      ),
                      child: Stack(
                        alignment: Alignment.center,
                        children: [
                          const Icon(
                            Icons.map_outlined,
                            size: 72,
                            color: AppColors.gray300,
                          ),
                          if (latitude == null)
                            const Text(
                              'اضغط لتحديد الموقع التقريبي على الخريطة',
                            )
                          else
                            Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Icon(
                                  Icons.location_pin,
                                  color: AppColors.error,
                                  size: 38,
                                ),
                                Text(
                                  '${latitude!.toStringAsFixed(5)}, ${longitude!.toStringAsFixed(5)}',
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                              ],
                            ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('إلغاء'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(dialogContext, true),
              child: const Text('حفظ العنوان'),
            ),
          ],
        ),
      ),
    );
    if (result != true) return;
    if (name.text.trim().isEmpty ||
        city.text.trim().isEmpty ||
        governorate.text.trim().isEmpty ||
        address.text.trim().isEmpty) {
      name.dispose();
      governorate.dispose();
      city.dispose();
      address.dispose();
      phone.dispose();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('أكمل بيانات العنوان المطلوبة')),
        );
      }
      return;
    }
    setState(() => _busy = true);
    try {
      final updated = await ref
          .read(checkoutRepositoryProvider)
          .addAddress(
            name: name.text.trim(),
            governorate: governorate.text.trim(),
            city: city.text.trim(),
            addressLine: address.text.trim(),
            phone: phone.text.trim().isEmpty ? null : phone.text.trim(),
            latitude: latitude,
            longitude: longitude,
          );
      if (!mounted) return;
      setState(() {
        _options = updated;
        _branchId = updated.branches
            .firstWhere((branch) => branch.name == name.text.trim())
            .id;
      });
    } finally {
      name.dispose();
      governorate.dispose();
      city.dispose();
      address.dispose();
      phone.dispose();
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pickPurchaseOrder() async {
    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: const ['pdf', 'png', 'jpg', 'jpeg'],
      withData: true,
    );
    if (result == null) return;
    setState(() => _busy = true);
    try {
      final attachment = await ref
          .read(checkoutRepositoryProvider)
          .uploadPurchaseOrder(result.files.single);
      if (mounted) setState(() => _poAttachment = attachment);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('تعذر رفع أمر الشراء: $error')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _preparePayment() async {
    final isPartial = _payment == 'Partial';
    final credit = double.tryParse(_creditPortion.text) ?? 0;
    final card = isPartial ? double.tryParse(_cardPortion.text) ?? 0 : _payable;
    if (card <= 0 ||
        (isPartial &&
            (credit <= 0 || (credit + card - _payable).abs() > .01))) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('وزّع إجمالي الطلب كاملًا بين الحد الائتماني والبطاقة'),
        ),
      );
      return;
    }
    if (isPartial && credit > (_selectedCostCenter?.available ?? 0)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('الجزء الائتماني يتجاوز المتاح في مركز التكلفة'),
        ),
      );
      return;
    }
    setState(() => _busy = true);
    try {
      final repository = ref.read(checkoutRepositoryProvider);
      final created = await repository.createPayment(card);
      final confirmed = await repository.confirmPayment(created.id);
      if (!mounted) return;
      setState(() => _paymentAttempt = confirmed);
      if (confirmed.status != 'Succeeded') {
        throw StateError(confirmed.failureMessage ?? 'فشلت عملية الدفع');
      }
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('تعذر إتمام الدفع: $error')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _pickDate() async {
    final date = await showDatePicker(
      context: context,
      firstDate: DateTime.now().add(const Duration(days: 1)),
      lastDate: DateTime.now().add(const Duration(days: 90)),
      initialDate: _date,
    );
    if (date != null) setState(() => _date = date);
  }

  Widget _errorView() => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.error_outline, size: 50, color: AppColors.error),
          const SizedBox(height: 10),
          Text(_error!, textAlign: TextAlign.center),
          OutlinedButton(
            onPressed: () {
              setState(() {
                _error = null;
                _options = null;
              });
              _load();
            },
            child: const Text('إعادة المحاولة'),
          ),
        ],
      ),
    ),
  );
  IconData _paymentIcon(String code) => switch (code) {
    'CreditLine' => Icons.account_balance_wallet_outlined,
    'BankTransfer' => Icons.account_balance_outlined,
    'CashOnDelivery' => Icons.payments_outlined,
    'MonthlyInvoice' => Icons.calendar_month_outlined,
    'Card' => Icons.credit_card_outlined,
    'Partial' => Icons.call_split_outlined,
    _ => Icons.receipt_long_outlined,
  };
  String _paymentName(String code) =>
      _options!.payments.firstWhere((payment) => payment.code == code).name;
  String _money(double value) => NumberFormat('#,##0.00', 'ar').format(value);
  (String, String, String) _choice(String code, String name, String note) =>
      (code, name, note);
}

class OrderSuccessScreen extends StatelessWidget {
  const OrderSuccessScreen({super.key, required this.order});
  final OrderCreated order;
  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(28),
          child: Column(
            children: [
              Container(
                width: 100,
                height: 100,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: order.requiresApproval
                      ? AppColors.warningTint
                      : AppColors.successTint,
                ),
                child: Icon(
                  order.requiresApproval
                      ? Icons.info_outline_rounded
                      : Icons.check_rounded,
                  size: 55,
                  color: order.requiresApproval
                      ? AppColors.warning
                      : AppColors.success,
                ),
              ),
              const SizedBox(height: 22),
              Text(
                order.requiresApproval
                    ? 'أُرسل الطلب للموافقة'
                    : 'تم إنشاء طلبك بنجاح!',
                style: const TextStyle(
                  fontSize: 23,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'رقم الطلب ${order.number}\nسنرسل لك تحديثات الحالة أولًا بأول',
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.gray500, height: 1.7),
              ),
              const SizedBox(height: 18),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    children: [
                      _row(
                        'الإجمالي',
                        '${NumberFormat('#,##0.00', 'ar').format(order.total)} ج.م',
                      ),
                      _row(
                        'التوصيل المتوقع',
                        DateFormat(
                          'd MMMM yyyy',
                          'ar',
                        ).format(order.requiredDate),
                      ),
                      _row(
                        'الحالة',
                        order.requiresApproval
                            ? 'بانتظار الموافقة'
                            : 'تم التأكيد',
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 18),
              SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: () => context.go('/orders'),
                  child: const Text('متابعة الطلب'),
                ),
              ),
              SizedBox(
                width: double.infinity,
                child: TextButton(
                  onPressed: () => context.go('/home'),
                  child: const Text('متابعة التسوق'),
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );
  Widget _row(String label, String value) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 6),
    child: Row(
      children: [
        Expanded(
          child: Text(label, style: const TextStyle(color: AppColors.gray500)),
        ),
        Text(value, style: const TextStyle(fontWeight: FontWeight.w800)),
      ],
    ),
  );
}
