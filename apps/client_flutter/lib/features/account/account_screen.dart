import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../core/widgets/skeleton.dart';
import '../../core/api/account_repository.dart';
import '../../core/theme/app_tokens.dart';

String _roleLabel(String? code) => switch (code) {
  'company_owner' => 'صاحب الشركة',
  'purchasing_officer' => 'موظف مشتريات',
  'company_admin' => 'مسؤول إداري',
  'finance_manager' => 'مدير مالي',
  'warehouse_officer' => 'مسؤول مخازن',
  'department_manager' => 'مدير إدارة',
  'approver' => 'مسؤول موافقات',
  'billing_officer' => 'مسؤول فواتير',
  'requester' => 'مستخدم مخول بالطلب',
  _ => code ?? 'مستخدم شركة',
};

class AccountScreen extends ConsumerWidget {
  const AccountScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    body: SafeArea(
      bottom: false,
      child: Column(
        children: [
          const _AccountHeader(),
          Expanded(
            child: ref
                .watch(accountOverviewProvider)
                .when(
                  loading: () => const ListSkeleton(),
                  error: (e, _) => ErrorView(
                    e,
                    () => ref.invalidate(accountOverviewProvider),
                  ),
                  data: (o) => RefreshIndicator(
                    onRefresh: () async {
                      ref.invalidate(accountOverviewProvider);
                      await ref.read(accountOverviewProvider.future);
                    },
                    child: ListView(
                      padding: const EdgeInsets.fromLTRB(16, 4, 16, 110),
                      children: [
                        _ProfileHero(overview: o),
                        const SizedBox(height: 14),
                        const _OrdersHub(),
                        const SizedBox(height: 14),
                        Row(
                          children: [
                            MetricBox(
                              '${o.users}',
                              'مستخدم',
                              Icons.group_outlined,
                            ),
                            const SizedBox(width: 8),
                            MetricBox(
                              '${o.branches}',
                              'فرع',
                              Icons.apartment_outlined,
                            ),
                            const SizedBox(width: 8),
                            MetricBox(
                              '${o.invites}',
                              'دعوة معلقة',
                              Icons.mail_outline,
                            ),
                          ],
                        ),
                        if (o.creditLimit > 0) ...[
                          const SizedBox(height: 14),
                          CreditCard(limit: o.creditLimit, used: o.creditUsed),
                        ],
                        const SizedBox(height: 14),
                        const _QuickCommerceActions(),
                        const SizedBox(height: 14),
                        const AccountGroup(
                          title: 'إدارة حساب الشركة',
                          items: [
                            AccountItem(
                              'بيانات الشركة',
                              Icons.business_outlined,
                              '/account/company',
                            ),
                            AccountItem(
                              'المستندات والتوثيق',
                              Icons.verified_outlined,
                              '/account/documents',
                            ),
                            AccountItem(
                              'الفروع وعناوين التوصيل',
                              Icons.location_on_outlined,
                              '/account/branches',
                            ),
                            AccountItem(
                              'المستخدمون والصلاحيات',
                              Icons.group_outlined,
                              '/account/users',
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        const AccountGroup(
                          title: 'التحكم المؤسسي',
                          items: [
                            AccountItem(
                              'الأدوار ومصفوفة الصلاحيات',
                              Icons.admin_panel_settings_outlined,
                              '/account/roles',
                            ),
                            AccountItem(
                              'مستويات الموافقة',
                              Icons.rule_folder_outlined,
                              '/account/approvals',
                            ),
                            AccountItem(
                              'مراكز التكلفة',
                              Icons.account_balance_wallet_outlined,
                              '/account/cost-centers',
                            ),
                            AccountItem(
                              'سجل نشاط النظام',
                              Icons.history_rounded,
                              '/account/audit',
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        const AccountGroup(
                          title: 'الفوترة والتعاقد',
                          items: [
                            AccountItem(
                              'الهوية البصرية للشركة',
                              Icons.palette_outlined,
                              '/account/brand',
                            ),
                            AccountItem(
                              'بيانات الفوترة والضرائب',
                              Icons.receipt_long_outlined,
                              '/account/billing',
                            ),
                            AccountItem(
                              'العقود وشروط الدفع',
                              Icons.description_outlined,
                              '/account/contracts',
                            ),
                          ],
                        ),
                        const SizedBox(height: 14),
                        const AccountGroup(
                          title: 'المساعدة والإعدادات',
                          items: [
                            AccountItem(
                              'الإشعارات',
                              Icons.notifications_outlined,
                              '/notifications',
                            ),
                            AccountItem(
                              'مركز الدعم',
                              Icons.support_agent_outlined,
                              '/support',
                            ),
                            AccountItem(
                              'الإعدادات والأمان',
                              Icons.settings_outlined,
                              '/settings',
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
          ),
        ],
      ),
    ),
  );
}

class _AccountHeader extends StatelessWidget {
  const _AccountHeader();

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
    child: Row(
      children: [
        Text(
          'حسابي',
          style: Theme.of(
            context,
          ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
        ),
        const Spacer(),
        IconButton.filledTonal(
          tooltip: 'الإشعارات',
          onPressed: () => context.push('/notifications'),
          icon: const Icon(Icons.notifications_none_rounded),
        ),
        const SizedBox(width: 6),
        IconButton.filledTonal(
          tooltip: 'الإعدادات',
          onPressed: () => context.push('/settings'),
          icon: const Icon(Icons.settings_outlined),
        ),
      ],
    ),
  );
}

class _ProfileHero extends StatelessWidget {
  const _ProfileHero({required this.overview});
  final AccountOverviewModel overview;

  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => context.push('/account/profile'),
    borderRadius: BorderRadius.circular(AppRadius.xxl),
    child: Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF0754D5), AppColors.primaryDark],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.circular(AppRadius.xxl),
        boxShadow: AppShadows.floating,
      ),
      child: Row(
        children: [
          Container(
            width: 66,
            height: 66,
            decoration: BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
              border: Border.all(
                color: Colors.white.withValues(alpha: .55),
                width: 3,
              ),
            ),
            alignment: Alignment.center,
            child: Text(
              initials(overview.profile.name),
              style: const TextStyle(
                color: AppColors.primary,
                fontWeight: FontWeight.w700,
                fontSize: 18,
              ),
            ),
          ),
          const SizedBox(width: 13),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  overview.profile.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.w700,
                    fontSize: 16,
                  ),
                ),
                Text(
                  overview.profile.jobTitle ??
                      _roleLabel(overview.profile.roles.firstOrNull),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: Colors.white70, fontSize: 10.5),
                ),
                const SizedBox(height: 5),
                Row(
                  children: [
                    const Icon(
                      Icons.verified_rounded,
                      color: Color(0xFF7EE2A8),
                      size: 16,
                    ),
                    const SizedBox(width: 4),
                    Expanded(
                      child: Text(
                        overview.company.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 9.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          const Icon(Icons.edit_outlined, color: Colors.white, size: 22),
        ],
      ),
    ),
  );
}

class _OrdersHub extends StatelessWidget {
  const _OrdersHub();

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.fromLTRB(14, 14, 14, 12),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: Column(
      children: [
        Row(
          children: [
            const Text(
              'طلباتي',
              style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14),
            ),
            const Spacer(),
            TextButton(
              onPressed: () => context.go('/orders'),
              child: const Text('عرض الكل'),
            ),
          ],
        ),
        const SizedBox(height: 4),
        Row(
          children: [
            _OrderShortcut(
              Icons.hourglass_top_rounded,
              'قيد المراجعة',
              AppColors.warning,
              () => context.go('/orders'),
            ),
            _OrderShortcut(
              Icons.inventory_2_outlined,
              'قيد التجهيز',
              AppColors.primary,
              () => context.go('/orders'),
            ),
            _OrderShortcut(
              Icons.local_shipping_outlined,
              'في الطريق',
              AppColors.info,
              () => context.go('/orders'),
            ),
            _OrderShortcut(
              Icons.assignment_return_outlined,
              'المرتجعات',
              AppColors.error,
              () => context.push('/returns'),
            ),
          ],
        ),
      ],
    ),
  );
}

class _OrderShortcut extends StatelessWidget {
  const _OrderShortcut(this.icon, this.label, this.color, this.onTap);
  final IconData icon;
  final String label;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Expanded(
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 7, horizontal: 2),
        child: Column(
          children: [
            Container(
              width: 42,
              height: 42,
              decoration: BoxDecoration(
                color: color.withValues(alpha: .1),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(icon, color: color, size: 22),
            ),
            const SizedBox(height: 6),
            Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                color: AppColors.gray600,
                fontSize: 8.5,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

class _QuickCommerceActions extends StatelessWidget {
  const _QuickCommerceActions();

  @override
  Widget build(BuildContext context) => Row(
    children: [
      _QuickAction('المفضلة', Icons.favorite_border_rounded, '/favorites'),
      const SizedBox(width: 8),
      _QuickAction('الفواتير', Icons.receipt_long_outlined, '/finance'),
      const SizedBox(width: 8),
      _QuickAction('العروض', Icons.request_quote_outlined, '/rfqs'),
      const SizedBox(width: 8),
      _QuickAction('الدعم', Icons.support_agent_outlined, '/support'),
    ],
  );
}

class _QuickAction extends StatelessWidget {
  const _QuickAction(this.label, this.icon, this.route);
  final String label, route;
  final IconData icon;

  @override
  Widget build(BuildContext context) => Expanded(
    child: InkWell(
      onTap: () => context.push(route),
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 13, horizontal: 4),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.gray150),
        ),
        child: Column(
          children: [
            Icon(icon, color: AppColors.primary, size: 22),
            const SizedBox(height: 5),
            Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 9, fontWeight: FontWeight.w700),
            ),
          ],
        ),
      ),
    ),
  );
}

class ProfileEditorScreen extends ConsumerStatefulWidget {
  const ProfileEditorScreen({super.key});
  @override
  ConsumerState<ProfileEditorScreen> createState() => _ProfileEditorState();
}

class _ProfileEditorState extends ConsumerState<ProfileEditorScreen> {
  final name = TextEditingController(),
      email = TextEditingController(),
      title = TextEditingController(),
      department = TextEditingController();
  bool loaded = false, saving = false;
  String language = 'ar';
  String? branch;
  @override
  void dispose() {
    name.dispose();
    email.dispose();
    title.dispose();
    department.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('الملف الشخصي')),
    body: ref
        .watch(accountOverviewProvider)
        .when(
          loading: () => const ListSkeleton(),
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountOverviewProvider)),
          data: (o) {
            if (!loaded) {
              final p = o.profile;
              name.text = p.name;
              email.text = p.email ?? '';
              title.text = p.jobTitle ?? '';
              department.text = p.department ?? '';
              language = p.language;
              branch = p.branchId;
              loaded = true;
            }
            return ref
                .watch(accountBranchesProvider)
                .when(
                  loading: () =>
                      const Center(child: CircularProgressIndicator()),
                  error: (e, _) => ErrorView(
                    e,
                    () => ref.invalidate(accountBranchesProvider),
                  ),
                  data: (branches) => ListView(
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
                    children: [
                      Container(
                        padding: const EdgeInsets.all(18),
                        decoration: BoxDecoration(
                          gradient: const LinearGradient(
                            colors: [Color(0xFF0754D5), AppColors.primaryDark],
                            begin: Alignment.topRight,
                            end: Alignment.bottomLeft,
                          ),
                          borderRadius: BorderRadius.circular(AppRadius.xxl),
                          boxShadow: AppShadows.floating,
                        ),
                        child: Row(
                          children: [
                            Stack(
                              clipBehavior: Clip.none,
                              children: [
                                CircleAvatar(
                                  radius: 38,
                                  backgroundColor: Colors.white,
                                  child: Text(
                                    initials(name.text),
                                    style: const TextStyle(
                                      color: AppColors.primary,
                                      fontWeight: FontWeight.w700,
                                      fontSize: 21,
                                    ),
                                  ),
                                ),
                                Positioned(
                                  left: -2,
                                  bottom: -2,
                                  child: Container(
                                    width: 27,
                                    height: 27,
                                    decoration: BoxDecoration(
                                      color: AppColors.primary,
                                      shape: BoxShape.circle,
                                      border: Border.all(
                                        color: Colors.white,
                                        width: 2,
                                      ),
                                    ),
                                    child: const Icon(
                                      Icons.camera_alt_outlined,
                                      color: Colors.white,
                                      size: 14,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    o.profile.name,
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(
                                      color: Colors.white,
                                      fontWeight: FontWeight.w700,
                                      fontSize: 16,
                                    ),
                                  ),
                                  Text(
                                    o.profile.jobTitle ??
                                        _roleLabel(o.profile.roles.firstOrNull),
                                    style: const TextStyle(
                                      color: Colors.white70,
                                      fontSize: 10.5,
                                    ),
                                  ),
                                  const SizedBox(height: 5),
                                  const Row(
                                    children: [
                                      Icon(
                                        Icons.verified_rounded,
                                        color: Color(0xFF7EE2A8),
                                        size: 16,
                                      ),
                                      SizedBox(width: 4),
                                      Text(
                                        'حساب شركة موثّق',
                                        style: TextStyle(
                                          color: Colors.white,
                                          fontSize: 9.5,
                                          fontWeight: FontWeight.w700,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 22),
                      const Text(
                        'البيانات الشخصية',
                        style: TextStyle(
                          color: AppColors.gray900,
                          fontSize: 14,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      const SizedBox(height: 5),
                      const Text(
                        'تُستخدم هذه البيانات في الطلبات والفواتير والتواصل.',
                        style: TextStyle(
                          color: AppColors.gray500,
                          fontSize: 10.5,
                        ),
                      ),
                      const SizedBox(height: 14),
                      Field(name, 'الاسم بالكامل', icon: Icons.person_outline),
                      Field(
                        email,
                        'البريد الإلكتروني',
                        icon: Icons.email_outlined,
                        type: TextInputType.emailAddress,
                      ),
                      Field(
                        title,
                        'المسمى الوظيفي',
                        icon: Icons.badge_outlined,
                      ),
                      Field(
                        department,
                        'الإدارة / القسم',
                        icon: Icons.corporate_fare_outlined,
                      ),
                      DropdownButtonFormField<String?>(
                        value: branch,
                        decoration: const InputDecoration(
                          labelText: 'الفرع الافتراضي',
                          prefixIcon: Icon(Icons.apartment_outlined),
                        ),
                        items: [
                          const DropdownMenuItem(
                            value: null,
                            child: Text('بدون فرع افتراضي'),
                          ),
                          ...branches.map(
                            (b) => DropdownMenuItem(
                              value: b.id,
                              child: Text(b.name),
                            ),
                          ),
                        ],
                        onChanged: (v) => setState(() => branch = v),
                      ),
                      const SizedBox(height: 12),
                      SegmentedButton<String>(
                        segments: const [
                          ButtonSegment(value: 'ar', label: Text('العربية')),
                          ButtonSegment(value: 'en', label: Text('English')),
                        ],
                        selected: {language},
                        onSelectionChanged: (v) =>
                            setState(() => language = v.first),
                      ),
                      const SizedBox(height: 20),
                      FilledButton.icon(
                        onPressed: saving
                            ? null
                            : () async {
                                setState(() => saving = true);
                                try {
                                  await ref
                                      .read(accountRepositoryProvider)
                                      .updateProfile({
                                        'fullName': name.text,
                                        'email': email.text,
                                        'language': language,
                                        'jobTitle': title.text,
                                        'department': department.text,
                                        'defaultBranchId': branch,
                                      });
                                  ref.invalidate(accountOverviewProvider);
                                  if (context.mounted) {
                                    toast(context, 'تم حفظ البيانات');
                                    context.pop();
                                  }
                                } catch (e) {
                                  if (context.mounted) toast(context, '$e');
                                } finally {
                                  if (mounted) setState(() => saving = false);
                                }
                              },
                        icon: saving
                            ? const SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : const Icon(Icons.check_rounded),
                        label: Text(saving ? 'جارٍ الحفظ...' : 'حفظ التعديلات'),
                      ),
                    ],
                  ),
                );
          },
        ),
  );
}

class CompanyAccountScreen extends ConsumerWidget {
  const CompanyAccountScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('بيانات الشركة')),
    body: ref
        .watch(accountOverviewProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountOverviewProvider)),
          data: (o) {
            final c = o.company;
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                StatusBanner(c.status),
                const SizedBox(height: 12),
                DetailCard(
                  title: c.name,
                  icon: Icons.business,
                  rows: {
                    'الاسم بالإنجليزية': c.nameEn ?? '—',
                    'السجل التجاري': c.registrationNo ?? '—',
                    'البطاقة الضريبية': c.taxNo ?? '—',
                    'النشاط': c.industry ?? '—',
                    'الهاتف': c.phone,
                    'البريد': c.email ?? '—',
                    'العنوان': [
                      c.address,
                      c.city,
                      c.governorate,
                    ].whereType<String>().join('، '),
                  },
                ),
                const SizedBox(height: 14),
                FilledButton.icon(
                  onPressed: () => editCompany(context, ref, c),
                  icon: const Icon(Icons.edit_outlined),
                  label: const Text('تعديل بيانات الشركة'),
                ),
              ],
            );
          },
        ),
  );
}

class AccountDocumentsScreen extends ConsumerWidget {
  const AccountDocumentsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('مستندات الشركة')),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () => context.push('/documents'),
      icon: const Icon(Icons.upload_file),
      label: const Text('رفع مستند'),
    ),
    body: ref
        .watch(accountDocumentsProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountDocumentsProvider)),
          data: (docs) => docs.isEmpty
              ? const EmptyView(
                  'لم يتم رفع مستندات بعد',
                  Icons.folder_off_outlined,
                )
              : ListView(
                  padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
                  children: docs
                      .map(
                        (d) => Card(
                          child: ListTile(
                            leading: const Icon(
                              Icons.picture_as_pdf_outlined,
                              color: AppColors.primary,
                            ),
                            title: Text(
                              documentName(d.type),
                              style: const TextStyle(
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            subtitle: Text(
                              '${d.name} • ${(d.size / 1048576).toStringAsFixed(1)} م.ب',
                            ),
                            trailing: StatusChip(d.status),
                            onTap: () => toast(
                              context,
                              'المستند متاح للتحميل الآمن من حساب الشركة',
                            ),
                          ),
                        ),
                      )
                      .toList(),
                ),
        ),
  );
}

class BranchesScreen extends ConsumerWidget {
  const BranchesScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('فروع وعناوين الشركة')),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () => editBranch(context, ref),
      icon: const Icon(Icons.add_location_alt_outlined),
      label: const Text('إضافة فرع'),
    ),
    body: ref
        .watch(accountBranchesProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountBranchesProvider)),
          data: (items) => ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
            children: items
                .map(
                  (b) => Card(
                    child: ListTile(
                      onTap: () => editBranch(context, ref, b),
                      leading: CircleAvatar(
                        backgroundColor: b.main
                            ? AppColors.primary
                            : AppColors.primaryTint,
                        child: Icon(
                          b.main ? Icons.business : Icons.apartment,
                          color: b.main ? Colors.white : AppColors.primary,
                        ),
                      ),
                      title: Row(
                        children: [
                          Expanded(
                            child: Text(
                              b.name,
                              style: const TextStyle(
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                          if (b.main) const StatusChip('Main'),
                        ],
                      ),
                      subtitle: Text(
                        [
                          b.address,
                          b.city,
                          b.governorate,
                        ].whereType<String>().join('، '),
                      ),
                      trailing: const Icon(Icons.edit_outlined),
                    ),
                  ),
                )
                .toList(),
          ),
        ),
  );
}

class CompanyUsersScreen extends ConsumerWidget {
  const CompanyUsersScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(
      title: const Text('مستخدمو الشركة'),
      actions: [
        IconButton(
          onPressed: () => inviteUser(context, ref),
          icon: const Icon(Icons.mail_outline),
        ),
      ],
    ),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () => editUser(context, ref),
      icon: const Icon(Icons.person_add_alt),
      label: const Text('إضافة مستخدم'),
    ),
    body: ref
        .watch(accountUsersProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountUsersProvider)),
          data: (users) => ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
            children: users
                .map(
                  (u) => Card(
                    child: ListTile(
                      onTap: () => editUser(context, ref, u),
                      leading: CircleAvatar(
                        backgroundColor: u.active
                            ? AppColors.primaryTint
                            : AppColors.gray200,
                        child: Text(
                          initials(u.name),
                          style: TextStyle(
                            color: u.active
                                ? AppColors.primary
                                : AppColors.gray500,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      title: Text(
                        u.name,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      subtitle: Text(
                        '${u.jobTitle ?? u.roles.map((r) => r.name).join('، ')}\n${u.email ?? u.phone}',
                      ),
                      isThreeLine: true,
                      trailing: StatusChip(u.active ? 'Active' : 'Inactive'),
                    ),
                  ),
                )
                .toList(),
          ),
        ),
  );
}

class RolesPermissionsScreen extends ConsumerWidget {
  const RolesPermissionsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('مصفوفة صلاحيات الشركة')),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () => editRole(context, ref),
      icon: const Icon(Icons.add_moderator_outlined),
      label: const Text('إنشاء دور'),
    ),
    body: ref
        .watch(accountRolesProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountRolesProvider)),
          data: (roles) => ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
            children: [
              const InfoBanner(
                'الصلاحيات تحدد ما يستطيع كل مستخدم عرضه أو تنفيذه داخل حساب الشركة.',
              ),
              const SizedBox(height: 10),
              ...roles.map(
                (r) => Card(
                  child: ExpansionTile(
                    leading: Icon(
                      r.system
                          ? Icons.verified_user_outlined
                          : Icons.admin_panel_settings_outlined,
                      color: AppColors.primary,
                    ),
                    title: Text(
                      r.name,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    subtitle: Text(
                      '${r.users} مستخدم • ${r.permissions.length} صلاحية',
                    ),
                    trailing: r.system
                        ? null
                        : IconButton(
                            onPressed: () => editRole(context, ref, r),
                            icon: const Icon(Icons.edit_outlined),
                          ),
                    children: [
                      Padding(
                        padding: const EdgeInsets.fromLTRB(16, 0, 16, 14),
                        child: Wrap(
                          spacing: 6,
                          runSpacing: 5,
                          children: r.permissions
                              .map(
                                (p) => Chip(
                                  label: Text(
                                    permissionLabel(p),
                                    style: const TextStyle(fontSize: 9.5),
                                  ),
                                ),
                              )
                              .toList(),
                        ),
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

class ApprovalSettingsScreen extends ConsumerWidget {
  const ApprovalSettingsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('إدارة مستويات الموافقة')),
    body: ref
        .watch(accountPoliciesProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountPoliciesProvider)),
          data: (items) => ListView(
            padding: const EdgeInsets.all(16),
            children: items
                .map(
                  (p) => Card(
                    child: Padding(
                      padding: const EdgeInsets.all(15),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  p.name,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.w700,
                                    fontSize: 15,
                                  ),
                                ),
                              ),
                              StatusChip(p.active ? 'Active' : 'Inactive'),
                            ],
                          ),
                          Text(
                            'تبدأ من ${money(p.minimum)} ج.م',
                            style: const TextStyle(color: AppColors.gray500),
                          ),
                          const Divider(height: 22),
                          ...p.levels.map(
                            (l) => ListTile(
                              contentPadding: EdgeInsets.zero,
                              leading: CircleAvatar(
                                radius: 15,
                                child: Text('${l.sequence}'),
                              ),
                              title: Text(
                                l.name,
                                style: const TextStyle(
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              subtitle: Text(
                                '${l.approvers.length} مسؤول • مهلة ${l.sla} ساعة',
                              ),
                              trailing: Text(
                                l.limit == null
                                    ? 'بدون حد'
                                    : '${money(l.limit!)} ج.م',
                                style: const TextStyle(
                                  fontSize: 10.5,
                                  color: AppColors.primary,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                            ),
                          ),
                          OutlinedButton.icon(
                            onPressed: () => editApproval(context, ref, p),
                            icon: const Icon(Icons.tune),
                            label: const Text('ضبط المستويات'),
                          ),
                        ],
                      ),
                    ),
                  ),
                )
                .toList(),
          ),
        ),
  );
}

class CostCentersSettingsScreen extends ConsumerWidget {
  const CostCentersSettingsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('إدارة مراكز التكلفة')),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () => editCenter(context, ref),
      icon: const Icon(Icons.add),
      label: const Text('مركز جديد'),
    ),
    body: ref
        .watch(accountCentersProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountCentersProvider)),
          data: (items) => ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
            children: items.map((c) {
              final utilization = c.budget <= 0
                  ? 0.0
                  : (c.used + c.reserved) / c.budget;
              return Card(
                child: InkWell(
                  onTap: () => editCenter(context, ref, c),
                  child: Padding(
                    padding: const EdgeInsets.all(15),
                    child: Column(
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    c.name,
                                    style: const TextStyle(
                                      fontWeight: FontWeight.w700,
                                    ),
                                  ),
                                  Text(
                                    c.code,
                                    style: const TextStyle(
                                      color: AppColors.gray500,
                                      fontSize: 10.5,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Text(
                              '${money(c.budget)} ج.م',
                              style: const TextStyle(
                                fontWeight: FontWeight.w700,
                                color: AppColors.primary,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 10),
                        LinearProgressIndicator(
                          value: utilization.clamp(0, 1),
                          minHeight: 7,
                          borderRadius: BorderRadius.circular(7),
                          color: utilization >= 1
                              ? AppColors.error
                              : utilization >= .8
                              ? AppColors.warning
                              : AppColors.success,
                        ),
                        const SizedBox(height: 5),
                        Row(
                          children: [
                            Text(
                              '${(utilization * 100).toStringAsFixed(0)}% مستخدم ومحجوز',
                              style: const TextStyle(fontSize: 9.5),
                            ),
                            const Spacer(),
                            Text(
                              'حد الموافقة ${c.threshold == null ? '—' : money(c.threshold!)}',
                              style: const TextStyle(fontSize: 9.5),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              );
            }).toList(),
          ),
        ),
  );
}

class AccountAuditScreen extends ConsumerWidget {
  const AccountAuditScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('سجل نشاط المستخدمين')),
    body: ref
        .watch(accountAuditProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountAuditProvider)),
          data: (items) => items.isEmpty
              ? const EmptyView('لا يوجد نشاط مسجل', Icons.history_toggle_off)
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: items.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 8),
                  itemBuilder: (_, i) {
                    final a = items[i];
                    return Card(
                      child: ListTile(
                        leading: const CircleAvatar(
                          backgroundColor: AppColors.primaryTint,
                          child: Icon(Icons.history, color: AppColors.primary),
                        ),
                        title: Text(
                          actionLabel(a.action),
                          style: const TextStyle(fontWeight: FontWeight.w700),
                        ),
                        subtitle: Text(
                          '${a.userName ?? 'النظام'} • ${DateFormat('d MMM yyyy، h:mm a', 'ar').format(a.at.toLocal())}',
                        ),
                        trailing: Text(
                          a.entity,
                          style: const TextStyle(
                            fontSize: 9.5,
                            color: AppColors.gray500,
                          ),
                        ),
                      ),
                    );
                  },
                ),
        ),
  );
}

class BrandSettingsScreen extends ConsumerWidget {
  const BrandSettingsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('الهوية البصرية للشركة')),
    body: ref
        .watch(accountBrandProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountBrandProvider)),
          data: (b) => BrandForm(b),
        ),
  );
}

class BrandForm extends ConsumerStatefulWidget {
  const BrandForm(this.brand, {super.key});
  final BrandProfileModel brand;
  @override
  ConsumerState<BrandForm> createState() => _BrandFormState();
}

class _BrandFormState extends ConsumerState<BrandForm> {
  late final TextEditingController ar = TextEditingController(
        text: widget.brand.nameAr,
      ),
      en = TextEditingController(text: widget.brand.nameEn),
      primary = TextEditingController(text: widget.brand.primary),
      secondary = TextEditingController(text: widget.brand.secondary);
  @override
  void dispose() {
    ar.dispose();
    en.dispose();
    primary.dispose();
    secondary.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      Container(
        height: 150,
        decoration: BoxDecoration(
          gradient: LinearGradient(
            colors: [parseColor(primary.text), parseColor(secondary.text)],
          ),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Center(
          child: CircleAvatar(
            radius: 43,
            backgroundColor: Colors.white,
            child: widget.brand.logo == null
                ? const Icon(Icons.business, color: AppColors.primary, size: 36)
                : const Icon(Icons.image, color: AppColors.primary),
          ),
        ),
      ),
      const SizedBox(height: 14),
      OutlinedButton.icon(
        onPressed: () => toast(
          context,
          'يمكن اختيار الشعار من معرض الملفات عند تشغيل التطبيق على الجهاز',
        ),
        icon: const Icon(Icons.upload_outlined),
        label: const Text('رفع شعار الشركة'),
      ),
      Field(ar, 'اسم العلامة بالعربية', icon: Icons.translate),
      Field(en, 'اسم العلامة بالإنجليزية', icon: Icons.language),
      Field(primary, 'اللون الأساسي Hex', icon: Icons.color_lens_outlined),
      Field(secondary, 'اللون الثانوي Hex', icon: Icons.color_lens_outlined),
      FilledButton(
        onPressed: () async {
          try {
            await ref.read(accountRepositoryProvider).updateBrand({
              'primaryColor': primary.text,
              'secondaryColor': secondary.text,
              'brandNameAr': ar.text,
              'brandNameEn': en.text,
            });
            ref.invalidate(accountBrandProvider);
            if (context.mounted) toast(context, 'تم تحديث الهوية البصرية');
          } catch (e) {
            if (context.mounted) toast(context, '$e');
          }
        },
        child: const Text('حفظ الهوية'),
      ),
    ],
  );
}

class BillingSettingsScreen extends ConsumerWidget {
  const BillingSettingsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('بيانات الفوترة والضرائب')),
    body: ref
        .watch(accountBillingProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountBillingProvider)),
          data: (b) => BillingForm(b),
        ),
  );
}

class BillingForm extends ConsumerStatefulWidget {
  const BillingForm(this.billing, {super.key});
  final BillingProfileModel billing;
  @override
  ConsumerState<BillingForm> createState() => _BillingFormState();
}

class _BillingFormState extends ConsumerState<BillingForm> {
  late final TextEditingController name = TextEditingController(
        text: widget.billing.name,
      ),
      email = TextEditingController(text: widget.billing.email),
      tax = TextEditingController(text: widget.billing.taxNo),
      address = TextEditingController(text: widget.billing.address),
      days = TextEditingController(text: '${widget.billing.days}');
  late bool po = widget.billing.poRequired;
  @override
  void dispose() {
    name.dispose();
    email.dispose();
    tax.dispose();
    address.dispose();
    days.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      const InfoBanner(
        'ستظهر هذه البيانات تلقائيًا في الفواتير الإلكترونية وكشوف الحساب.',
      ),
      const SizedBox(height: 12),
      Field(name, 'الاسم القانوني بالفاتورة', icon: Icons.business_outlined),
      Field(email, 'بريد الفوترة', icon: Icons.email_outlined),
      Field(tax, 'رقم التسجيل الضريبي', icon: Icons.numbers),
      Field(
        address,
        'العنوان الضريبي',
        icon: Icons.location_on_outlined,
        lines: 3,
      ),
      Field(
        days,
        'مهلة السداد بالأيام',
        icon: Icons.calendar_today_outlined,
        type: TextInputType.number,
      ),
      SwitchListTile(
        contentPadding: EdgeInsets.zero,
        value: po,
        onChanged: (v) => setState(() => po = v),
        title: const Text('اشتراط أمر شراء'),
        subtitle: const Text('لن يكتمل الطلب الآجل بدون رقم أمر شراء'),
      ),
      const SizedBox(height: 14),
      FilledButton(
        onPressed: () async {
          try {
            await ref.read(accountRepositoryProvider).updateBilling({
              'invoiceLegalName': name.text,
              'billingEmail': email.text,
              'taxRegistrationNo': tax.text,
              'taxAddress': address.text,
              'paymentTermsDays': int.tryParse(days.text) ?? 30,
              'purchaseOrderRequired': po,
            });
            ref.invalidate(accountBillingProvider);
            if (context.mounted) toast(context, 'تم حفظ بيانات الفوترة');
          } catch (e) {
            if (context.mounted) toast(context, '$e');
          }
        },
        child: const Text('حفظ البيانات'),
      ),
    ],
  );
}

class CompanyContractsScreen extends ConsumerWidget {
  const CompanyContractsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    backgroundColor: AppColors.background,
    appBar: AppBar(title: const Text('العقود وشروط الدفع')),
    body: ref
        .watch(accountContractsProvider)
        .when(
          loading: loader,
          error: (e, _) =>
              ErrorView(e, () => ref.invalidate(accountContractsProvider)),
          data: (items) => items.isEmpty
              ? const EmptyView(
                  'لا يوجد عقد نشط حاليًا',
                  Icons.description_outlined,
                )
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: items
                      .map(
                        (c) => ContractCard(
                          c,
                          () => renewContract(context, ref, c),
                        ),
                      )
                      .toList(),
                ),
        ),
  );
}

class ContractCard extends StatelessWidget {
  const ContractCard(this.contract, this.renew, {super.key});
  final CompanyContractModel contract;
  final VoidCallback renew;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              const CircleAvatar(
                backgroundColor: AppColors.primaryTint,
                child: Icon(
                  Icons.description_outlined,
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      contract.number,
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    Text(
                      '${DateFormat('d MMM yyyy', 'ar').format(contract.start)} — ${DateFormat('d MMM yyyy', 'ar').format(contract.end)}',
                      style: const TextStyle(
                        fontSize: 10.5,
                        color: AppColors.gray500,
                      ),
                    ),
                  ],
                ),
              ),
              StatusChip(contract.status),
            ],
          ),
          if (contract.remaining <= 30) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: AppColors.warningTint,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                'ينتهي العقد خلال ${contract.remaining} يومًا',
                style: const TextStyle(
                  color: AppColors.warning,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
          const Divider(height: 24),
          InfoRow('حد الائتمان', '${money(contract.credit)} ج.م'),
          InfoRow('مهلة السداد', '${contract.days} يومًا'),
          InfoRow(
            'التجديد التلقائي',
            contract.autoRenew ? 'مفعّل' : 'غير مفعّل',
          ),
          if (contract.summary != null)
            Padding(
              padding: const EdgeInsets.only(top: 10),
              child: Text(
                contract.summary!,
                style: const TextStyle(fontSize: 11, color: AppColors.gray600),
              ),
            ),
          const SizedBox(height: 14),
          FilledButton(
            onPressed: renew,
            style: FilledButton.styleFrom(
              backgroundColor: contract.remaining <= 30
                  ? AppColors.warning
                  : AppColors.primary,
            ),
            child: const Text('طلب تجديد العقد'),
          ),
        ],
      ),
    ),
  );
}

Future<void> editCompany(
  BuildContext context,
  WidgetRef ref,
  AccountCompanyModel c,
) async {
  final name = TextEditingController(text: c.name),
      en = TextEditingController(text: c.nameEn),
      phone = TextEditingController(text: c.phone),
      email = TextEditingController(text: c.email),
      gov = TextEditingController(text: c.governorate),
      city = TextEditingController(text: c.city),
      address = TextEditingController(text: c.address),
      industry = TextEditingController(text: c.industry),
      employees = TextEditingController(text: '${c.employees}');
  await formSheet(
    context,
    'تعديل بيانات الشركة',
    [
      Field(name, 'الاسم القانوني'),
      Field(en, 'الاسم بالإنجليزية'),
      Field(phone, 'الهاتف'),
      Field(email, 'البريد الإلكتروني'),
      Field(industry, 'النشاط'),
      Field(gov, 'المحافظة'),
      Field(city, 'المدينة'),
      Field(address, 'العنوان', lines: 2),
      Field(employees, 'نطاق عدد الموظفين', type: TextInputType.number),
    ],
    () async {
      await ref.read(accountRepositoryProvider).updateCompany({
        'legalName': name.text,
        'legalNameEn': en.text,
        'phone': phone.text,
        'email': email.text,
        'governorate': gov.text,
        'city': city.text,
        'addressLine': address.text,
        'industry': industry.text,
        'employeeCountRange': int.tryParse(employees.text) ?? 0,
      });
      ref.invalidate(accountOverviewProvider);
    },
  );
  for (final c in [
    name,
    en,
    phone,
    email,
    gov,
    city,
    address,
    industry,
    employees,
  ]) {
    c.dispose();
  }
}

Future<void> editBranch(
  BuildContext context,
  WidgetRef ref, [
  AccountBranchModel? b,
]) async {
  final name = TextEditingController(text: b?.name),
      gov = TextEditingController(text: b?.governorate),
      city = TextEditingController(text: b?.city),
      address = TextEditingController(text: b?.address),
      phone = TextEditingController(text: b?.phone);
  var main = b?.main ?? false;
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
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                b == null ? 'إضافة فرع' : 'تعديل الفرع',
                style: const TextStyle(
                  fontSize: 19,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 12),
              Field(name, 'اسم الفرع'),
              Field(gov, 'المحافظة'),
              Field(city, 'المدينة'),
              Field(address, 'العنوان', lines: 2),
              Field(phone, 'رقم التواصل'),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                value: main,
                onChanged: (v) => set(() => main = v),
                title: const Text('الفرع الرئيسي'),
              ),
              FilledButton(
                onPressed: () async {
                  try {
                    await ref
                        .read(accountRepositoryProvider)
                        .saveBranch(
                          AccountBranchModel(
                            id: b?.id ?? '',
                            name: name.text,
                            governorate: gov.text,
                            city: city.text,
                            address: address.text,
                            phone: phone.text,
                            main: main,
                          ),
                          create: b == null,
                        );
                    ref.invalidate(accountBranchesProvider);
                    ref.invalidate(accountOverviewProvider);
                    if (sheet.mounted) Navigator.pop(sheet);
                  } catch (e) {
                    if (sheet.mounted) toast(sheet, '$e');
                  }
                },
                child: const Text('حفظ الفرع'),
              ),
              if (b != null && !b.main)
                TextButton(
                  onPressed: () async {
                    await ref
                        .read(accountRepositoryProvider)
                        .deleteBranch(b.id);
                    ref.invalidate(accountBranchesProvider);
                    if (sheet.mounted) Navigator.pop(sheet);
                  },
                  style: TextButton.styleFrom(foregroundColor: AppColors.error),
                  child: const Text('حذف الفرع'),
                ),
            ],
          ),
        ),
      ),
    ),
  );
  for (final c in [name, gov, city, address, phone]) {
    c.dispose();
  }
}

Future<void> editUser(
  BuildContext context,
  WidgetRef ref, [
  AccountUserModel? u,
]) async {
  final roles = await ref.read(accountRolesProvider.future),
      branches = await ref.read(accountBranchesProvider.future);
  if (!context.mounted) return;
  final name = TextEditingController(text: u?.name),
      phone = TextEditingController(text: u?.phone),
      email = TextEditingController(text: u?.email),
      password = TextEditingController(),
      title = TextEditingController(text: u?.jobTitle),
      department = TextEditingController(text: u?.department),
      limit = TextEditingController(text: u?.limit?.toStringAsFixed(0));
  var roleIds = u?.roles.map((r) => r.id).toSet() ?? <String>{};
  String? branch = u?.branchId;
  var active = u?.active ?? true;
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
          MediaQuery.viewInsetsOf(sheet).bottom + 20,
        ),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                u == null ? 'إضافة مستخدم' : 'تعديل المستخدم',
                style: const TextStyle(
                  fontSize: 19,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 10),
              Field(name, 'الاسم'),
              if (u == null) Field(phone, 'رقم الهاتف'),
              Field(email, 'البريد الإلكتروني'),
              if (u == null)
                Field(password, 'كلمة المرور المؤقتة', obscure: true),
              Field(title, 'المسمى الوظيفي'),
              Field(department, 'الإدارة'),
              Field(limit, 'الحد النقدي للطلب', type: TextInputType.number),
              DropdownButtonFormField<String?>(
                value: branch,
                decoration: const InputDecoration(labelText: 'الفرع'),
                items: [
                  const DropdownMenuItem(value: null, child: Text('بدون فرع')),
                  ...branches.map(
                    (b) => DropdownMenuItem(value: b.id, child: Text(b.name)),
                  ),
                ],
                onChanged: (v) => set(() => branch = v),
              ),
              const SizedBox(height: 10),
              const Text(
                'الأدوار',
                style: TextStyle(fontWeight: FontWeight.w700),
              ),
              ...roles.map(
                (r) => CheckboxListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  value: roleIds.contains(r.id),
                  onChanged: (v) => set(
                    () => v == true ? roleIds.add(r.id) : roleIds.remove(r.id),
                  ),
                  title: Text(r.name),
                ),
              ),
              if (u != null)
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  value: active,
                  onChanged: (v) => set(() => active = v),
                  title: const Text('الحساب نشط'),
                ),
              FilledButton(
                onPressed: () async {
                  try {
                    final data = {
                      'fullName': name.text,
                      if (u == null) 'phone': phone.text,
                      'email': email.text,
                      if (u == null) 'password': password.text,
                      'jobTitle': title.text,
                      'department': department.text,
                      'purchaseLimit': double.tryParse(limit.text),
                      'defaultBranchId': branch,
                      'roleIds': roleIds.toList(),
                      if (u != null) 'isActive': active,
                    };
                    if (u == null) {
                      await ref
                          .read(accountRepositoryProvider)
                          .createUser(data);
                    } else {
                      await ref
                          .read(accountRepositoryProvider)
                          .updateUser(u.id, data);
                    }
                    ref.invalidate(accountUsersProvider);
                    ref.invalidate(accountOverviewProvider);
                    if (sheet.mounted) Navigator.pop(sheet);
                  } catch (e) {
                    if (sheet.mounted) toast(sheet, '$e');
                  }
                },
                child: const Text('حفظ المستخدم'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
  for (final c in [name, phone, email, password, title, department, limit]) {
    c.dispose();
  }
}

Future<void> inviteUser(BuildContext context, WidgetRef ref) async {
  final roles = await ref.read(accountRolesProvider.future),
      branches = await ref.read(accountBranchesProvider.future);
  if (!context.mounted) return;
  final name = TextEditingController(),
      email = TextEditingController(),
      phone = TextEditingController();
  String? role = roles.firstOrNull?.id, branch;
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
              'دعوة مستخدم',
              style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 12),
            Field(name, 'الاسم'),
            Field(email, 'البريد الإلكتروني'),
            Field(phone, 'رقم الهاتف (اختياري)'),
            DropdownButtonFormField(
              value: role,
              decoration: const InputDecoration(labelText: 'الدور'),
              items: roles
                  .map(
                    (r) => DropdownMenuItem(value: r.id, child: Text(r.name)),
                  )
                  .toList(),
              onChanged: (v) => set(() => role = v),
            ),
            const SizedBox(height: 9),
            DropdownButtonFormField<String?>(
              value: branch,
              decoration: const InputDecoration(labelText: 'الفرع'),
              items: [
                const DropdownMenuItem(value: null, child: Text('بدون فرع')),
                ...branches.map(
                  (b) => DropdownMenuItem(value: b.id, child: Text(b.name)),
                ),
              ],
              onChanged: (v) => set(() => branch = v),
            ),
            const SizedBox(height: 14),
            FilledButton(
              onPressed: role == null
                  ? null
                  : () async {
                      try {
                        final result = await ref
                            .read(accountRepositoryProvider)
                            .invite({
                              'fullName': name.text,
                              'email': email.text,
                              'phone': phone.text,
                              'roleId': role,
                              'branchId': branch,
                            });
                        ref.invalidate(accountOverviewProvider);
                        if (sheet.mounted) {
                          Navigator.pop(sheet);
                          toast(
                            context,
                            'تم إنشاء الدعوة حتى ${DateFormat('d MMM', 'ar').format(DateTime.parse(result['expiresAt'] as String))}',
                          );
                        }
                      } catch (e) {
                        if (sheet.mounted) toast(sheet, '$e');
                      }
                    },
              child: const Text('إرسال الدعوة'),
            ),
          ],
        ),
      ),
    ),
  );
  for (final c in [name, email, phone]) {
    c.dispose();
  }
}

Future<void> editRole(
  BuildContext context,
  WidgetRef ref, [
  AccountRoleModel? role,
]) async {
  final permissions = await ref.read(accountPermissionsProvider.future);
  if (!context.mounted) return;
  final ar = TextEditingController(text: role?.name),
      en = TextEditingController(text: role?.nameEn);
  var selected = permissions
      .where((p) => role?.permissions.contains(p.code) ?? false)
      .map((p) => p.id)
      .toSet();
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (_, set) => SizedBox(
        height: MediaQuery.sizeOf(sheet).height * .88,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(18, 0, 18, 20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                role == null ? 'إنشاء دور' : 'تعيين الصلاحيات',
                style: const TextStyle(
                  fontSize: 19,
                  fontWeight: FontWeight.w700,
                ),
              ),
              if (role == null) ...[
                Field(ar, 'اسم الدور بالعربية'),
                Field(en, 'اسم الدور بالإنجليزية'),
              ],
              const SizedBox(height: 8),
              Expanded(
                child: ListView(
                  children: permissions
                      .map(
                        (p) => CheckboxListTile(
                          dense: true,
                          contentPadding: EdgeInsets.zero,
                          value: selected.contains(p.id),
                          onChanged: (v) => set(
                            () => v == true
                                ? selected.add(p.id)
                                : selected.remove(p.id),
                          ),
                          title: Text(
                            p.description,
                            style: const TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                          subtitle: Text(
                            '${p.module} • ${p.code}',
                            style: const TextStyle(fontSize: 9.5),
                          ),
                        ),
                      )
                      .toList(),
                ),
              ),
              FilledButton(
                onPressed: () async {
                  try {
                    if (role == null) {
                      await ref
                          .read(accountRepositoryProvider)
                          .createRole(ar.text, en.text, selected.toList());
                    } else {
                      await ref
                          .read(accountRepositoryProvider)
                          .updateRole(role.id, selected.toList());
                    }
                    ref.invalidate(accountRolesProvider);
                    if (sheet.mounted) Navigator.pop(sheet);
                  } catch (e) {
                    if (sheet.mounted) toast(sheet, '$e');
                  }
                },
                child: const Text('حفظ الصلاحيات'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
  ar.dispose();
  en.dispose();
}

Future<void> editApproval(
  BuildContext context,
  WidgetRef ref,
  ApprovalPolicyAccountModel p,
) async {
  final users = await ref.read(accountUsersProvider.future);
  if (!context.mounted) return;
  var levels = p.levels
      .map(
        (l) => ApprovalLevelAccountModel(
          l.id,
          l.sequence,
          l.name,
          l.limit,
          l.sla,
          [...l.approvers],
        ),
      )
      .toList();
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (_, set) => SizedBox(
        height: MediaQuery.sizeOf(sheet).height * .82,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(18, 0, 18, 18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'مستويات الموافقة',
                style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
              ),
              const Text(
                'اختر المسؤول والحد المالي لكل مستوى',
                style: TextStyle(color: AppColors.gray500),
              ),
              const SizedBox(height: 10),
              Expanded(
                child: ListView(
                  children: levels.asMap().entries.map((entry) {
                    final l = entry.value;
                    return Card(
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Column(
                          children: [
                            Text(
                              '${l.sequence}. ${l.name}',
                              style: const TextStyle(
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            DropdownButtonFormField<String>(
                              value: l.approvers.firstOrNull,
                              decoration: const InputDecoration(
                                labelText: 'مسؤول الموافقة',
                              ),
                              items: users
                                  .where((u) => u.active)
                                  .map(
                                    (u) => DropdownMenuItem(
                                      value: u.id,
                                      child: Text(u.name),
                                    ),
                                  )
                                  .toList(),
                              onChanged: (v) => set(
                                () => levels[entry.key] =
                                    ApprovalLevelAccountModel(
                                      l.id,
                                      l.sequence,
                                      l.name,
                                      l.limit,
                                      l.sla,
                                      v == null ? [] : [v],
                                    ),
                              ),
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                Expanded(
                                  child: TextFormField(
                                    initialValue: l.limit?.toStringAsFixed(0),
                                    keyboardType: TextInputType.number,
                                    decoration: const InputDecoration(
                                      labelText: 'حد الصلاحية',
                                    ),
                                    onChanged: (v) => levels[entry.key] =
                                        ApprovalLevelAccountModel(
                                          l.id,
                                          l.sequence,
                                          l.name,
                                          double.tryParse(v),
                                          l.sla,
                                          l.approvers,
                                        ),
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: TextFormField(
                                    initialValue: '${l.sla}',
                                    keyboardType: TextInputType.number,
                                    decoration: const InputDecoration(
                                      labelText: 'المهلة/ساعة',
                                    ),
                                    onChanged: (v) => levels[entry.key] =
                                        ApprovalLevelAccountModel(
                                          l.id,
                                          l.sequence,
                                          l.name,
                                          l.limit,
                                          int.tryParse(v) ?? l.sla,
                                          l.approvers,
                                        ),
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),
                    );
                  }).toList(),
                ),
              ),
              FilledButton(
                onPressed: () async {
                  try {
                    await ref
                        .read(accountRepositoryProvider)
                        .updatePolicy(p, levels);
                    ref.invalidate(accountPoliciesProvider);
                    if (sheet.mounted) Navigator.pop(sheet);
                  } catch (e) {
                    if (sheet.mounted) toast(sheet, '$e');
                  }
                },
                child: const Text('حفظ المستويات'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

Future<void> editCenter(
  BuildContext context,
  WidgetRef ref, [
  AccountCostCenterModel? c,
]) async {
  final code = TextEditingController(text: c?.code),
      name = TextEditingController(text: c?.name),
      budget = TextEditingController(text: c?.budget.toStringAsFixed(0)),
      threshold = TextEditingController(text: c?.threshold?.toStringAsFixed(0));
  var active = c?.active ?? true;
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
            Text(
              c == null ? 'إضافة مركز تكلفة' : 'تعديل مركز التكلفة',
              style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 12),
            Field(code, 'الكود'),
            Field(name, 'اسم المركز'),
            Field(budget, 'الميزانية', type: TextInputType.number),
            Field(threshold, 'حد طلب الموافقة', type: TextInputType.number),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              value: active,
              onChanged: (v) => set(() => active = v),
              title: const Text('المركز نشط'),
            ),
            FilledButton(
              onPressed: () async {
                try {
                  await ref.read(accountRepositoryProvider).saveCenter({
                    'code': code.text,
                    'name': name.text,
                    'budget': double.tryParse(budget.text) ?? 0,
                    'approvalThreshold': double.tryParse(threshold.text),
                    'isActive': active,
                  }, id: c?.id);
                  ref.invalidate(accountCentersProvider);
                  if (sheet.mounted) Navigator.pop(sheet);
                } catch (e) {
                  if (sheet.mounted) toast(sheet, '$e');
                }
              },
              child: const Text('حفظ المركز'),
            ),
          ],
        ),
      ),
    ),
  );
  for (final x in [code, name, budget, threshold]) {
    x.dispose();
  }
}

Future<void> renewContract(
  BuildContext context,
  WidgetRef ref,
  CompanyContractModel c,
) async {
  final months = TextEditingController(text: '12'),
      note = TextEditingController();
  await formSheet(
    context,
    'طلب تجديد العقد',
    [
      InfoBanner('العقد ${c.number} ينتهي خلال ${c.remaining} يومًا.'),
      Field(months, 'مدة التجديد بالشهور', type: TextInputType.number),
      Field(note, 'ملاحظات الطلب', lines: 3),
    ],
    () async {
      await ref
          .read(accountRepositoryProvider)
          .renew(c.id, int.tryParse(months.text) ?? 12, note.text);
      if (context.mounted) toast(context, 'تم إرسال طلب التجديد للمراجعة');
    },
  );
  months.dispose();
  note.dispose();
}

Future<void> formSheet(
  BuildContext context,
  String title,
  List<Widget> fields,
  Future<void> Function() save,
) async => showModalBottomSheet(
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
    child: SingleChildScrollView(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 12),
          ...fields,
          const SizedBox(height: 10),
          FilledButton(
            onPressed: () async {
              try {
                await save();
                if (sheet.mounted) Navigator.pop(sheet);
              } catch (e) {
                if (sheet.mounted) toast(sheet, '$e');
              }
            },
            child: const Text('حفظ التعديلات'),
          ),
        ],
      ),
    ),
  ),
);

class AccountGroup extends StatelessWidget {
  const AccountGroup({super.key, required this.title, required this.items});
  final String title;
  final List<AccountItem> items;
  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: Padding(
      padding: const EdgeInsets.fromLTRB(14, 15, 14, 7),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            title,
            style: const TextStyle(
              color: AppColors.gray900,
              fontSize: 12.5,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 7),
          ...items.map(
            (i) => ListTile(
              contentPadding: EdgeInsets.zero,
              minVerticalPadding: 7,
              onTap: () => context.push(i.route),
              leading: Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: AppColors.primaryTint,
                  borderRadius: BorderRadius.circular(13),
                ),
                child: Icon(i.icon, size: 20, color: AppColors.primary),
              ),
              title: Text(
                i.label,
                style: const TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 11,
                ),
              ),
              trailing: const Icon(
                Icons.arrow_back_ios_new_rounded,
                color: AppColors.gray400,
                size: 14,
              ),
            ),
          ),
        ],
      ),
    ),
  );
}

class AccountItem {
  const AccountItem(this.label, this.icon, this.route);
  final String label, route;
  final IconData icon;
}

class MetricBox extends StatelessWidget {
  const MetricBox(this.value, this.label, this.icon, {super.key});
  final String value, label;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Expanded(
    child: Container(
      padding: const EdgeInsets.symmetric(vertical: 13, horizontal: 4),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(17),
        border: Border.all(color: AppColors.gray150),
      ),
      child: Padding(
        padding: EdgeInsets.zero,
        child: Column(
          children: [
            Icon(icon, color: AppColors.primary, size: 20),
            const SizedBox(height: 5),
            Text(
              value,
              style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 17),
            ),
            Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: AppColors.gray500, fontSize: 9.5),
            ),
          ],
        ),
      ),
    ),
  );
}

class CreditCard extends StatelessWidget {
  const CreditCard({super.key, required this.limit, required this.used});
  final double limit, used;
  @override
  Widget build(BuildContext context) {
    final p = limit <= 0 ? 0.0 : used / limit;
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(AppRadius.xl),
        border: Border.all(color: AppColors.gray150),
        boxShadow: AppShadows.soft,
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          children: [
            Row(
              children: [
                const Icon(
                  Icons.credit_score_outlined,
                  color: AppColors.primary,
                ),
                const SizedBox(width: 8),
                const Expanded(
                  child: Text(
                    'الحد الائتماني',
                    style: TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
                Text(
                  '${money(limit - used)} ج.م متاح',
                  style: const TextStyle(
                    color: AppColors.success,
                    fontSize: 10.5,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            LinearProgressIndicator(
              value: p.clamp(0, 1),
              minHeight: 7,
              borderRadius: BorderRadius.circular(7),
            ),
            const SizedBox(height: 4),
            Row(
              children: [
                Text(
                  'مستخدم ${money(used)}',
                  style: const TextStyle(fontSize: 9.5),
                ),
                const Spacer(),
                Text(
                  'الإجمالي ${money(limit)}',
                  style: const TextStyle(fontSize: 9.5),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class DetailCard extends StatelessWidget {
  const DetailCard({
    super.key,
    required this.title,
    required this.icon,
    required this.rows,
  });
  final String title;
  final IconData icon;
  final Map<String, String> rows;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          CircleAvatar(
            radius: 30,
            backgroundColor: AppColors.primaryTint,
            child: Icon(icon, size: 29, color: AppColors.primary),
          ),
          const SizedBox(height: 8),
          Text(
            title,
            style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 17),
          ),
          const Divider(height: 25),
          ...rows.entries.map((e) => InfoRow(e.key, e.value)),
        ],
      ),
    ),
  );
}

class InfoRow extends StatelessWidget {
  const InfoRow(this.label, this.value, {super.key});
  final String label, value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 5),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 120,
          child: Text(
            label,
            style: const TextStyle(fontSize: 10.5, color: AppColors.gray500),
          ),
        ),
        Expanded(
          child: Text(
            value.isEmpty ? '—' : value,
            textAlign: TextAlign.end,
            style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
          ),
        ),
      ],
    ),
  );
}

class StatusChip extends StatelessWidget {
  const StatusChip(this.status, {super.key});
  final String status;
  @override
  Widget build(BuildContext context) {
    final good = ['Active', 'Approved', 'Main'].contains(status),
        bad = ['Rejected', 'Inactive', 'Expired', 'Suspended'].contains(status);
    final color = good
        ? AppColors.success
        : bad
        ? AppColors.error
        : AppColors.warning;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        statusLabel(status),
        style: TextStyle(
          fontSize: 10.5,
          color: color,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class StatusBanner extends StatelessWidget {
  const StatusBanner(this.status, {super.key});
  final String status;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(
      color: AppColors.successTint,
      borderRadius: BorderRadius.circular(12),
      border: Border.all(color: AppColors.success.withValues(alpha: .25)),
    ),
    child: Row(
      children: [
        const Icon(Icons.verified_outlined, color: AppColors.success),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            'حساب الشركة ${statusLabel(status)}',
            style: const TextStyle(
              color: AppColors.success,
              fontWeight: FontWeight.w700,
            ),
          ),
        ),
      ],
    ),
  );
}

class InfoBanner extends StatelessWidget {
  const InfoBanner(this.text, {super.key});
  final String text;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(
      color: AppColors.infoTint,
      borderRadius: BorderRadius.circular(12),
    ),
    child: Row(
      children: [
        const Icon(Icons.info_outline, color: AppColors.info),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            text,
            style: const TextStyle(fontSize: 10.5, color: AppColors.info),
          ),
        ),
      ],
    ),
  );
}

class Field extends StatelessWidget {
  const Field(
    this.controller,
    this.label, {
    this.icon,
    this.lines = 1,
    this.type,
    this.obscure = false,
    super.key,
  });
  final TextEditingController controller;
  final String label;
  final IconData? icon;
  final int lines;
  final TextInputType? type;
  final bool obscure;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 10),
    child: TextField(
      controller: controller,
      maxLines: lines,
      keyboardType: type,
      obscureText: obscure,
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: icon == null ? null : Icon(icon),
      ),
    ),
  );
}

class ErrorView extends StatelessWidget {
  const ErrorView(this.error, this.retry, {super.key});
  final Object error;
  final VoidCallback retry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(
            Icons.cloud_off_outlined,
            size: 44,
            color: AppColors.gray400,
          ),
          const SizedBox(height: 10),
          Text('$error', textAlign: TextAlign.center),
          TextButton(onPressed: retry, child: const Text('إعادة المحاولة')),
        ],
      ),
    ),
  );
}

class EmptyView extends StatelessWidget {
  const EmptyView(this.text, this.icon, {super.key});
  final String text;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Center(
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 48, color: AppColors.gray400),
        const SizedBox(height: 10),
        Text(text, style: const TextStyle(color: AppColors.gray500)),
      ],
    ),
  );
}

Widget loader() => const Center(child: CircularProgressIndicator());
String initials(String name) => name
    .trim()
    .split(RegExp(r'\s+'))
    .take(2)
    .map((e) => e.isEmpty ? '' : e[0])
    .join();
String money(double v) => NumberFormat('#,##0.##', 'ar').format(v);
void toast(BuildContext context, String message) => ScaffoldMessenger.of(
  context,
).showSnackBar(SnackBar(content: Text(message)));
Color parseColor(String hex) {
  final clean = hex.replaceFirst('#', '');
  return Color(int.tryParse('FF$clean', radix: 16) ?? 0xFF023BAA);
}

String documentName(String type) =>
    {
      'CommercialRegistration': 'السجل التجاري',
      'TaxCard': 'البطاقة الضريبية',
      'AuthorizationLetter': 'خطاب التفويض',
    }[type] ??
    'مستند آخر';
String permissionLabel(String code) => code
    .split('.')
    .map(
      (e) =>
          {
            'view': 'عرض',
            'manage': 'إدارة',
            'create': 'إنشاء',
            'cancel': 'إلغاء',
            'act': 'اعتماد',
          }[e] ??
          e,
    )
    .join(' ');
String actionLabel(String action) =>
    {
      'company.user_created': 'إضافة مستخدم',
      'company.user_updated': 'تعديل مستخدم',
      'company.user_deactivated': 'تعطيل مستخدم',
      'company.user_invited': 'دعوة مستخدم',
      'company.role_created': 'إنشاء دور',
      'company.role_permissions_updated': 'تعديل صلاحيات دور',
      'company.approval_policy_updated': 'تعديل مستويات الموافقة',
      'company.branch_created': 'إضافة فرع',
      'company.branch_updated': 'تعديل فرع',
      'company.cost_center_updated': 'تعديل مركز تكلفة',
      'company.brand_updated': 'تعديل الهوية البصرية',
      'company.billing_updated': 'تعديل بيانات الفوترة',
      'company.contract_renewal_requested': 'طلب تجديد عقد',
    }[action] ??
    action;
String statusLabel(String status) =>
    {
      'Active': 'نشط',
      'Approved': 'معتمد',
      'Pending': 'قيد المراجعة',
      'UnderReview': 'تحت المراجعة',
      'Rejected': 'مرفوض',
      'Inactive': 'غير نشط',
      'Main': 'رئيسي',
      'Expiring': 'قارب على الانتهاء',
      'Expired': 'منتهي',
      'Suspended': 'موقوف',
      'PendingVerification': 'بانتظار التحقق',
    }[status] ??
    status;
