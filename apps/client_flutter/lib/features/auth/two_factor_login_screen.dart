import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/api/auth_repository.dart';
import 'auth_widgets.dart';

class TwoFactorLoginScreen extends ConsumerStatefulWidget {
  const TwoFactorLoginScreen({super.key, required this.challenge});
  final AuthResult challenge;
  @override
  ConsumerState<TwoFactorLoginScreen> createState() => _State();
}

class _State extends ConsumerState<TwoFactorLoginScreen> {
  final code = TextEditingController();
  bool loading = false;
  String? error;
  @override
  void initState() {
    super.initState();
    code.text = widget.challenge.developmentCode ?? '';
  }

  @override
  void dispose() {
    code.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AuthShell(
    showBack: true,
    title: 'المصادقة الثنائية',
    subtitle: 'أرسلنا رمز حماية إلى رقم الهاتف المسجل',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (error != null) InlineError(error!),
        TextField(
          controller: code,
          autofocus: true,
          textAlign: TextAlign.center,
          keyboardType: TextInputType.number,
          maxLength: 6,
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          decoration: const InputDecoration(
            labelText: 'رمز التحقق',
            prefixIcon: Icon(Icons.shield_outlined),
          ),
        ),
        const SizedBox(height: 14),
        FilledButton(
          onPressed: loading
              ? null
              : () async {
                  if (code.text.length != 6) {
                    setState(() => error = 'أدخل الرمز المكوّن من 6 أرقام');
                    return;
                  }
                  setState(() {
                    loading = true;
                    error = null;
                  });
                  try {
                    final result = await ref
                        .read(authRepositoryProvider)
                        .verifyTwoFactor(
                          widget.challenge.challengeToken!,
                          code.text,
                        );
                    ref.read(currentUserProvider.notifier).setUser(result.user);
                    if (context.mounted) {
                      context.go(
                        result.user?.tenantStatus == 'Active'
                            ? '/home'
                            : '/verification',
                      );
                    }
                  } catch (e) {
                    if (mounted) setState(() => error = '$e');
                  } finally {
                    if (mounted) setState(() => loading = false);
                  }
                },
          child: Text(loading ? 'جارٍ التحقق...' : 'تأكيد الدخول'),
        ),
      ],
    ),
  );
}
