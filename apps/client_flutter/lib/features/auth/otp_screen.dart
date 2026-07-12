import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/auth_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'auth_widgets.dart';

class OtpScreen extends ConsumerStatefulWidget {
  const OtpScreen({super.key, required this.phone, this.devCode});
  final String phone;
  final String? devCode;

  @override
  ConsumerState<OtpScreen> createState() => _OtpScreenState();
}

class _OtpScreenState extends ConsumerState<OtpScreen> {
  late final List<TextEditingController> _digits;
  late final List<FocusNode> _focus;
  Timer? _timer;
  int _seconds = 59;
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _digits = List.generate(6, (_) => TextEditingController());
    _focus = List.generate(6, (_) => FocusNode());
    if (widget.devCode?.length == 6) {
      for (var i = 0; i < 6; i++) {
        _digits[i].text = widget.devCode![i];
      }
    }
    _startTimer();
  }

  void _startTimer() {
    _seconds = 59;
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted) return;
      if (_seconds == 0) {
        timer.cancel();
      } else {
        setState(() => _seconds--);
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    for (final controller in _digits) {
      controller.dispose();
    }
    for (final node in _focus) {
      node.dispose();
    }
    super.dispose();
  }

  String get _code => _digits.map((item) => item.text).join();

  Future<void> _verify() async {
    if (_code.length != 6) {
      setState(() => _error = 'أدخل رمز التحقق المكوّن من 6 أرقام');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final repository = ref.read(authRepositoryProvider);
      final result = await repository.verifyOtp(widget.phone, _code);
      if (!mounted) return;
      if (result.isNewUser) {
        final registrationCode = await repository.requestOtp(
          widget.phone,
          'Registration',
        );
        if (!mounted) return;
        context.go(
          Uri(
            path: '/register',
            queryParameters: {
              'phone': widget.phone,
              if (registrationCode != null) 'code': registrationCode,
            },
          ).toString(),
        );
      } else {
        ref.read(currentUserProvider.notifier).setUser(result.user);
        context.go(
          result.user?.tenantStatus == 'Approved' ? '/home' : '/verification',
        );
      }
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _resend() async {
    setState(() => _error = null);
    try {
      final code = await ref
          .read(authRepositoryProvider)
          .requestOtp(widget.phone, 'Login');
      if (code?.length == 6) {
        for (var i = 0; i < 6; i++) {
          _digits[i].text = code![i];
        }
      }
      _startTimer();
      if (mounted) setState(() {});
    } catch (error) {
      setState(() => _error = error.toString());
    }
  }

  @override
  Widget build(BuildContext context) => AuthShell(
    showBack: true,
    title: 'أدخل رمز التحقق',
    subtitle: 'أرسلنا رمزًا مكوّنًا من 6 أرقام إلى\n${widget.phone}',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (_error != null) InlineError(_error!),
        Directionality(
          textDirection: TextDirection.ltr,
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: List.generate(6, (index) {
              return SizedBox(
                width: 48,
                child: TextField(
                  controller: _digits[index],
                  focusNode: _focus[index],
                  autofocus: index == 0 && widget.devCode == null,
                  keyboardType: TextInputType.number,
                  textAlign: TextAlign.center,
                  maxLength: 1,
                  inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                  decoration: const InputDecoration(
                    counterText: '',
                    contentPadding: EdgeInsets.zero,
                  ),
                  style: const TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.w700,
                  ),
                  onChanged: (value) {
                    if (value.isNotEmpty && index < 5) {
                      _focus[index + 1].requestFocus();
                    } else if (value.isEmpty && index > 0) {
                      _focus[index - 1].requestFocus();
                    }
                  },
                ),
              );
            }),
          ),
        ),
        const SizedBox(height: 24),
        FilledButton(
          onPressed: _loading ? null : _verify,
          child: _loading
              ? const SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.white,
                  ),
                )
              : const Text('تحقق'),
        ),
        const SizedBox(height: 14),
        TextButton(
          onPressed: _seconds == 0 ? _resend : null,
          child: Text(
            _seconds == 0
                ? 'إعادة إرسال الرمز'
                : 'إعادة الإرسال خلال 00:${_seconds.toString().padLeft(2, '0')}',
          ),
        ),
        const SizedBox(height: 18),
        const Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.shield_outlined, color: AppColors.success, size: 18),
            SizedBox(width: 6),
            Text(
              'بياناتك محمية ومشفرة',
              style: TextStyle(color: AppColors.gray500),
            ),
          ],
        ),
      ],
    ),
  );
}
