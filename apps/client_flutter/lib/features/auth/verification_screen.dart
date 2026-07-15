import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/auth_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'auth_widgets.dart';

class VerificationScreen extends ConsumerWidget {
  const VerificationScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    body: SafeArea(
      child: Center(
        child: FutureBuilder<Map<String, dynamic>>(
          future: ref.read(authRepositoryProvider).verificationStatus(),
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const CircularProgressIndicator();
            }
            if (snapshot.hasError) {
              return Padding(
                padding: const EdgeInsets.all(24),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 460),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      InlineError(snapshot.error.toString()),
                      OutlinedButton(
                        onPressed: () => context.go('/documents'),
                        child: const Text('العودة للمستندات'),
                      ),
                    ],
                  ),
                ),
              );
            }
            final data = snapshot.data!;
            final status = data['tenantStatus'] as String? ?? 'UnderReview';
            if (status == 'Active') {
              WidgetsBinding.instance.addPostFrameCallback((_) {
                if (context.mounted) context.go('/home');
              });
            }
            final rejected = status == 'Rejected';
            final suspended = status == 'Suspended';
            return SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 460),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const BrandMark(compact: true),
                    const SizedBox(height: 42),
                    Container(
                      width: 128,
                      height: 128,
                      decoration: BoxDecoration(
                        color: rejected || suspended
                            ? AppColors.errorTint
                            : AppColors.primaryTint,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        suspended
                            ? Icons.block_rounded
                            : rejected
                            ? Icons.assignment_late_outlined
                            : Icons.schedule_rounded,
                        size: 64,
                        color: rejected || suspended
                            ? AppColors.error
                            : AppColors.primary,
                      ),
                    ),
                    const SizedBox(height: 28),
                    Text(
                      suspended
                          ? 'حساب الشركة موقوف مؤقتًا'
                          : rejected
                          ? 'تحتاج بعض المستندات إلى تعديل'
                          : 'جاري مراجعة حسابك',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.headlineSmall
                          ?.copyWith(fontWeight: FontWeight.w800),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      suspended
                          ? 'لا يمكن تنفيذ عمليات جديدة حاليًا. تواصل مع الدعم لمعرفة السبب وخطوات إعادة التفعيل.'
                          : rejected
                          ? 'راجع الملاحظات وأعد رفع المستندات المطلوبة'
                          : 'فريقنا يراجع بيانات شركتك، وسنرسل إليك إشعارًا فور تفعيل الحساب.',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: AppColors.gray500,
                        height: 1.7,
                      ),
                    ),
                    const SizedBox(height: 28),
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: AppColors.gray50,
                        borderRadius: BorderRadius.circular(AppRadius.lg),
                      ),
                      child: const Row(
                        children: [
                          Icon(
                            Icons.notifications_active_outlined,
                            color: AppColors.primary,
                          ),
                          SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              'تستغرق المراجعة عادة من يوم إلى يومي عمل',
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 24),
                    if (suspended)
                      FilledButton.icon(
                        onPressed: () => context.push('/support'),
                        icon: const Icon(Icons.support_agent_rounded),
                        label: const Text('تواصل مع الدعم'),
                      )
                    else if (rejected)
                      FilledButton(
                        onPressed: () => context.go('/documents'),
                        child: const Text('تعديل المستندات'),
                      )
                    else
                      OutlinedButton.icon(
                        onPressed: () => context.go('/verification'),
                        icon: const Icon(Icons.refresh_rounded),
                        label: const Text('تحديث حالة الطلب'),
                      ),
                    if (!suspended)
                      TextButton(
                        onPressed: () => context.push('/support'),
                        child: const Text('تواصل مع الدعم'),
                      ),
                  ],
                ),
              ),
            );
          },
        ),
      ),
    ),
  );
}
