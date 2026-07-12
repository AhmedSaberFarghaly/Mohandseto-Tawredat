import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/auth_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'auth_widgets.dart';

class DocumentsScreen extends ConsumerStatefulWidget {
  const DocumentsScreen({super.key});

  @override
  ConsumerState<DocumentsScreen> createState() => _DocumentsScreenState();
}

class _DocumentsScreenState extends ConsumerState<DocumentsScreen> {
  final Map<String, String> _uploaded = {};
  String? _loadingType;
  String? _error;

  static const _documents = [
    ('CommercialRegistration', 'السجل التجاري', Icons.store_outlined),
    ('TaxCard', 'البطاقة الضريبية', Icons.receipt_long_outlined),
    ('AuthorizationLetter', 'خطاب تفويض', Icons.assignment_ind_outlined),
  ];

  Future<void> _pick(String type) async {
    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: const ['pdf', 'jpg', 'jpeg', 'png'],
      withData: true,
    );
    if (result == null) return;
    final file = result.files.single;
    if (file.bytes == null) {
      setState(() => _error = 'تعذر قراءة الملف، حاول اختيار ملف آخر');
      return;
    }
    setState(() {
      _loadingType = type;
      _error = null;
    });
    try {
      final extension = file.extension?.toLowerCase();
      final contentType = extension == 'pdf'
          ? 'application/pdf'
          : extension == 'png'
          ? 'image/png'
          : 'image/jpeg';
      await ref
          .read(authRepositoryProvider)
          .uploadDocument(type, file.name, file.bytes!, contentType);
      setState(() => _uploaded[type] = file.name);
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loadingType = null);
    }
  }

  @override
  Widget build(BuildContext context) => AuthShell(
    showBack: true,
    title: 'رفع مستندات الشركة',
    subtitle:
        'ارفع المستندات المطلوبة بصيغة PDF أو JPG أو PNG وبحد أقصى 10 ميجابايت',
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const StepHeader(current: 3, total: 3),
        const SizedBox(height: 22),
        if (_error != null) InlineError(_error!),
        ..._documents.map((document) {
          final uploaded = _uploaded[document.$1];
          final loading = _loadingType == document.$1;
          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Container(
                    width: 46,
                    height: 46,
                    decoration: BoxDecoration(
                      color: uploaded == null
                          ? AppColors.primaryTint
                          : AppColors.successTint,
                      borderRadius: BorderRadius.circular(AppRadius.md),
                    ),
                    child: Icon(
                      uploaded == null
                          ? document.$3
                          : Icons.check_circle_rounded,
                      color: uploaded == null
                          ? AppColors.primary
                          : AppColors.success,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          document.$2,
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                        Text(
                          uploaded ?? 'لم يتم اختيار ملف',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            color: AppColors.gray500,
                            fontSize: 12,
                          ),
                        ),
                      ],
                    ),
                  ),
                  TextButton(
                    onPressed: loading ? null : () => _pick(document.$1),
                    child: loading
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Text(uploaded == null ? 'رفع' : 'تغيير'),
                  ),
                ],
              ),
            ),
          );
        }),
        const SizedBox(height: 16),
        FilledButton(
          onPressed:
              _uploaded.length == _documents.length && _loadingType == null
              ? () => context.go('/verification')
              : null,
          child: const Text('إرسال للمراجعة'),
        ),
        const SizedBox(height: 12),
        const Text(
          'تُراجع مستندات شركتك بأمان، ولن يتم استخدامها إلا للتحقق من بيانات الحساب.',
          textAlign: TextAlign.center,
          style: TextStyle(color: AppColors.gray500, height: 1.5),
        ),
      ],
    ),
  );
}
