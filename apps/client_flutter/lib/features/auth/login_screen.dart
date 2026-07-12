import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/auth_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'auth_widgets.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _emailMode = false;
  bool _loading = false;
  bool _obscure = true;
  String? _error;

  @override
  void dispose() {
    _phone.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final repository = ref.read(authRepositoryProvider);
      if (_emailMode) {
        final result = await repository.loginWithEmail(
          _email.text.trim(),
          _password.text,
        );
        ref.read(currentUserProvider.notifier).setUser(result.user);
        if (!mounted) return;
        context.go(_destination(result.user?.tenantStatus));
      } else {
        final phone = _phone.text.trim();
        final devCode = await repository.requestOtp(phone, 'Login');
        if (!mounted) return;
        context.push(
          Uri(
            path: '/otp',
            queryParameters: {
              'phone': phone,
              if (devCode != null) 'devCode': devCode,
            },
          ).toString(),
        );
      }
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  String _destination(String? status) =>
      status == null || status == 'Active' ? '/home' : '/verification';

  @override
  Widget build(BuildContext context) => AuthShell(
    title: _emailMode ? 'تسجيل الدخول بالبريد' : 'مرحبًا بعودتك',
    subtitle: _emailMode
        ? 'أدخل بريد العمل وكلمة المرور'
        : 'أدخل رقم هاتفك لإرسال رمز التحقق',
    child: Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (_error != null) InlineError(_error!),
          if (_emailMode) ...[
            TextFormField(
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              textDirection: TextDirection.ltr,
              decoration: const InputDecoration(
                labelText: 'البريد الإلكتروني',
                prefixIcon: Icon(Icons.email_outlined),
              ),
              validator: (value) => value != null && value.contains('@')
                  ? null
                  : 'أدخل بريدًا إلكترونيًا صحيحًا',
            ),
            const SizedBox(height: 14),
            TextFormField(
              controller: _password,
              obscureText: _obscure,
              textDirection: TextDirection.ltr,
              decoration: InputDecoration(
                labelText: 'كلمة المرور',
                prefixIcon: const Icon(Icons.lock_outline_rounded),
                suffixIcon: IconButton(
                  onPressed: () => setState(() => _obscure = !_obscure),
                  icon: Icon(
                    _obscure
                        ? Icons.visibility_outlined
                        : Icons.visibility_off_outlined,
                  ),
                ),
              ),
              validator: (value) => (value?.length ?? 0) >= 8
                  ? null
                  : 'كلمة المرور لا تقل عن 8 أحرف',
            ),
          ] else
            TextFormField(
              controller: _phone,
              autofocus: true,
              keyboardType: TextInputType.phone,
              textDirection: TextDirection.ltr,
              decoration: const InputDecoration(
                labelText: 'رقم الهاتف',
                hintText: '01xxxxxxxxx',
                prefixIcon: Icon(Icons.phone_android_rounded),
                suffixText: '+20',
              ),
              validator: (value) {
                final digits = value?.replaceAll(RegExp(r'\D'), '') ?? '';
                return digits.length >= 10 ? null : 'أدخل رقم هاتف صحيحًا';
              },
            ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _loading ? null : _submit,
            child: _loading
                ? const SizedBox(
                    width: 22,
                    height: 22,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  )
                : Text(_emailMode ? 'تسجيل الدخول' : 'إرسال رمز التحقق'),
          ),
          const SizedBox(height: 14),
          OutlinedButton.icon(
            onPressed: () => setState(() {
              _emailMode = !_emailMode;
              _error = null;
            }),
            icon: Icon(
              _emailMode
                  ? Icons.phone_android_rounded
                  : Icons.alternate_email_rounded,
            ),
            label: Text(
              _emailMode ? 'الدخول برقم الهاتف' : 'الدخول بالبريد الإلكتروني',
            ),
          ),
          const SizedBox(height: 28),
          const Row(
            children: [
              Expanded(child: Divider(color: AppColors.gray200)),
              Padding(
                padding: EdgeInsets.symmetric(horizontal: 12),
                child: Text('أو', style: TextStyle(color: AppColors.gray400)),
              ),
              Expanded(child: Divider(color: AppColors.gray200)),
            ],
          ),
          const SizedBox(height: 18),
          OutlinedButton.icon(
            onPressed: () {},
            icon: const Icon(Icons.g_mobiledata_rounded, size: 28),
            label: const Text('المتابعة باستخدام Google'),
          ),
          const SizedBox(height: 10),
          OutlinedButton.icon(
            onPressed: () {},
            icon: const Icon(Icons.window_rounded, size: 20),
            label: const Text('المتابعة باستخدام Microsoft'),
          ),
        ],
      ),
    ),
  );
}
