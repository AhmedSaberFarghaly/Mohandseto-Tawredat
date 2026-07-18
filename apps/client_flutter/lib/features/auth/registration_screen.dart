import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/auth_repository.dart';
import 'auth_widgets.dart';

class RegistrationScreen extends ConsumerStatefulWidget {
  const RegistrationScreen({
    super.key,
    required this.phone,
    this.registrationCode,
  });

  final String phone;
  final String? registrationCode;

  @override
  ConsumerState<RegistrationScreen> createState() => _RegistrationScreenState();
}

class _RegistrationScreenState extends ConsumerState<RegistrationScreen> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _company = TextEditingController();
  final _companyEn = TextEditingController();
  final _commercial = TextEditingController();
  final _tax = TextEditingController();
  final _city = TextEditingController();
  final _address = TextEditingController();
  late final TextEditingController _code;
  int _step = 1;
  bool _loading = false;
  bool _obscurePassword = true;
  String? _error;
  String _governorate = 'القاهرة';
  String _industry = 'تجارة وتوزيع';

  static const _governorates = [
    'القاهرة',
    'الجيزة',
    'الإسكندرية',
    'القليوبية',
    'الشرقية',
    'الدقهلية',
    'الغربية',
    'المنوفية',
    'أخرى',
  ];
  static const _industries = [
    'تجارة وتوزيع',
    'مقاولات',
    'تصنيع',
    'خدمات',
    'ضيافة وفنادق',
    'تعليم',
    'رعاية صحية',
    'أخرى',
  ];

  @override
  void initState() {
    super.initState();
    _code = TextEditingController(text: widget.registrationCode);
  }

  @override
  void dispose() {
    for (final controller in [
      _name,
      _email,
      _password,
      _company,
      _companyEn,
      _commercial,
      _tax,
      _city,
      _address,
      _code,
    ]) {
      controller.dispose();
    }
    super.dispose();
  }

  void _next() {
    if (_formKey.currentState!.validate()) {
      setState(() {
        _step = 2;
        _error = null;
      });
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await ref.read(authRepositoryProvider).registerCompany({
        'phone': widget.phone,
        'otpCode': _code.text.trim(),
        'companyLegalName': _company.text.trim(),
        'companyLegalNameEn': _companyEn.text.trim().isEmpty
            ? null
            : _companyEn.text.trim(),
        'commercialRegistrationNo': _commercial.text.trim(),
        'taxCardNo': _tax.text.trim(),
        'governorate': _governorate,
        'city': _city.text.trim(),
        'addressLine': _address.text.trim(),
        'industry': _industry,
        'adminFullName': _name.text.trim(),
        'adminEmail': _email.text.trim().isEmpty ? null : _email.text.trim(),
        'adminPassword': _password.text,
      });
      ref.read(currentUserProvider.notifier).setUser(result.user);
      if (mounted) context.go('/documents');
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => AuthShell(
    showBack: true,
    title: _step == 1 ? 'إنشاء حساب جديد' : 'بيانات الشركة',
    subtitle: _step == 1
        ? 'أنشئ حساب مسؤول الشركة للمتابعة'
        : 'أدخل البيانات القانونية كما تظهر في المستندات الرسمية',
    child: Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          StepHeader(current: _step, total: 3),
          const SizedBox(height: 24),
          if (_error != null) InlineError(_error!),
          if (_step == 1) ...[
            _field(
              _name,
              'الاسم بالكامل',
              Icons.person_outline_rounded,
              validator: _required,
            ),
            _gap,
            TextFormField(
              initialValue: widget.phone,
              enabled: false,
              textDirection: TextDirection.ltr,
              decoration: const InputDecoration(
                labelText: 'رقم الهاتف',
                prefixIcon: Icon(Icons.phone_android_rounded),
              ),
            ),
            _gap,
            _field(
              _email,
              'البريد الإلكتروني للعمل',
              Icons.email_outlined,
              keyboardType: TextInputType.emailAddress,
              validator: (value) => value!.isEmpty || value.contains('@')
                  ? null
                  : 'أدخل بريدًا صحيحًا',
            ),
            _gap,
            _field(
              _password,
              'كلمة المرور',
              Icons.lock_outline_rounded,
              obscure: _obscurePassword,
              suffixIcon: IconButton(
                onPressed: () =>
                    setState(() => _obscurePassword = !_obscurePassword),
                icon: Icon(
                  _obscurePassword
                      ? Icons.visibility_outlined
                      : Icons.visibility_off_outlined,
                ),
              ),
              validator: (value) =>
                  value!.length >= 8 ? null : 'كلمة المرور لا تقل عن 8 أحرف',
            ),
            _gap,
            _field(
              _code,
              'رمز تأكيد التسجيل',
              Icons.password_rounded,
              keyboardType: TextInputType.number,
              validator: (value) =>
                  value!.length == 6 ? null : 'أدخل الرمز المرسل إلى هاتفك',
            ),
            const SizedBox(height: 22),
            FilledButton.icon(
              onPressed: _next,
              icon: const Icon(Icons.arrow_back_rounded),
              label: const Text('متابعة لبيانات الشركة'),
            ),
          ] else ...[
            _field(
              _company,
              'الاسم القانوني للشركة',
              Icons.business_outlined,
              validator: _required,
            ),
            _gap,
            _field(
              _companyEn,
              'اسم الشركة بالإنجليزية (اختياري)',
              Icons.translate_rounded,
            ),
            _gap,
            LayoutBuilder(
              builder: (context, constraints) {
                final commercial = _field(
                  _commercial,
                  'السجل التجاري',
                  Icons.description_outlined,
                );
                final tax = _field(
                  _tax,
                  'البطاقة الضريبية',
                  Icons.receipt_long_outlined,
                );
                if (constraints.maxWidth < 380) {
                  return Column(children: [commercial, _gap, tax]);
                }
                return Row(
                  children: [
                    Expanded(child: commercial),
                    const SizedBox(width: 10),
                    Expanded(child: tax),
                  ],
                );
              },
            ),
            _gap,
            DropdownButtonFormField<String>(
              value: _governorate,
              decoration: const InputDecoration(
                labelText: 'المحافظة',
                prefixIcon: Icon(Icons.location_on_outlined),
              ),
              items: _governorates
                  .map(
                    (item) => DropdownMenuItem(value: item, child: Text(item)),
                  )
                  .toList(),
              onChanged: (value) => _governorate = value!,
            ),
            _gap,
            _field(
              _city,
              'المدينة / المنطقة',
              Icons.map_outlined,
              validator: _required,
            ),
            _gap,
            _field(
              _address,
              'العنوان بالتفصيل',
              Icons.home_work_outlined,
              validator: _required,
            ),
            _gap,
            DropdownButtonFormField<String>(
              value: _industry,
              decoration: const InputDecoration(
                labelText: 'مجال النشاط',
                prefixIcon: Icon(Icons.category_outlined),
              ),
              items: _industries
                  .map(
                    (item) => DropdownMenuItem(value: item, child: Text(item)),
                  )
                  .toList(),
              onChanged: (value) => _industry = value!,
            ),
            const SizedBox(height: 22),
            FilledButton.icon(
              onPressed: _loading ? null : _submit,
              icon: _loading
                  ? const SizedBox(
                      width: 22,
                      height: 22,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.verified_outlined),
              label: Text(
                _loading ? 'جارٍ إنشاء الحساب...' : 'إنشاء الحساب والمتابعة',
              ),
            ),
            TextButton(
              onPressed: () => setState(() => _step = 1),
              child: const Text('السابق'),
            ),
          ],
        ],
      ),
    ),
  );

  Widget _field(
    TextEditingController controller,
    String label,
    IconData icon, {
    bool obscure = false,
    TextInputType? keyboardType,
    String? Function(String?)? validator,
    Widget? suffixIcon,
  }) => TextFormField(
    controller: controller,
    obscureText: obscure,
    keyboardType: keyboardType,
    decoration: InputDecoration(
      labelText: label,
      prefixIcon: Icon(icon),
      suffixIcon: suffixIcon,
    ),
    validator: validator,
  );

  String? _required(String? value) =>
      value == null || value.trim().isEmpty ? 'هذا الحقل مطلوب' : null;

  Widget get _gap => const SizedBox(height: 14);
}
