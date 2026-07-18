import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/auth_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'auth_widgets.dart';

class VerificationScreen extends ConsumerWidget {
  const VerificationScreen({super.key});

  @override
  Widget build(
    BuildContext context,
    WidgetRef ref,
  ) => FutureBuilder<Map<String, dynamic>>(
    future: ref.read(authRepositoryProvider).verificationStatus(),
    builder: (context, snapshot) {
      if (snapshot.connectionState == ConnectionState.waiting) {
        return const AuthShell(
          title: 'جاري التحقق من حسابك',
          subtitle: 'لحظات ونراجع أحدث حالة لملف شركتك',
          child: Padding(
            padding: EdgeInsets.all(24),
            child: Center(child: CircularProgressIndicator()),
          ),
        );
      }
      if (snapshot.hasError) {
        return AuthShell(
          showBack: true,
          title: 'تعذر تحديث الحالة',
          subtitle: 'تحقق من الاتصال ثم حاول مرة أخرى',
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              InlineError(snapshot.error.toString()),
              OutlinedButton.icon(
                onPressed: () => context.go('/documents'),
                icon: const Icon(Icons.description_outlined),
                label: const Text('العودة للمستندات'),
              ),
            ],
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
      final danger = rejected || suspended;
      final title = suspended
          ? 'حساب الشركة موقوف مؤقتًا'
          : rejected
          ? 'مطلوب تعديل بعض المستندات'
          : 'طلبك قيد المراجعة';
      final subtitle = suspended
          ? 'فريق الدعم جاهز لمساعدتك في خطوات إعادة التفعيل'
          : rejected
          ? 'راجع الملاحظات وأعد رفع الملفات المطلوبة'
          : 'سنرسل إشعارًا فور اكتمال توثيق حساب الشركة';
      return AuthShell(
        title: title,
        subtitle: subtitle,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: 112,
                height: 112,
                decoration: BoxDecoration(
                  color: danger ? AppColors.errorTint : AppColors.primaryTint,
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  suspended
                      ? Icons.block_rounded
                      : rejected
                      ? Icons.assignment_late_outlined
                      : Icons.fact_check_outlined,
                  size: 54,
                  color: danger ? AppColors.error : AppColors.primary,
                ),
              ),
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: danger ? AppColors.errorTint : AppColors.infoTint,
                borderRadius: BorderRadius.circular(16),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    danger
                        ? Icons.info_outline_rounded
                        : Icons.schedule_rounded,
                    color: danger ? AppColors.error : AppColors.primary,
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      suspended
                          ? 'لا يمكن تنفيذ عمليات جديدة حاليًا حتى مراجعة الحساب.'
                          : rejected
                          ? 'يمكنك استبدال المستندات المرفوضة وإرسالها للمراجعة مرة أخرى.'
                          : 'تستغرق المراجعة عادة من يوم إلى يومي عمل.',
                      style: TextStyle(
                        color: danger ? AppColors.error : AppColors.info,
                        fontSize: 10.5,
                        height: 1.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            if (suspended)
              FilledButton.icon(
                onPressed: () => context.push('/support'),
                icon: const Icon(Icons.support_agent_rounded),
                label: const Text('تواصل مع الدعم'),
              )
            else if (rejected)
              FilledButton.icon(
                onPressed: () => context.go('/documents'),
                icon: const Icon(Icons.upload_file_rounded),
                label: const Text('تعديل المستندات'),
              )
            else
              FilledButton.icon(
                onPressed: () => context.go('/verification'),
                icon: const Icon(Icons.refresh_rounded),
                label: const Text('تحديث حالة الطلب'),
              ),
            if (!suspended)
              TextButton.icon(
                onPressed: () => context.push('/support'),
                icon: const Icon(Icons.help_outline_rounded),
                label: const Text('تحتاج مساعدة؟'),
              ),
          ],
        ),
      );
    },
  );
}
