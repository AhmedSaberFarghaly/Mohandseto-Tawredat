import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/api/auth_repository.dart';
import '../../core/theme/app_tokens.dart';

class LinkedAccountsScreen extends ConsumerStatefulWidget {
  const LinkedAccountsScreen({super.key});

  @override
  ConsumerState<LinkedAccountsScreen> createState() =>
      _LinkedAccountsScreenState();
}

class _LinkedAccountsScreenState extends ConsumerState<LinkedAccountsScreen> {
  List<ExternalProviderModel> providers = const [];
  List<LinkedExternalIdentityModel> linked = const [];
  bool loading = true;
  String? error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final repository = ref.read(authRepositoryProvider);
      final results = await Future.wait([
        repository.externalProviders(),
        repository.linkedExternal(),
      ]);
      if (!mounted) return;
      setState(() {
        providers = results[0] as List<ExternalProviderModel>;
        linked = results[1] as List<LinkedExternalIdentityModel>;
      });
    } catch (exception) {
      if (mounted) setState(() => error = exception.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _link(ExternalProviderModel provider) async {
    if (!provider.enabled) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('${provider.name} ينتظر Client ID من إعدادات الإنتاج'),
        ),
      );
      return;
    }
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final result = await ref
          .read(authRepositoryProvider)
          .linkExternalProvider(provider.code);
      if (result != null) await _load();
    } catch (exception) {
      if (mounted) setState(() => error = exception.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('الحسابات المرتبطة')),
    body: RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              gradient: const LinearGradient(
                colors: [Color(0xFFEFF8FF), Color(0xFFEFF3FC)],
              ),
              borderRadius: BorderRadius.circular(AppRadius.xl),
              border: Border.all(
                color: AppColors.primary.withValues(alpha: .1),
              ),
            ),
            child: const Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.verified_user_outlined, color: AppColors.primary),
                SizedBox(width: 12),
                Expanded(
                  child: Text(
                    'اربط حساب Google أو Microsoft وأكمل الدخول لاحقًا من شاشة اختيار الحساب. لا نخزن رمز المزود أو كلمة مروره.',
                    style: TextStyle(height: 1.6),
                  ),
                ),
              ],
            ),
          ),
          if (error != null) ...[
            const SizedBox(height: 12),
            Text(error!, style: const TextStyle(color: AppColors.error)),
          ],
          const SizedBox(height: 16),
          ...providers.map((provider) {
            final identity = linked
                .where((item) => item.provider == provider.code)
                .firstOrNull;
            return Container(
              margin: const EdgeInsets.only(bottom: 12),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(AppRadius.xl),
                border: Border.all(
                  color: identity != null
                      ? AppColors.success.withValues(alpha: .25)
                      : AppColors.gray150,
                ),
                boxShadow: AppShadows.soft,
              ),
              child: ListTile(
                leading: CircleAvatar(
                  backgroundColor: provider.code == 'google'
                      ? Colors.white
                      : const Color(0xFFF1F5FF),
                  child: Icon(
                    provider.code == 'google'
                        ? Icons.g_mobiledata_rounded
                        : Icons.window_rounded,
                    color: provider.code == 'google'
                        ? AppColors.error
                        : AppColors.primary,
                    size: provider.code == 'google' ? 30 : 22,
                  ),
                ),
                title: Text(
                  provider.name,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                subtitle: Text(
                  identity != null
                      ? '${identity.email ?? 'حساب موثق'} • مرتبط ${DateFormat('d MMM yyyy', 'ar').format(identity.linkedAt)}'
                      : provider.enabled
                      ? 'غير مرتبط - اضغط لاختيار الحساب'
                      : 'غير مهيأ في بيئة التشغيل',
                ),
                trailing: identity != null
                    ? Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 5,
                        ),
                        decoration: BoxDecoration(
                          color: AppColors.successTint,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: const Text(
                          'مرتبط',
                          style: TextStyle(
                            color: AppColors.success,
                            fontSize: 9.5,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      )
                    : const Icon(Icons.add_link_rounded),
                onTap: identity == null && !loading
                    ? () => _link(provider)
                    : null,
              ),
            );
          }),
          if (loading)
            const Padding(
              padding: EdgeInsets.all(24),
              child: Center(child: CircularProgressIndicator()),
            ),
        ],
      ),
    ),
  );
}
