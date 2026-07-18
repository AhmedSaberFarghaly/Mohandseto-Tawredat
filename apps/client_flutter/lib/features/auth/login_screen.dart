import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/auth_repository.dart';
import '../../core/theme/app_theme.dart';
import '../../core/theme/app_tokens.dart';
import 'auth_widgets.dart';

/// Brand accent used alongside the primary navy in the wordmark and artwork.
const _accent = Color(0xFF17A2C4);
const _accentTint = Color(0xFFE7F6FA);
const _boxAmber = Color(0xFFB5771F);
const _boxTint = Color(0xFFFDF3E1);

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _identifier = TextEditingController();
  final _password = TextEditingController();
  bool _remember = true;
  bool _loading = false;
  bool _obscure = true;
  String? _error;
  Map<String, ExternalProviderModel> _providers = const {};

  @override
  void initState() {
    super.initState();
    _loadProviders();
  }

  Future<void> _loadProviders() async {
    try {
      final items = await ref.read(authRepositoryProvider).externalProviders();
      if (mounted) {
        setState(
          () => _providers = {for (final item in items) item.code: item},
        );
      }
    } catch (_) {
      // Email and phone login remain available when provider discovery is unavailable.
    }
  }

  @override
  void dispose() {
    _identifier.dispose();
    _password.dispose();
    super.dispose();
  }

  bool get _isEmail => _identifier.text.trim().contains('@');

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final identifier = _identifier.text.trim();
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final repository = ref.read(authRepositoryProvider);
      if (identifier.contains('@')) {
        final result = await repository.loginWithEmail(
          identifier,
          _password.text,
        );
        if (result.requiresTwoFactor && result.challengeToken != null) {
          if (!mounted) return;
          context.push('/two-factor-login', extra: result);
          return;
        }
        ref.read(currentUserProvider.notifier).setUser(result.user);
        if (!mounted) return;
        context.go(_destination(result.user?.tenantStatus));
      } else {
        final devCode = await repository.requestOtp(identifier, 'Login');
        if (!mounted) return;
        context.push(
          Uri(
            path: '/otp',
            queryParameters: {
              'phone': identifier,
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

  void _forgotPassword() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'لإعادة تعيين كلمة المرور، سجّل الدخول برقم الموبايل ورمز التحقق أو تواصل مع الدعم.',
        ),
      ),
    );
  }

  void _createAccount() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'أدخل رقم موبايلك بالأعلى واضغط «دخول» لإتمام إنشاء حساب شركتك عبر رمز التحقق.',
        ),
      ),
    );
  }

  Future<void> _social(String provider) async {
    final configured = _providers[provider]?.enabled == true;
    if (!configured) {
      final name = switch (provider) {
        'google' => 'Google',
        'apple' => 'Apple',
        _ => 'Microsoft',
      };
      setState(
        () => _error =
            'تسجيل الدخول عبر $name ينتظر إعداد بيانات المزود. استخدم البريد أو الهاتف حاليًا.',
      );
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final result = await ref
          .read(authRepositoryProvider)
          .loginWithExternalProvider(provider);
      if (result == null) return;
      if (result.requiresTwoFactor && result.challengeToken != null) {
        if (mounted) context.push('/two-factor-login', extra: result);
        return;
      }
      if (result.isNewUser) {
        if (result.prefillEmail != null) {
          _identifier.text = result.prefillEmail!;
        }
        if (mounted) {
          setState(() {
            _error =
                'الحساب الخارجي موثّق لكنه غير مرتبط بحساب شركة. سجّل بالبريد مرة واحدة أو أنشئ حساب شركة ثم اربطه من إعدادات الأمان.';
          });
        }
        return;
      }
      ref.read(currentUserProvider.notifier).setUser(result.user);
      if (mounted) context.go(_destination(result.user?.tenantStatus));
    } catch (error) {
      if (mounted) setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    body: SingleChildScrollView(
      padding: EdgeInsets.zero,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 520),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const _HeroHeader(),
            Transform.translate(
              offset: const Offset(0, -26),
              child: _loginCard(context),
            ),
          ],
        ),
      ),
    ),
  );

  Widget _loginCard(BuildContext context) => Container(
    padding: const EdgeInsets.fromLTRB(22, 26, 22, 24),
    decoration: const BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.vertical(top: Radius.circular(30)),
      boxShadow: [
        BoxShadow(
          color: Color(0x12102846),
          blurRadius: 24,
          offset: Offset(0, -6),
        ),
      ],
    ),
    child: SafeArea(
      top: false,
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'تسجيل الدخول',
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                color: AppColors.primary,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 22),
            if (_error != null) InlineError(_error!),
            TextFormField(
              controller: _identifier,
              keyboardType: TextInputType.emailAddress,
              textDirection: TextDirection.ltr,
              autofillHints: const [AutofillHints.username],
              onChanged: (_) => setState(() {}),
              decoration: const InputDecoration(
                hintText: 'رقم الموبايل أو البريد الإلكتروني',
                prefixIcon: Icon(Icons.person_outline_rounded),
                suffixIcon: Icon(Icons.mail_outline_rounded, color: _accent),
              ),
              validator: (value) {
                final text = value?.trim() ?? '';
                if (text.contains('@')) return null;
                final digits = text.replaceAll(RegExp(r'\D'), '');
                return digits.length >= 10
                    ? null
                    : 'أدخل رقم موبايل أو بريد إلكتروني صحيح';
              },
            ),
            const SizedBox(height: 14),
            TextFormField(
              controller: _password,
              obscureText: _obscure,
              textDirection: TextDirection.ltr,
              autofillHints: const [AutofillHints.password],
              decoration: InputDecoration(
                hintText: 'كلمة المرور',
                prefixIcon: const Icon(Icons.lock_outline_rounded),
                suffixIcon: IconButton(
                  onPressed: () => setState(() => _obscure = !_obscure),
                  icon: Icon(
                    _obscure
                        ? Icons.visibility_off_outlined
                        : Icons.visibility_outlined,
                    color: AppColors.gray400,
                  ),
                ),
              ),
              validator: (value) {
                if (!_isEmail) return null;
                return (value?.length ?? 0) >= 8
                    ? null
                    : 'كلمة المرور لا تقل عن 8 أحرف';
              },
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                _RememberMe(
                  value: _remember,
                  onChanged: (v) => setState(() => _remember = v),
                ),
                const Spacer(),
                TextButton(
                  onPressed: _forgotPassword,
                  style: TextButton.styleFrom(
                    foregroundColor: _accent,
                    padding: const EdgeInsets.symmetric(horizontal: 4),
                    minimumSize: const Size(0, 36),
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                  child: const Text('نسيت كلمة المرور؟'),
                ),
              ],
            ),
            const SizedBox(height: 14),
            FilledButton(
              onPressed: _loading ? null : _submit,
              style: FilledButton.styleFrom(
                backgroundColor: AppColors.primary,
                minimumSize: const Size.fromHeight(54),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(16),
                ),
                textStyle: const TextStyle(
                  fontFamily: AppTheme.fontFamily,
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                ),
              ),
              child: _loading
                  ? const SizedBox(
                      width: 22,
                      height: 22,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Text('دخول'),
            ),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _loading ? null : _createAccount,
              icon: const Icon(Icons.person_add_alt_1_outlined, size: 20),
              label: const Text('إنشاء حساب جديد'),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                minimumSize: const Size.fromHeight(52),
                side: const BorderSide(color: _accent),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(16),
                ),
                textStyle: const TextStyle(
                  fontFamily: AppTheme.fontFamily,
                  fontSize: 14.5,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
            const SizedBox(height: 20),
            const Row(
              children: [
                Expanded(child: Divider(color: AppColors.gray200)),
                Padding(
                  padding: EdgeInsets.symmetric(horizontal: 14),
                  child: Text(
                    'أو',
                    style: TextStyle(
                      color: AppColors.gray400,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                Expanded(child: Divider(color: AppColors.gray200)),
              ],
            ),
            const SizedBox(height: 18),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _SocialTile(
                  onTap: _loading ? null : () => _social('microsoft'),
                  child: const _MicrosoftLogo(),
                ),
                const SizedBox(width: 16),
                _SocialTile(
                  onTap: _loading ? null : () => _social('google'),
                  child: const _GoogleLogo(),
                ),
                const SizedBox(width: 16),
                _SocialTile(
                  onTap: _loading ? null : () => _social('apple'),
                  child: const Icon(
                    Icons.apple,
                    size: 30,
                    color: AppColors.gray900,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 20),
            _TermsNote(onTap: _createAccount),
          ],
        ),
      ),
    ),
  );
}

/// Gradient hero with the brand wordmark, tagline and a supplies illustration.
class _HeroHeader extends StatelessWidget {
  const _HeroHeader();

  @override
  Widget build(BuildContext context) => Container(
    width: double.infinity,
    decoration: const BoxDecoration(
      gradient: LinearGradient(
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
        colors: [Color(0xFFEAF1FB), Color(0xFFDCEAF7), Color(0xFFEFF5FB)],
      ),
    ),
    child: SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(24, 22, 24, 34),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const _LogoMark(),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        'مهندسيتو',
                        style: Theme.of(context).textTheme.displaySmall
                            ?.copyWith(
                              fontSize: 32,
                              height: 1.05,
                              color: AppColors.primary,
                              fontWeight: FontWeight.w700,
                            ),
                      ),
                      Text(
                        'توريدات',
                        style: Theme.of(context).textTheme.displaySmall
                            ?.copyWith(
                              fontSize: 32,
                              height: 1.05,
                              color: _accent,
                              fontWeight: FontWeight.w700,
                            ),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'كل احتياجات شركتك في مكان واحد',
                        style: TextStyle(
                          color: AppColors.gray600,
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 26),
            const _HeroArtwork(),
          ],
        ),
      ),
    ),
  );
}

/// Hexagon-style brand glyph echoing the app logo.
class _LogoMark extends StatelessWidget {
  const _LogoMark();

  @override
  Widget build(BuildContext context) => Container(
    width: 76,
    height: 76,
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(22),
      boxShadow: AppShadows.floating,
    ),
    child: Stack(
      alignment: Alignment.center,
      children: [
        const Icon(Icons.hexagon_outlined, size: 52, color: AppColors.primary),
        const Icon(Icons.hexagon_outlined, size: 34, color: _accent),
        Container(
          width: 15,
          height: 15,
          decoration: BoxDecoration(
            color: AppColors.warning,
            borderRadius: BorderRadius.circular(4),
          ),
        ),
      ],
    ),
  );
}

/// Stylised logistics scene: cart, boxes, checklist, delivery and location.
class _HeroArtwork extends StatelessWidget {
  const _HeroArtwork();

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 96,
    child: Stack(
      alignment: Alignment.center,
      children: [
        Align(
          alignment: Alignment.bottomCenter,
          child: Container(
            height: 16,
            margin: const EdgeInsets.symmetric(horizontal: 8),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                colors: [Color(0x00B9CBE4), Color(0x55A9BFE0), Color(0x00B9CBE4)],
              ),
              borderRadius: BorderRadius.circular(999),
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 4),
          child: FittedBox(
            fit: BoxFit.scaleDown,
            child: Row(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: const [
                _ArtTile(
                  icon: Icons.location_on_rounded,
                  bg: _boxTint,
                  fg: AppColors.warning,
                  size: 54,
                ),
                SizedBox(width: 10),
                _ArtTile(
                  icon: Icons.local_shipping_rounded,
                  bg: _accentTint,
                  fg: _accent,
                  size: 66,
                ),
                SizedBox(width: 10),
                _ArtTile(
                  icon: Icons.inventory_2_rounded,
                  bg: _boxTint,
                  fg: _boxAmber,
                  size: 76,
                ),
                SizedBox(width: 10),
                _ArtTile(
                  icon: Icons.assignment_turned_in_rounded,
                  bg: AppColors.successTint,
                  fg: AppColors.success,
                  size: 66,
                ),
                SizedBox(width: 10),
                _ArtTile(
                  icon: Icons.shopping_cart_rounded,
                  bg: AppColors.primary,
                  fg: Colors.white,
                  size: 54,
                ),
              ],
            ),
          ),
        ),
      ],
    ),
  );
}

class _ArtTile extends StatelessWidget {
  const _ArtTile({
    required this.icon,
    required this.bg,
    required this.fg,
    required this.size,
  });
  final IconData icon;
  final Color bg;
  final Color fg;
  final double size;

  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(
      color: bg,
      borderRadius: BorderRadius.circular(size * 0.28),
      boxShadow: const [
        BoxShadow(
          color: Color(0x1A102846),
          blurRadius: 14,
          offset: Offset(0, 6),
        ),
      ],
    ),
    child: Icon(icon, size: size * 0.5, color: fg),
  );
}

class _RememberMe extends StatelessWidget {
  const _RememberMe({required this.value, required this.onChanged});
  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => onChanged(!value),
    borderRadius: BorderRadius.circular(8),
    child: Padding(
      padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 2),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          AnimatedContainer(
            duration: const Duration(milliseconds: 150),
            width: 22,
            height: 22,
            decoration: BoxDecoration(
              color: value ? AppColors.primary : Colors.white,
              borderRadius: BorderRadius.circular(6),
              border: Border.all(
                color: value ? AppColors.primary : AppColors.gray300,
                width: 1.5,
              ),
            ),
            child: value
                ? const Icon(Icons.check_rounded, size: 16, color: Colors.white)
                : null,
          ),
          const SizedBox(width: 8),
          const Text(
            'تذكرني',
            style: TextStyle(
              color: AppColors.gray700,
              fontSize: 13,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    ),
  );
}

class _SocialTile extends StatelessWidget {
  const _SocialTile({required this.child, this.onTap});
  final Widget child;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: onTap,
    borderRadius: BorderRadius.circular(16),
    child: Container(
      width: 66,
      height: 60,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.gray200),
        boxShadow: AppShadows.soft,
      ),
      child: child,
    ),
  );
}

class _MicrosoftLogo extends StatelessWidget {
  const _MicrosoftLogo();

  @override
  Widget build(BuildContext context) {
    Widget square(Color color) =>
        Container(width: 12, height: 12, color: color);
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            square(const Color(0xFFF25022)),
            const SizedBox(width: 2),
            square(const Color(0xFF7FBA00)),
          ],
        ),
        const SizedBox(height: 2),
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            square(const Color(0xFF00A4EF)),
            const SizedBox(width: 2),
            square(const Color(0xFFFFB900)),
          ],
        ),
      ],
    );
  }
}

class _GoogleLogo extends StatelessWidget {
  const _GoogleLogo();

  @override
  Widget build(BuildContext context) => const Text(
    'G',
    style: TextStyle(
      fontSize: 28,
      fontWeight: FontWeight.w800,
      color: Color(0xFF4285F4),
    ),
  );
}

class _TermsNote extends StatelessWidget {
  const _TermsNote({required this.onTap});
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Text.rich(
    TextSpan(
      style: const TextStyle(
        color: AppColors.gray500,
        fontSize: 12,
        height: 1.5,
      ),
      children: [
        const TextSpan(text: 'بتسجيل الدخول أنت توافق على '),
        TextSpan(
          text: 'الشروط والأحكام',
          style: const TextStyle(
            color: _accent,
            fontWeight: FontWeight.w700,
          ),
          recognizer: _TapRecognizer(onTap),
        ),
      ],
    ),
    textAlign: TextAlign.center,
  );
}

// Lightweight gesture recognizer wrapper for the inline terms link.
class _TapRecognizer extends TapGestureRecognizer {
  _TapRecognizer(VoidCallback handler) {
    onTap = handler;
  }
}
