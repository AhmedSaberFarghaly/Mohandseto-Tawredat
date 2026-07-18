import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../core/api/engagement_repository.dart';
import '../../core/api/api_client.dart';
import '../../core/theme/app_tokens.dart';
import '../../core/theme/appearance_controller.dart';

class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({super.key});
  @override
  ConsumerState<NotificationsScreen> createState() => _NotificationsState();
}

class _NotificationsState extends ConsumerState<NotificationsScreen> {
  bool unread = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(
      title: const Text('الإشعارات'),
      actions: [
        IconButton(
          onPressed: () => context.push('/notifications/preferences'),
          icon: const Icon(Icons.tune),
        ),
        IconButton(
          onPressed: () async {
            await ref.read(engagementRepositoryProvider).readNotification(null);
            ref.invalidate(notificationsProvider);
          },
          icon: const Icon(Icons.done_all),
          tooltip: 'تحديد الكل كمقروء',
        ),
      ],
    ),
    body: Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: SegmentedButton<bool>(
            segments: const [
              ButtonSegment(value: false, label: Text('الكل')),
              ButtonSegment(value: true, label: Text('غير المقروءة')),
            ],
            selected: {unread},
            onSelectionChanged: (v) => setState(() => unread = v.first),
          ),
        ),
        Expanded(
          child: ref
              .watch(notificationsProvider(unread))
              .when(
                loading: loading,
                error: errorView,
                data: (page) => page.items.isEmpty
                    ? const AppStateView(
                        icon: Icons.notifications_off_outlined,
                        title: 'لا توجد إشعارات',
                        message: 'ستظهر هنا تحديثات الطلبات والعروض والفواتير.',
                      )
                    : RefreshIndicator(
                        onRefresh: () async {
                          ref.invalidate(notificationsProvider);
                          await ref.read(notificationsProvider(unread).future);
                        },
                        child: ListView.separated(
                          padding: const EdgeInsets.fromLTRB(16, 4, 16, 90),
                          itemCount: page.items.length,
                          separatorBuilder: (_, _) => const SizedBox(height: 7),
                          itemBuilder: (_, i) => NotificationTile(
                            page.items[i],
                            () async {
                              final n = page.items[i];
                              if (!n.read) {
                                await ref
                                    .read(engagementRepositoryProvider)
                                    .readNotification(n.id);
                              }
                              ref.invalidate(notificationsProvider);
                              if (context.mounted) openNotification(context, n);
                            },
                          ),
                        ),
                      ),
              ),
        ),
      ],
    ),
  );
}

class NotificationTile extends StatelessWidget {
  const NotificationTile(this.item, this.tap, {super.key});
  final NotificationModel item;
  final VoidCallback tap;
  @override
  Widget build(BuildContext context) {
    final meta = notificationMeta(item.type);
    return Card(
      color: item.read ? null : AppColors.primaryTint,
      child: ListTile(
        onTap: tap,
        leading: CircleAvatar(
          backgroundColor: meta.$2.withValues(alpha: .12),
          child: Icon(meta.$1, color: meta.$2),
        ),
        title: Text(
          item.title,
          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 11),
        ),
        subtitle: Text(
          '${item.body}\n${relative(item.createdAt)}',
          maxLines: 3,
          style: const TextStyle(fontSize: 10.5),
        ),
        isThreeLine: true,
        trailing: item.read
            ? null
            : const CircleAvatar(radius: 4, backgroundColor: AppColors.primary),
      ),
    );
  }
}

class NotificationPreferencesScreen extends ConsumerWidget {
  const NotificationPreferencesScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('إعدادات الإشعارات')),
    body: ref
        .watch(notificationPreferencesProvider)
        .when(
          loading: loading,
          error: errorView,
          data: (p) => NotificationPreferencesForm(p),
        ),
  );
}

class NotificationPreferencesForm extends ConsumerStatefulWidget {
  const NotificationPreferencesForm(this.value, {super.key});
  final NotificationPreferencesModel value;
  @override
  ConsumerState<NotificationPreferencesForm> createState() => _PrefsState();
}

class _PrefsState extends ConsumerState<NotificationPreferencesForm> {
  late var push = widget.value.push,
      email = widget.value.email,
      sms = widget.value.sms,
      orders = widget.value.orders,
      approvals = widget.value.approvals,
      quotes = widget.value.quotes,
      invoices = widget.value.invoices,
      promotions = widget.value.promotions;
  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      const SectionLabel('قنوات الإشعار'),
      toggle(
        'إشعارات التطبيق',
        Icons.notifications_active_outlined,
        push,
        (v) => setState(() => push = v),
      ),
      toggle(
        'البريد الإلكتروني',
        Icons.email_outlined,
        email,
        (v) => setState(() => email = v),
      ),
      toggle(
        'الرسائل النصية',
        Icons.sms_outlined,
        sms,
        (v) => setState(() => sms = v),
      ),
      const SectionLabel('أنواع الإشعارات'),
      toggle(
        'الطلبات والتوصيل',
        Icons.local_shipping_outlined,
        orders,
        (v) => setState(() => orders = v),
      ),
      toggle(
        'طلبات الموافقة',
        Icons.approval_outlined,
        approvals,
        (v) => setState(() => approvals = v),
      ),
      toggle(
        'عروض الأسعار',
        Icons.request_quote_outlined,
        quotes,
        (v) => setState(() => quotes = v),
      ),
      toggle(
        'الفواتير والاستحقاقات',
        Icons.receipt_long_outlined,
        invoices,
        (v) => setState(() => invoices = v),
      ),
      toggle(
        'العروض التسويقية',
        Icons.campaign_outlined,
        promotions,
        (v) => setState(() => promotions = v),
      ),
      const SizedBox(height: 16),
      FilledButton(
        onPressed: () async {
          await ref
              .read(engagementRepositoryProvider)
              .saveNotificationPreferences(
                NotificationPreferencesModel(
                  push,
                  email,
                  sms,
                  orders,
                  approvals,
                  quotes,
                  invoices,
                  promotions,
                ),
              );
          ref.invalidate(notificationPreferencesProvider);
          if (context.mounted) snack(context, 'تم حفظ إعدادات الإشعارات');
        },
        child: const Text('حفظ الإعدادات'),
      ),
    ],
  );
}

class SupportHubScreen extends ConsumerWidget {
  const SupportHubScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('مركز الدعم')),
    body: ListView(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 90),
      children: [
        Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            gradient: const LinearGradient(
              colors: [AppColors.primary, AppColors.primaryDark],
            ),
            borderRadius: BorderRadius.circular(AppRadius.xl),
            boxShadow: AppShadows.floating,
          ),
          child: const Column(
            children: [
              Icon(Icons.support_agent, color: Colors.white, size: 42),
              SizedBox(height: 8),
              Text(
                'كيف يمكننا مساعدتك؟',
                style: TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w700,
                  fontSize: 18,
                ),
              ),
              Text(
                'فريق الدعم متاح لمتابعة طلبات شركتك',
                style: TextStyle(color: Colors.white70, fontSize: 10.5),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            SupportAction(
              Icons.add_comment_outlined,
              'تذكرة جديدة',
              () => context.push('/support/tickets/new'),
            ),
            const SizedBox(width: 8),
            SupportAction(
              Icons.confirmation_number_outlined,
              'تذاكري',
              () => context.push('/support/tickets'),
            ),
            const SizedBox(width: 8),
            SupportAction(
              Icons.phone_callback_outlined,
              'طلب مكالمة',
              () => context.push('/support/callback'),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Card(
          child: Column(
            children: [
              SupportRow(
                Icons.chat_outlined,
                'محادثة مباشرة مع المبيعات',
                'أنشئ تذكرة مبيعات وسيرد الفريق',
                () => context.push('/support/tickets/new?sales=true'),
              ),
              SupportRow(
                Icons.chat_bubble_outline,
                'التواصل عبر WhatsApp',
                'انسخ رقم واتساب خدمة العملاء',
                () => contactAction(context, ref, 'contact', true),
              ),
              SupportRow(
                Icons.help_outline,
                'الأسئلة الشائعة',
                'إجابات فورية لأكثر الأسئلة',
                () => context.push('/support/faq'),
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),
        Card(
          child: Column(
            children: [
              SupportRow(
                Icons.gavel_outlined,
                'الشروط والأحكام',
                'سياسات استخدام المنصة',
                () => context.push('/support/content/terms'),
              ),
              SupportRow(
                Icons.privacy_tip_outlined,
                'سياسة الخصوصية',
                'كيف نحمي بياناتك',
                () => context.push('/support/content/privacy'),
              ),
              SupportRow(
                Icons.info_outline,
                'من نحن',
                'تعرف على مهندسيتو',
                () => context.push('/support/content/about'),
              ),
              SupportRow(
                Icons.contact_support_outlined,
                'معلومات التواصل',
                'الهاتف والبريد والعنوان',
                () => context.push('/support/content/contact'),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

class SupportAction extends StatelessWidget {
  const SupportAction(this.icon, this.label, this.tap, {super.key});
  final IconData icon;
  final String label;
  final VoidCallback tap;
  @override
  Widget build(BuildContext context) => Expanded(
    child: InkWell(
      onTap: tap,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        height: 88,
        decoration: BoxDecoration(
          color: Colors.white,
          border: Border.all(color: AppColors.gray200),
          borderRadius: BorderRadius.circular(AppRadius.lg),
          boxShadow: AppShadows.soft,
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: AppColors.primary),
            const SizedBox(height: 6),
            Text(
              label,
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 9.5,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

class SupportRow extends StatelessWidget {
  const SupportRow(this.icon, this.title, this.subtitle, this.tap, {super.key});
  final IconData icon;
  final String title, subtitle;
  final VoidCallback tap;
  @override
  Widget build(BuildContext context) => ListTile(
    onTap: tap,
    leading: CircleAvatar(
      backgroundColor: AppColors.primaryTint,
      child: Icon(icon, color: AppColors.primary, size: 19),
    ),
    title: Text(
      title,
      style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
    ),
    subtitle: Text(subtitle, style: const TextStyle(fontSize: 9.5)),
    trailing: const Icon(Icons.chevron_right),
  );
}

class SupportTicketsScreen extends ConsumerWidget {
  const SupportTicketsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('قائمة التذاكر')),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () => context.push('/support/tickets/new'),
      icon: const Icon(Icons.add),
      label: const Text('تذكرة جديدة'),
    ),
    body: ref
        .watch(supportTicketsProvider)
        .when(
          loading: loading,
          error: errorView,
          data: (items) => items.isEmpty
              ? const AppStateView(
                  icon: Icons.confirmation_number_outlined,
                  title: 'لا توجد تذاكر',
                  message: 'يمكنك إنشاء تذكرة وسيتابعها فريق الدعم.',
                )
              : RefreshIndicator(
                  onRefresh: () async {
                    ref.invalidate(supportTicketsProvider);
                    await ref.read(supportTicketsProvider.future);
                  },
                  child: ListView.separated(
                    padding: const EdgeInsets.fromLTRB(16, 10, 16, 90),
                    itemCount: items.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 7),
                    itemBuilder: (_, i) {
                      final t = items[i];
                      return Card(
                        child: ListTile(
                          onTap: () => context.push('/support/tickets/${t.id}'),
                          leading: CircleAvatar(
                            backgroundColor: ticketColor(
                              t.status,
                            ).withValues(alpha: .1),
                            child: Icon(
                              Icons.confirmation_number_outlined,
                              color: ticketColor(t.status),
                            ),
                          ),
                          title: Text(
                            t.subject,
                            style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontSize: 11,
                            ),
                          ),
                          subtitle: Text(
                            '${t.number} • ${typeLabel(t.type)}\n${relative(t.updatedAt ?? t.createdAt)}',
                          ),
                          isThreeLine: true,
                          trailing: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              StatePill(t.status),
                              if (t.unread > 0)
                                Badge(label: Text('${t.unread}')),
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

class CreateSupportTicketScreen extends ConsumerStatefulWidget {
  const CreateSupportTicketScreen({super.key, required this.sales});
  final bool sales;
  @override
  ConsumerState<CreateSupportTicketScreen> createState() =>
      _CreateTicketState();
}

class _CreateTicketState extends ConsumerState<CreateSupportTicketScreen> {
  final subject = TextEditingController(),
      description = TextEditingController();
  String type = 'Technical', priority = 'Normal';
  List<PlatformFile> files = [];
  bool loadingState = false;
  @override
  void initState() {
    super.initState();
    if (widget.sales) {
      type = 'Other';
      subject.text = 'التواصل مع فريق المبيعات';
    }
  }

  @override
  void dispose() {
    subject.dispose();
    description.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(
      title: Text(widget.sales ? 'محادثة مع المبيعات' : 'إنشاء تذكرة دعم'),
    ),
    body: ListView(
      padding: const EdgeInsets.all(16),
      children: [
        const SectionLabel('اختر نوع المشكلة'),
        Wrap(
          spacing: 7,
          runSpacing: 7,
          children:
              [
                    'Order',
                    'Delivery',
                    'Payment',
                    'Invoice',
                    'Product',
                    'Account',
                    'Technical',
                    'Other',
                  ]
                  .map(
                    (v) => ChoiceChip(
                      label: Text(typeLabel(v)),
                      selected: type == v,
                      onSelected: (_) => setState(() => type = v),
                    ),
                  )
                  .toList(),
        ),
        const SizedBox(height: 15),
        TextField(
          controller: subject,
          decoration: const InputDecoration(
            labelText: 'عنوان المشكلة',
            prefixIcon: Icon(Icons.title),
          ),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: description,
          maxLines: 6,
          decoration: const InputDecoration(
            labelText: 'اشرح المشكلة بالتفصيل',
            alignLabelWithHint: true,
          ),
        ),
        const SizedBox(height: 10),
        DropdownButtonFormField(
          value: priority,
          decoration: const InputDecoration(labelText: 'الأولوية'),
          items: ['Low', 'Normal', 'High', 'Urgent']
              .map(
                (v) =>
                    DropdownMenuItem(value: v, child: Text(priorityLabel(v))),
              )
              .toList(),
          onChanged: (v) => setState(() => priority = v!),
        ),
        const SizedBox(height: 12),
        OutlinedButton.icon(
          onPressed: () async {
            final picked = await FilePicker.pickFiles(
              allowMultiple: true,
              withData: true,
              allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png'],
              type: FileType.custom,
            );
            if (picked != null) {
              setState(() => files = picked.files.take(5).toList());
            }
          },
          icon: const Icon(Icons.attach_file),
          label: Text(files.isEmpty ? 'رفع مرفقات' : '${files.length} مرفق'),
        ),
        if (files.isNotEmpty)
          ...files.map(
            (f) => ListTile(
              dense: true,
              leading: const Icon(Icons.insert_drive_file_outlined),
              title: Text(f.name),
              trailing: Text('${(f.size / 1024).ceil()} ك.ب'),
            ),
          ),
        const SizedBox(height: 16),
        FilledButton(
          onPressed: loadingState
              ? null
              : () async {
                  if (subject.text.trim().isEmpty ||
                      description.text.trim().isEmpty) {
                    snack(context, 'أكمل عنوان وتفاصيل المشكلة');
                    return;
                  }
                  setState(() => loadingState = true);
                  try {
                    final t = await ref
                        .read(engagementRepositoryProvider)
                        .createTicket({
                          'type': type,
                          'priority': priority,
                          'subject': subject.text,
                          'description': description.text,
                        }, files);
                    ref.invalidate(supportTicketsProvider);
                    if (context.mounted) {
                      context.go('/support/tickets/${t.id}');
                    }
                  } catch (e) {
                    if (context.mounted) {
                      snack(context, '$e');
                    }
                  } finally {
                    if (mounted) setState(() => loadingState = false);
                  }
                },
          child: Text(loadingState ? 'جارٍ الإرسال...' : 'إرسال التذكرة'),
        ),
      ],
    ),
  );
}

class SupportTicketDetailScreen extends ConsumerStatefulWidget {
  const SupportTicketDetailScreen({super.key, required this.id});
  final String id;
  @override
  ConsumerState<SupportTicketDetailScreen> createState() =>
      _TicketDetailState();
}

class _TicketDetailState extends ConsumerState<SupportTicketDetailScreen> {
  final message = TextEditingController();
  @override
  void dispose() {
    message.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('تفاصيل التذكرة')),
    body: ref
        .watch(supportTicketProvider(widget.id))
        .when(
          loading: loading,
          error: errorView,
          data: (t) => Column(
            children: [
              Container(
                width: double.infinity,
                margin: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: Colors.white,
                  border: Border.all(color: AppColors.gray200),
                  borderRadius: BorderRadius.circular(AppRadius.lg),
                  boxShadow: AppShadows.soft,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            t.subject,
                            style: const TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ),
                        StatePill(t.status),
                      ],
                    ),
                    Text(
                      '${t.number} • ${typeLabel(t.type)}',
                      style: const TextStyle(
                        fontSize: 10.5,
                        color: AppColors.gray500,
                      ),
                    ),
                  ],
                ),
              ),
              Expanded(
                child: ListView(
                  padding: const EdgeInsets.all(14),
                  children: [
                    ...t.messages.map(
                      (m) => Align(
                        alignment: m.staff
                            ? Alignment.centerLeft
                            : Alignment.centerRight,
                        child: Container(
                          margin: const EdgeInsets.only(bottom: 9),
                          constraints: const BoxConstraints(maxWidth: 300),
                          padding: const EdgeInsets.all(11),
                          decoration: BoxDecoration(
                            color: m.staff
                                ? AppColors.gray100
                                : AppColors.primary,
                            borderRadius: BorderRadius.circular(14),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                m.sender,
                                style: TextStyle(
                                  fontSize: 9.5,
                                  fontWeight: FontWeight.w700,
                                  color: m.staff
                                      ? AppColors.gray700
                                      : Colors.white70,
                                ),
                              ),
                              Text(
                                m.body,
                                style: TextStyle(
                                  fontSize: 11,
                                  color: m.staff
                                      ? AppColors.gray900
                                      : Colors.white,
                                ),
                              ),
                              Text(
                                DateFormat(
                                  'h:mm a',
                                  'ar',
                                ).format(m.createdAt.toLocal()),
                                style: TextStyle(
                                  fontSize: 10.5,
                                  color: m.staff
                                      ? AppColors.gray500
                                      : Colors.white70,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    if (t.attachments.isNotEmpty) ...[
                      const SectionLabel('المرفقات'),
                      ...t.attachments.map(
                        (a) => ListTile(
                          leading: const Icon(Icons.attach_file),
                          title: Text(a['name'] as String),
                          subtitle: Text(
                            '${((a['sizeBytes'] as num) / 1024).ceil()} ك.ب',
                          ),
                        ),
                      ),
                    ],
                    if (t.status == 'Resolved' || t.status == 'Closed')
                      RateSupportCard(t, () => rateTicket(context, ref, t)),
                  ],
                ),
              ),
              if (t.status != 'Closed')
                SafeArea(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(10, 7, 10, 8),
                    child: Row(
                      children: [
                        Expanded(
                          child: TextField(
                            controller: message,
                            decoration: const InputDecoration(
                              hintText: 'اكتب رسالتك...',
                            ),
                            minLines: 1,
                            maxLines: 3,
                          ),
                        ),
                        const SizedBox(width: 7),
                        IconButton.filled(
                          onPressed: () async {
                            if (message.text.trim().isEmpty) return;
                            await ref
                                .read(engagementRepositoryProvider)
                                .message(t.id, message.text, []);
                            message.clear();
                            ref.invalidate(supportTicketProvider(t.id));
                          },
                          icon: const Icon(Icons.send),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
  );
}

class RateSupportCard extends StatelessWidget {
  const RateSupportCard(this.ticket, this.tap, {super.key});
  final TicketDetailModel ticket;
  final VoidCallback tap;
  @override
  Widget build(BuildContext context) => Card(
    color: AppColors.warningTint,
    child: ListTile(
      onTap: ticket.rating == null ? tap : null,
      leading: const Icon(Icons.star_outline, color: AppColors.warning),
      title: Text(
        ticket.rating == null ? 'قيّم خدمة الدعم' : 'تقييمك ${ticket.rating}/5',
        style: const TextStyle(fontWeight: FontWeight.w700),
      ),
      subtitle: Text(
        ticket.rating == null
            ? 'ساعدنا على تحسين مستوى الخدمة'
            : 'شكرًا لمشاركتك',
      ),
    ),
  );
}

class CallbackScreen extends ConsumerStatefulWidget {
  const CallbackScreen({super.key});
  @override
  ConsumerState<CallbackScreen> createState() => _CallbackState();
}

class _CallbackState extends ConsumerState<CallbackScreen> {
  final phone = TextEditingController(), topic = TextEditingController();
  DateTime at = DateTime.now().add(const Duration(days: 1));
  @override
  void dispose() {
    phone.dispose();
    topic.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('طلب مكالمة')),
    body: ListView(
      padding: const EdgeInsets.all(16),
      children: [
        const AppStateView(
          icon: Icons.phone_callback_outlined,
          title: 'نتصل بك في الموعد المناسب',
          message: 'اختر الموعد وموضوع المكالمة وسيتواصل معك أحد المختصين.',
          compact: true,
        ),
        const SizedBox(height: 14),
        TextField(
          controller: phone,
          keyboardType: TextInputType.phone,
          decoration: const InputDecoration(labelText: 'رقم الهاتف'),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: topic,
          maxLines: 3,
          decoration: const InputDecoration(labelText: 'موضوع المكالمة'),
        ),
        const SizedBox(height: 10),
        ListTile(
          tileColor: AppColors.primaryTint,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          onTap: () async {
            final d = await showDatePicker(
              context: context,
              firstDate: DateTime.now(),
              lastDate: DateTime.now().add(const Duration(days: 30)),
              initialDate: at,
            );
            if (d != null) {
              setState(() => at = DateTime(d.year, d.month, d.day, 11));
            }
          },
          leading: const Icon(Icons.calendar_today, color: AppColors.primary),
          title: const Text('الموعد المفضل'),
          subtitle: Text(DateFormat('EEEE d MMMM، h:mm a', 'ar').format(at)),
        ),
        const SizedBox(height: 16),
        FilledButton(
          onPressed: () async {
            try {
              await ref
                  .read(engagementRepositoryProvider)
                  .callback(phone.text, topic.text, at);
              if (context.mounted) {
                snack(context, 'تم تسجيل طلب المكالمة');
                context.pop();
              }
            } catch (e) {
              if (context.mounted) snack(context, '$e');
            }
          },
          child: const Text('إرسال طلب المكالمة'),
        ),
      ],
    ),
  );
}

class FaqScreen extends ConsumerWidget {
  const FaqScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('الأسئلة الشائعة')),
    body: ref
        .watch(faqProvider)
        .when(
          loading: loading,
          error: errorView,
          data: (items) => ListView(
            padding: const EdgeInsets.all(16),
            children: items
                .map(
                  (f) => Card(
                    child: ExpansionTile(
                      title: Text(
                        f.question,
                        style: const TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      children: [
                        Padding(
                          padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                          child: Text(
                            f.answer,
                            style: const TextStyle(fontSize: 11, height: 1.8),
                          ),
                        ),
                      ],
                    ),
                  ),
                )
                .toList(),
          ),
        ),
  );
}

class ContentScreen extends ConsumerWidget {
  const ContentScreen({super.key, required this.slug});
  final String slug;
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    body: ref
        .watch(contentPageProvider(slug))
        .when(
          loading: loading,
          error: errorView,
          data: (p) => CustomScrollView(
            slivers: [
              SliverAppBar(pinned: true, title: Text(p.title)),
              SliverPadding(
                padding: const EdgeInsets.all(18),
                sliver: SliverList.list(
                  children: [
                    Text(
                      p.body,
                      style: const TextStyle(height: 2, fontSize: 11),
                    ),
                    if (p.phone != null) ...[
                      const SizedBox(height: 20),
                      ContactTile(Icons.phone_outlined, 'الهاتف', p.phone!),
                    ],
                    if (p.whatsApp != null)
                      ContactTile(Icons.chat_outlined, 'WhatsApp', p.whatsApp!),
                    if (p.email != null)
                      ContactTile(
                        Icons.email_outlined,
                        'البريد الإلكتروني',
                        p.email!,
                      ),
                    if (p.address != null)
                      ContactTile(
                        Icons.location_on_outlined,
                        'العنوان',
                        p.address!,
                      ),
                  ],
                ),
              ),
            ],
          ),
        ),
  );
}

class ContactTile extends StatelessWidget {
  const ContactTile(this.icon, this.label, this.value, {super.key});
  final IconData icon;
  final String label, value;
  @override
  Widget build(BuildContext context) => Card(
    child: ListTile(
      onTap: () {
        Clipboard.setData(ClipboardData(text: value));
        snack(context, 'تم النسخ');
      },
      leading: Icon(icon, color: AppColors.primary),
      title: Text(label),
      subtitle: Text(value),
      trailing: const Icon(Icons.copy, size: 18),
    ),
  );
}

class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('الإعدادات')),
    body: ref
        .watch(userSettingsProvider)
        .when(
          loading: loading,
          error: errorView,
          data: (s) => ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 90),
            children: [
              const SectionLabel('المظهر واللغة'),
              Card(
                child: Column(
                  children: [
                    ListTile(
                      leading: const Icon(
                        Icons.language,
                        color: AppColors.primary,
                      ),
                      title: const Text('اللغة'),
                      trailing: Text(
                        s.language == 'ar' ? 'العربية' : 'English',
                      ),
                      onTap: () => appearanceSheet(context, ref, s),
                    ),
                    ListTile(
                      leading: Icon(
                        s.theme == 'dark' ? Icons.dark_mode : Icons.light_mode,
                        color: AppColors.primary,
                      ),
                      title: const Text('المظهر'),
                      trailing: Text(themeLabel(s.theme)),
                      onTap: () => appearanceSheet(context, ref, s),
                    ),
                  ],
                ),
              ),
              const SectionLabel('الأمان'),
              Card(
                child: Column(
                  children: [
                    SettingsRow(
                      Icons.password,
                      'تغيير كلمة المرور',
                      'تحديث كلمة مرور الحساب',
                      () => context.push('/settings/password'),
                    ),
                    SettingsRow(
                      Icons.phonelink_lock,
                      'المصادقة الثنائية',
                      s.twoFactor ? 'مفعلة عبر الرسائل النصية' : 'غير مفعلة',
                      () => context.push('/settings/2fa'),
                    ),
                    SettingsRow(
                      Icons.devices,
                      'الأجهزة المسجل عليها الحساب',
                      '${s.sessions.length} جلسة نشطة',
                      () => context.push('/settings/sessions'),
                    ),
                    SettingsRow(
                      Icons.account_tree_outlined,
                      'الحسابات المرتبطة',
                      'Google وMicrosoft للدخول السريع',
                      () => context.push('/settings/linked-accounts'),
                    ),
                  ],
                ),
              ),
              const SectionLabel('الحساب'),
              Card(
                child: Column(
                  children: [
                    SettingsRow(
                      Icons.notifications_outlined,
                      'إعدادات الإشعارات',
                      'القنوات وأنواع التنبيهات',
                      () => context.push('/notifications/preferences'),
                    ),
                    SettingsRow(
                      Icons.delete_outline,
                      'حذف الحساب',
                      s.deletionAt == null
                          ? 'طلب حذف آمن خلال 30 يومًا'
                          : 'مجدول في ${DateFormat('d MMM yyyy', 'ar').format(s.deletionAt!)}',
                      () => context.push('/settings/delete'),
                      danger: true,
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
  );
}

class SettingsRow extends StatelessWidget {
  const SettingsRow(
    this.icon,
    this.title,
    this.subtitle,
    this.tap, {
    this.danger = false,
    super.key,
  });
  final IconData icon;
  final String title, subtitle;
  final VoidCallback tap;
  final bool danger;
  @override
  Widget build(BuildContext context) => ListTile(
    onTap: tap,
    leading: Icon(icon, color: danger ? AppColors.error : AppColors.primary),
    title: Text(
      title,
      style: TextStyle(
        fontSize: 11,
        fontWeight: FontWeight.w700,
        color: danger ? AppColors.error : null,
      ),
    ),
    subtitle: Text(subtitle, style: const TextStyle(fontSize: 9.5)),
    trailing: const Icon(Icons.chevron_right),
  );
}

class ChangePasswordScreen extends ConsumerStatefulWidget {
  const ChangePasswordScreen({super.key});
  @override
  ConsumerState<ChangePasswordScreen> createState() => _PasswordState();
}

class _PasswordState extends ConsumerState<ChangePasswordScreen> {
  final current = TextEditingController(),
      next = TextEditingController(),
      confirm = TextEditingController();
  bool obscure = true;
  @override
  void dispose() {
    current.dispose();
    next.dispose();
    confirm.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('تغيير كلمة المرور')),
    body: ListView(
      padding: const EdgeInsets.all(16),
      children: [
        TextField(
          controller: current,
          obscureText: obscure,
          decoration: const InputDecoration(
            labelText: 'كلمة المرور الحالية',
            prefixIcon: Icon(Icons.lock_outline),
          ),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: next,
          obscureText: obscure,
          decoration: const InputDecoration(
            labelText: 'كلمة المرور الجديدة',
            prefixIcon: Icon(Icons.password),
          ),
        ),
        const SizedBox(height: 10),
        TextField(
          controller: confirm,
          obscureText: obscure,
          decoration: InputDecoration(
            labelText: 'تأكيد كلمة المرور',
            prefixIcon: const Icon(Icons.verified_user_outlined),
            suffixIcon: IconButton(
              onPressed: () => setState(() => obscure = !obscure),
              icon: Icon(obscure ? Icons.visibility : Icons.visibility_off),
            ),
          ),
        ),
        const SizedBox(height: 8),
        const Text(
          '8 أحرف على الأقل، وحرف كبير ورقم.',
          style: TextStyle(fontSize: 10.5, color: AppColors.gray500),
        ),
        const SizedBox(height: 16),
        FilledButton(
          onPressed: () async {
            if (next.text != confirm.text) {
              snack(context, 'كلمتا المرور غير متطابقتين');
              return;
            }
            try {
              await ref
                  .read(engagementRepositoryProvider)
                  .changePassword(current.text, next.text);
              await ref.read(tokenStoreProvider).clear();
              if (context.mounted) {
                snack(context, 'تم تغيير كلمة المرور؛ سجل الدخول مجددًا');
                context.go('/login');
              }
            } catch (e) {
              if (context.mounted) snack(context, '$e');
            }
          },
          child: const Text('تحديث كلمة المرور'),
        ),
      ],
    ),
  );
}

class TwoFactorSettingsScreen extends ConsumerWidget {
  const TwoFactorSettingsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('المصادقة الثنائية')),
    body: ref
        .watch(userSettingsProvider)
        .when(
          loading: loading,
          error: errorView,
          data: (s) => ListView(
            padding: const EdgeInsets.all(16),
            children: [
              AppStateView(
                icon: s.twoFactor ? Icons.verified_user : Icons.phonelink_lock,
                title: s.twoFactor
                    ? 'المصادقة الثنائية مفعلة'
                    : 'أضف طبقة حماية إضافية',
                message: s.twoFactor
                    ? 'سنطلب رمزًا عبر SMS عند الدخول بالبريد وكلمة المرور.'
                    : 'سيرسل رمز تحقق إلى هاتفك بعد كلمة المرور.',
                compact: true,
              ),
              const SizedBox(height: 16),
              if (!s.twoFactor)
                FilledButton.icon(
                  onPressed: () => enableTwoFactor(context, ref),
                  icon: const Icon(Icons.sms_outlined),
                  label: const Text('التفعيل عبر رسالة نصية'),
                )
              else
                FilledButton.icon(
                  style: FilledButton.styleFrom(
                    backgroundColor: AppColors.error,
                  ),
                  onPressed: () => disableTwoFactor(context, ref),
                  icon: const Icon(Icons.lock_open_outlined),
                  label: const Text('إيقاف المصادقة الثنائية'),
                ),
            ],
          ),
        ),
  );
}

class SessionsScreen extends ConsumerWidget {
  const SessionsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('الأجهزة المسجل عليها الحساب')),
    body: ref
        .watch(userSettingsProvider)
        .when(
          loading: loading,
          error: errorView,
          data: (s) => ListView(
            padding: const EdgeInsets.all(16),
            children: [
              ...s.sessions.map(
                (x) => Card(
                  child: ListTile(
                    leading: const CircleAvatar(
                      backgroundColor: AppColors.primaryTint,
                      child: Icon(Icons.devices, color: AppColors.primary),
                    ),
                    title: Text(
                      x.device,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    subtitle: Text(
                      'بدأت ${DateFormat('d MMM yyyy، h:mm a', 'ar').format(x.createdAt.toLocal())}\nتنتهي ${DateFormat('d MMM yyyy', 'ar').format(x.expiresAt.toLocal())}',
                    ),
                    isThreeLine: true,
                    trailing: IconButton(
                      onPressed: () async {
                        await ref
                            .read(engagementRepositoryProvider)
                            .revokeSession(x.id);
                        ref.invalidate(userSettingsProvider);
                      },
                      icon: const Icon(Icons.logout, color: AppColors.error),
                    ),
                  ),
                ),
              ),
              if (s.sessions.isNotEmpty)
                OutlinedButton.icon(
                  onPressed: () async {
                    await ref
                        .read(engagementRepositoryProvider)
                        .revokeSession(null);
                    await ref.read(tokenStoreProvider).clear();
                    ref.invalidate(userSettingsProvider);
                    if (context.mounted) context.go('/login');
                  },
                  icon: const Icon(Icons.logout),
                  label: const Text('تسجيل الخروج من كل الأجهزة'),
                ),
            ],
          ),
        ),
  );
}

class DeleteAccountScreen extends ConsumerStatefulWidget {
  const DeleteAccountScreen({super.key});
  @override
  ConsumerState<DeleteAccountScreen> createState() => _DeleteState();
}

class _DeleteState extends ConsumerState<DeleteAccountScreen> {
  final password = TextEditingController(), reason = TextEditingController();
  @override
  void dispose() {
    password.dispose();
    reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('حذف الحساب')),
    body: ref
        .watch(userSettingsProvider)
        .when(
          loading: loading,
          error: errorView,
          data: (s) => ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (s.deletionAt != null) ...[
                AppStateView(
                  icon: Icons.schedule,
                  title: 'الحذف مجدول',
                  message:
                      'سيُحذف الحساب في ${DateFormat('d MMMM yyyy', 'ar').format(s.deletionAt!)} ويمكنك إلغاء الطلب قبل هذا الموعد.',
                  compact: true,
                ),
                const SizedBox(height: 14),
                OutlinedButton(
                  onPressed: () async {
                    await ref
                        .read(engagementRepositoryProvider)
                        .cancelDeletion();
                    ref.invalidate(userSettingsProvider);
                  },
                  child: const Text('إلغاء طلب الحذف'),
                ),
              ] else ...[
                Container(
                  padding: const EdgeInsets.all(13),
                  decoration: BoxDecoration(
                    color: AppColors.errorTint,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Text(
                    'سيتم إيقاف الجلسات فورًا، مع فترة استرجاع 30 يومًا قبل الحذف النهائي.',
                    style: TextStyle(
                      color: AppColors.error,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                const SizedBox(height: 14),
                TextField(
                  controller: reason,
                  maxLines: 3,
                  decoration: const InputDecoration(
                    labelText: 'سبب حذف الحساب',
                  ),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: password,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'كلمة المرور للتأكيد',
                  ),
                ),
                const SizedBox(height: 16),
                FilledButton(
                  style: FilledButton.styleFrom(
                    backgroundColor: AppColors.error,
                  ),
                  onPressed: () async {
                    try {
                      final date = await ref
                          .read(engagementRepositoryProvider)
                          .deleteAccount(password.text, reason.text);
                      await ref.read(tokenStoreProvider).clear();
                      ref.invalidate(userSettingsProvider);
                      if (context.mounted) {
                        snack(
                          context,
                          'تم جدولة الحذف في ${DateFormat('d MMM yyyy', 'ar').format(date)}',
                        );
                        context.go('/login');
                      }
                    } catch (e) {
                      if (context.mounted) snack(context, '$e');
                    }
                  },
                  child: const Text('تأكيد حذف الحساب'),
                ),
              ],
            ],
          ),
        ),
  );
}

class AppStateView extends StatelessWidget {
  const AppStateView({
    super.key,
    required this.icon,
    required this.title,
    required this.message,
    this.action,
    this.compact = false,
  });
  final IconData icon;
  final String title, message;
  final Widget? action;
  final bool compact;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: EdgeInsets.all(compact ? 18 : 30),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircleAvatar(
            radius: compact ? 31 : 40,
            backgroundColor: AppColors.primaryTint,
            child: Icon(
              icon,
              size: compact ? 29 : 38,
              color: AppColors.primary,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            title,
            textAlign: TextAlign.center,
            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
          ),
          const SizedBox(height: 5),
          Text(
            message,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 10.5,
              color: AppColors.gray500,
              height: 1.7,
            ),
          ),
          if (action != null) ...[const SizedBox(height: 14), action!],
        ],
      ),
    ),
  );
}

class SystemRuntimeScreen extends StatelessWidget {
  const SystemRuntimeScreen({super.key, required this.type, this.config});
  final String type;
  final MobileAppConfigModel? config;
  @override
  Widget build(BuildContext context) {
    final maintenance = type == 'maintenance',
        required = type == 'update-required';
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: AppStateView(
          icon: maintenance
              ? Icons.engineering_outlined
              : Icons.system_update_alt,
          title: maintenance
              ? 'نقوم بأعمال صيانة'
              : required
              ? 'تحديث مطلوب للمتابعة'
              : 'يتوفر تحديث جديد',
          message:
              config?.message ??
              (maintenance
                  ? 'نعمل على تحسين الخدمة وسنعود في أقرب وقت.'
                  : 'حدّث التطبيق للحصول على أحدث المزايا والتحسينات الأمنية.'),
          action: Column(
            children: [
              FilledButton.icon(
                onPressed: () async {
                  final url = config?.updateUrl;
                  if (url != null) {
                    await Clipboard.setData(ClipboardData(text: url));
                    if (context.mounted) snack(context, 'تم نسخ رابط التحديث');
                  }
                },
                icon: const Icon(Icons.download),
                label: const Text('تحديث الآن'),
              ),
              if (!maintenance && !required)
                TextButton(
                  onPressed: () => context.go('/login'),
                  child: const Text('لاحقًا'),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class SectionLabel extends StatelessWidget {
  const SectionLabel(this.text, {super.key});
  final String text;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(4, 14, 4, 8),
    child: Text(
      text,
      style: const TextStyle(
        fontSize: 10.5,
        color: AppColors.gray500,
        fontWeight: FontWeight.w700,
      ),
    ),
  );
}

class StatePill extends StatelessWidget {
  const StatePill(this.status, {super.key});
  final String status;
  @override
  Widget build(BuildContext context) {
    final c = ticketColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: c.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        statusLabel(status),
        style: TextStyle(fontSize: 10.5, color: c, fontWeight: FontWeight.w700),
      ),
    );
  }
}

Widget toggle(
  String title,
  IconData icon,
  bool value,
  ValueChanged<bool> changed,
) => Card(
  child: SwitchListTile(
    value: value,
    onChanged: changed,
    secondary: Icon(icon, color: AppColors.primary),
    title: Text(
      title,
      style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
    ),
  ),
);
Widget loading() => const Center(child: CircularProgressIndicator());
Widget errorView(Object e, StackTrace _) => AppStateView(
  icon: Icons.cloud_off_outlined,
  title: 'تعذر تحميل البيانات',
  message: '$e',
);
void snack(BuildContext context, String text) =>
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));
(IconData, Color) notificationMeta(String type) => type.contains('invoice')
    ? (Icons.receipt_long_outlined, AppColors.error)
    : type.contains('approval')
    ? (Icons.approval_outlined, AppColors.warning)
    : type.contains('quote')
    ? (Icons.request_quote_outlined, AppColors.success)
    : type.contains('delivery')
    ? (Icons.local_shipping_outlined, AppColors.warning)
    : (Icons.notifications_outlined, AppColors.info);
void openNotification(BuildContext context, NotificationModel n) {
  if (n.entityId == null) return;
  if (n.entityType == 'Order') {
    context.push('/orders/${n.entityId}');
  } else if (n.entityType == 'Invoice') {
    context.push('/finance/invoices/${n.entityId}');
  } else if (n.type.contains('approval')) {
    context.push('/approvals/${n.entityId}');
  } else if (n.type.contains('quote')) {
    context.push('/rfqs/${n.entityId}');
  }
}

String relative(DateTime at) {
  final d = DateTime.now().difference(at.toLocal());
  if (d.inMinutes < 1) return 'الآن';
  if (d.inHours < 1) return 'منذ ${d.inMinutes} دقيقة';
  if (d.inDays < 1) return 'منذ ${d.inHours} ساعة';
  return DateFormat('d MMM yyyy', 'ar').format(at.toLocal());
}

Color ticketColor(String s) => s == 'Resolved' || s == 'Closed'
    ? AppColors.success
    : s == 'WaitingCustomer'
    ? AppColors.warning
    : s == 'Urgent'
    ? AppColors.error
    : AppColors.primary;
String statusLabel(String s) =>
    {
      'Open': 'مفتوحة',
      'InProgress': 'قيد المعالجة',
      'WaitingCustomer': 'بانتظار ردك',
      'Resolved': 'تم الحل',
      'Closed': 'مغلقة',
    }[s] ??
    s;
String typeLabel(String s) =>
    {
      'Order': 'طلب',
      'Delivery': 'التوصيل',
      'Payment': 'الدفع',
      'Invoice': 'الفاتورة',
      'Product': 'منتج',
      'Account': 'الحساب',
      'Technical': 'مشكلة تقنية',
      'Other': 'أخرى',
    }[s] ??
    s;
String priorityLabel(String s) =>
    {
      'Low': 'منخفضة',
      'Normal': 'عادية',
      'High': 'عالية',
      'Urgent': 'عاجلة',
    }[s] ??
    s;
String themeLabel(String s) =>
    {'system': 'تلقائي', 'light': 'فاتح', 'dark': 'داكن'}[s] ?? s;
Future<void> appearanceSheet(
  BuildContext context,
  WidgetRef ref,
  UserSettingsModel s,
) async {
  var language = s.language, theme = s.theme;
  await showModalBottomSheet(
    context: context,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (_, set) => Padding(
        padding: const EdgeInsets.fromLTRB(18, 0, 18, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'اللغة والمظهر',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
            ),
            const SizedBox(height: 14),
            SegmentedButton(
              segments: const [
                ButtonSegment(value: 'ar', label: Text('العربية')),
                ButtonSegment(value: 'en', label: Text('English')),
              ],
              selected: {language},
              onSelectionChanged: (v) => set(() => language = v.first),
            ),
            const SizedBox(height: 12),
            SegmentedButton(
              segments: const [
                ButtonSegment(
                  value: 'system',
                  icon: Icon(Icons.brightness_auto),
                  label: Text('تلقائي'),
                ),
                ButtonSegment(
                  value: 'light',
                  icon: Icon(Icons.light_mode),
                  label: Text('فاتح'),
                ),
                ButtonSegment(
                  value: 'dark',
                  icon: Icon(Icons.dark_mode),
                  label: Text('داكن'),
                ),
              ],
              selected: {theme},
              onSelectionChanged: (v) => set(() => theme = v.first),
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: () async {
                await ref
                    .read(engagementRepositoryProvider)
                    .appearance(language, theme);
                ref.read(themeModeProvider.notifier).set(theme);
                ref.read(localeProvider.notifier).set(language);
                ref.invalidate(userSettingsProvider);
                if (sheet.mounted) Navigator.pop(sheet);
              },
              child: const Text('تطبيق'),
            ),
          ],
        ),
      ),
    ),
  );
}

Future<void> enableTwoFactor(BuildContext context, WidgetRef ref) async {
  final code = TextEditingController();
  final dev = await ref.read(engagementRepositoryProvider).requestTwoFactor();
  code.text = dev ?? '';
  if (!context.mounted) return;
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => Padding(
      padding: EdgeInsets.fromLTRB(
        18,
        0,
        18,
        MediaQuery.viewInsetsOf(sheet).bottom + 24,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'أدخل رمز التحقق',
            style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: code,
            maxLength: 6,
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            decoration: const InputDecoration(labelText: 'رمز SMS'),
          ),
          FilledButton(
            onPressed: () async {
              try {
                await ref
                    .read(engagementRepositoryProvider)
                    .enableTwoFactor(code.text);
                ref.invalidate(userSettingsProvider);
                if (sheet.mounted) Navigator.pop(sheet);
              } catch (e) {
                if (sheet.mounted) snack(sheet, '$e');
              }
            },
            child: const Text('تفعيل الحماية'),
          ),
        ],
      ),
    ),
  );
  code.dispose();
}

Future<void> disableTwoFactor(BuildContext context, WidgetRef ref) async {
  final pass = TextEditingController();
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => Padding(
      padding: EdgeInsets.fromLTRB(
        18,
        0,
        18,
        MediaQuery.viewInsetsOf(sheet).bottom + 24,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const Text(
            'إيقاف المصادقة الثنائية',
            style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: pass,
            obscureText: true,
            decoration: const InputDecoration(labelText: 'كلمة المرور'),
          ),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: AppColors.error),
            onPressed: () async {
              try {
                await ref
                    .read(engagementRepositoryProvider)
                    .disableTwoFactor(pass.text);
                ref.invalidate(userSettingsProvider);
                if (sheet.mounted) Navigator.pop(sheet);
              } catch (e) {
                if (sheet.mounted) snack(sheet, '$e');
              }
            },
            child: const Text('تأكيد الإيقاف'),
          ),
        ],
      ),
    ),
  );
  pass.dispose();
}

Future<void> rateTicket(
  BuildContext context,
  WidgetRef ref,
  TicketDetailModel t,
) async {
  var rating = 5;
  final comment = TextEditingController();
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (_, set) => Padding(
        padding: EdgeInsets.fromLTRB(
          18,
          0,
          18,
          MediaQuery.viewInsetsOf(sheet).bottom + 24,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'تقييم خدمة الدعم',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 18),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(
                5,
                (i) => IconButton(
                  onPressed: () => set(() => rating = i + 1),
                  icon: Icon(
                    i < rating ? Icons.star : Icons.star_border,
                    color: AppColors.warning,
                    size: 30,
                  ),
                ),
              ),
            ),
            TextField(
              controller: comment,
              maxLines: 3,
              decoration: const InputDecoration(
                labelText: 'ملاحظاتك (اختياري)',
              ),
            ),
            const SizedBox(height: 12),
            FilledButton(
              onPressed: () async {
                await ref
                    .read(engagementRepositoryProvider)
                    .rateTicket(t.id, rating, comment.text);
                ref.invalidate(supportTicketProvider(t.id));
                if (sheet.mounted) Navigator.pop(sheet);
              },
              child: const Text('إرسال التقييم'),
            ),
          ],
        ),
      ),
    ),
  );
  comment.dispose();
}

Future<void> contactAction(
  BuildContext context,
  WidgetRef ref,
  String slug,
  bool whatsApp,
) async {
  try {
    final p = await ref.read(contentPageProvider(slug).future);
    final value = whatsApp ? p.whatsApp : p.phone;
    if (value != null) {
      await Clipboard.setData(ClipboardData(text: value));
      if (context.mounted) snack(context, 'تم نسخ $value');
    }
  } catch (e) {
    if (context.mounted) snack(context, '$e');
  }
}
