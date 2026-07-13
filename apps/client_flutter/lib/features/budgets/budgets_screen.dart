import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../core/api/budget_repository.dart';
import '../../core/theme/app_tokens.dart';

class BudgetsScreen extends ConsumerStatefulWidget {
  const BudgetsScreen({super.key});
  @override
  ConsumerState<BudgetsScreen> createState() => _BudgetsScreenState();
}

class _BudgetsScreenState extends ConsumerState<BudgetsScreen> {
  int year = DateTime.now().year;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('ميزانية الشركة'),
      actions: [
        PopupMenuButton<int>(
          initialValue: year,
          onSelected: (v) => setState(() => year = v),
          itemBuilder: (_) => List.generate(
            4,
            (i) => PopupMenuItem(
              value: DateTime.now().year - i,
              child: Text('${DateTime.now().year - i}'),
            ),
          ),
        ),
      ],
    ),
    body: ref
        .watch(budgetSummaryProvider(year))
        .when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Text('$e')),
          data: (s) => RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(budgetSummaryProvider(year));
              await ref.read(budgetSummaryProvider(year).future);
            },
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
              children: [
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [AppColors.primary, AppColors.primaryDark],
                    ),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          const Expanded(
                            child: Text(
                              'إجمالي الميزانية السنوية',
                              style: TextStyle(color: Colors.white70),
                            ),
                          ),
                          Text(
                            '$year',
                            style: const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        ],
                      ),
                      Text(
                        '${money(s.total)} ج.م',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 28,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                      const SizedBox(height: 12),
                      LinearProgressIndicator(
                        value: (s.utilization / 100).clamp(0, 1),
                        minHeight: 9,
                        borderRadius: BorderRadius.circular(8),
                        backgroundColor: Colors.white24,
                        color: s.utilization >= 100
                            ? Colors.redAccent
                            : s.utilization >= 80
                            ? Colors.orangeAccent
                            : Colors.greenAccent,
                      ),
                      const SizedBox(height: 7),
                      Row(
                        children: [
                          Text(
                            'مستخدم ومحجوز ${money(s.used + s.reserved)}',
                            style: const TextStyle(
                              color: Colors.white70,
                              fontSize: 9,
                            ),
                          ),
                          const Spacer(),
                          Text(
                            '${s.utilization.toStringAsFixed(1)}%',
                            style: const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.w900,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: Metric(
                        title: 'المتاح',
                        value: s.available,
                        color: AppColors.success,
                        icon: Icons.savings_outlined,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Metric(
                        title: 'المتوقع نهاية العام',
                        value: s.forecast,
                        color: s.forecast > s.total
                            ? AppColors.error
                            : AppColors.primary,
                        icon: Icons.trending_up,
                      ),
                    ),
                  ],
                ),
                if (s.alerts.isNotEmpty) ...[
                  const SizedBox(height: 12),
                  ...s.alerts.map((a) => AlertCard(alert: a)),
                ],
                const SizedBox(height: 12),
                Section(
                  title: 'الصرف الشهري',
                  icon: Icons.show_chart,
                  child: Bars(
                    points: s.months.take(DateTime.now().month).toList(),
                    color: AppColors.primary,
                  ),
                ),
                if (s.categories.isNotEmpty) ...[
                  const SizedBox(height: 12),
                  Section(
                    title: 'توزيع الميزانية حسب الأقسام',
                    icon: Icons.donut_large,
                    child: Breakdown(points: s.categories),
                  ),
                ],
                const SizedBox(height: 12),
                Section(
                  title: 'مراكز التكلفة',
                  icon: Icons.account_balance_wallet_outlined,
                  child: Column(
                    children: s.centers
                        .map((c) => CenterTile(center: c))
                        .toList(),
                  ),
                ),
              ],
            ),
          ),
        ),
  );
}

class BudgetCenterScreen extends ConsumerWidget {
  const BudgetCenterScreen({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    appBar: AppBar(title: const Text('تفاصيل مركز التكلفة')),
    body: ref
        .watch(budgetCenterProvider(id))
        .when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (e, _) => Center(child: Text('$e')),
          data: (d) => RefreshIndicator(
            onRefresh: () async {
              ref.invalidate(budgetCenterProvider(id));
              await ref.read(budgetCenterProvider(id).future);
            },
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
              children: [
                Container(
                  padding: const EdgeInsets.all(18),
                  decoration: BoxDecoration(
                    color: healthColor(d.center.health).withValues(alpha: .08),
                    border: Border.all(
                      color: healthColor(d.center.health).withValues(alpha: .3),
                    ),
                    borderRadius: BorderRadius.circular(18),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  d.center.name,
                                  style: const TextStyle(
                                    fontSize: 18,
                                    fontWeight: FontWeight.w900,
                                  ),
                                ),
                                Text(
                                  d.center.code,
                                  style: const TextStyle(
                                    color: AppColors.gray500,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Text(
                            '${d.center.utilization.toStringAsFixed(1)}%',
                            style: TextStyle(
                              fontSize: 23,
                              fontWeight: FontWeight.w900,
                              color: healthColor(d.center.health),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      LinearProgressIndicator(
                        value: (d.center.utilization / 100).clamp(0, 1),
                        minHeight: 9,
                        borderRadius: BorderRadius.circular(9),
                        color: healthColor(d.center.health),
                      ),
                      const SizedBox(height: 10),
                      Info('الميزانية', d.center.budget),
                      Info('المستخدم', d.center.used),
                      Info('المحجوز', d.center.reserved),
                      Info('المتاح', d.center.available),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: Metric(
                        title: 'متوسط الصرف الشهري',
                        value: d.average,
                        color: AppColors.primary,
                        icon: Icons.calendar_view_month,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Metric(
                        title: 'توقع نهاية العام',
                        value: d.forecast,
                        color: d.forecast > d.center.budget
                            ? AppColors.error
                            : AppColors.success,
                        icon: Icons.auto_graph,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                Section(
                  title: 'الصرف الفعلي مقابل الخطة',
                  icon: Icons.query_stats,
                  child: Bars(
                    points: d.months.take(DateTime.now().month).toList(),
                    color: healthColor(d.center.health),
                  ),
                ),
                if (d.departments.isNotEmpty) ...[
                  const SizedBox(height: 12),
                  Section(
                    title: 'الصرف حسب القسم',
                    icon: Icons.pie_chart_outline,
                    child: Breakdown(points: d.departments),
                  ),
                ],
                const SizedBox(height: 12),
                Section(
                  title: 'الطلبات المحملة على المركز',
                  icon: Icons.receipt_long_outlined,
                  child: d.orders.isEmpty
                      ? const Text('لا توجد طلبات')
                      : Column(
                          children: d.orders
                              .map(
                                (o) => ListTile(
                                  contentPadding: EdgeInsets.zero,
                                  onTap: () => context.push('/orders/${o.id}'),
                                  title: Text(
                                    o.number,
                                    style: const TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                  subtitle: Text(
                                    '${o.department}${o.project == null ? '' : ' • ${o.project}'} • ${DateFormat('d MMM yyyy', 'ar').format(o.createdAt)}',
                                  ),
                                  trailing: Text(
                                    '${money(o.total)} ج.م',
                                    style: const TextStyle(
                                      fontSize: 10,
                                      fontWeight: FontWeight.w900,
                                    ),
                                  ),
                                ),
                              )
                              .toList(),
                        ),
                ),
                const SizedBox(height: 14),
                OutlinedButton.icon(
                  onPressed: () => adjust(context, ref, d.center),
                  icon: const Icon(Icons.edit_note),
                  label: const Text('طلب تعديل الميزانية'),
                ),
              ],
            ),
          ),
        ),
  );
}

Future<void> adjust(
  BuildContext context,
  WidgetRef ref,
  BudgetCenterModel c,
) async {
  final amount = TextEditingController(
        text: (c.budget * 1.2).round().toString(),
      ),
      reason = TextEditingController();
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
          Text(
            'تعديل ميزانية ${c.code}',
            style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w900),
          ),
          Text(
            'الحالية ${money(c.budget)} ج.م',
            style: const TextStyle(color: AppColors.gray500),
          ),
          const SizedBox(height: 12),
          TextField(
            controller: amount,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'الميزانية المطلوبة'),
          ),
          const SizedBox(height: 9),
          TextField(
            controller: reason,
            maxLines: 3,
            decoration: const InputDecoration(labelText: 'مبررات التعديل'),
          ),
          const SizedBox(height: 12),
          FilledButton(
            onPressed: () async {
              try {
                await ref
                    .read(budgetRepositoryProvider)
                    .adjustment(c.id, double.parse(amount.text), reason.text);
                if (!sheet.mounted) return;
                ScaffoldMessenger.of(sheet).showSnackBar(
                  const SnackBar(content: Text('تم إرسال طلب التعديل')),
                );
                Navigator.pop(sheet);
              } catch (e) {
                if (sheet.mounted) {
                  ScaffoldMessenger.of(
                    sheet,
                  ).showSnackBar(SnackBar(content: Text('$e')));
                }
              }
            },
            child: const Text('إرسال الطلب'),
          ),
        ],
      ),
    ),
  );
  amount.dispose();
  reason.dispose();
}

class CenterTile extends StatelessWidget {
  const CenterTile({super.key, required this.center});
  final BudgetCenterModel center;
  @override
  Widget build(BuildContext context) => InkWell(
    onTap: () => context.push('/budgets/centers/${center.id}'),
    child: Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      center.name,
                      style: const TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    Text(
                      center.code,
                      style: const TextStyle(
                        fontSize: 8,
                        color: AppColors.gray500,
                      ),
                    ),
                  ],
                ),
              ),
              Text(
                '${money(center.available)} ج.م متاح',
                style: TextStyle(
                  fontSize: 9,
                  color: healthColor(center.health),
                  fontWeight: FontWeight.w800,
                ),
              ),
              const Icon(Icons.chevron_right, size: 18),
            ],
          ),
          const SizedBox(height: 6),
          LinearProgressIndicator(
            value: (center.utilization / 100).clamp(0, 1),
            minHeight: 6,
            borderRadius: BorderRadius.circular(6),
            color: healthColor(center.health),
          ),
          const SizedBox(height: 3),
          Row(
            children: [
              Text(
                '${center.utilization.toStringAsFixed(1)}% مستخدم',
                style: const TextStyle(fontSize: 8),
              ),
              const Spacer(),
              Text(
                '${money(center.budget)} ج.م',
                style: const TextStyle(fontSize: 8),
              ),
            ],
          ),
        ],
      ),
    ),
  );
}

class AlertCard extends StatelessWidget {
  const AlertCard({super.key, required this.alert});
  final BudgetAlertModel alert;
  @override
  Widget build(BuildContext context) {
    final critical = alert.severity == 'Critical';
    return Card(
      color: critical ? AppColors.errorTint : AppColors.warningTint,
      child: ListTile(
        leading: Icon(
          critical ? Icons.error_outline : Icons.warning_amber,
          color: critical ? AppColors.error : AppColors.warning,
        ),
        title: Text(
          alert.title,
          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w900),
        ),
        subtitle: Text(alert.body, style: const TextStyle(fontSize: 9)),
        trailing: alert.centerId == null
            ? null
            : const Icon(Icons.chevron_right),
        onTap: alert.centerId == null
            ? null
            : () => context.push('/budgets/centers/${alert.centerId}'),
      ),
    );
  }
}

class Bars extends StatelessWidget {
  const Bars({super.key, required this.points, required this.color});
  final List<BudgetPointModel> points;
  final Color color;
  @override
  Widget build(BuildContext context) {
    final max = points.fold<double>(0, (m, p) => p.amount > m ? p.amount : m);
    return SizedBox(
      height: 130,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: points
            .map(
              (p) => Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 2),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      Text(
                        p.amount <= 0 ? '' : compact(p.amount),
                        style: const TextStyle(fontSize: 6),
                      ),
                      Container(
                        height: max <= 0 ? 2 : 90 * p.amount / max + 2,
                        decoration: BoxDecoration(
                          color: color.withValues(alpha: .75),
                          borderRadius: const BorderRadius.vertical(
                            top: Radius.circular(4),
                          ),
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(p.label, style: const TextStyle(fontSize: 7)),
                    ],
                  ),
                ),
              ),
            )
            .toList(),
      ),
    );
  }
}

class Breakdown extends StatelessWidget {
  const Breakdown({super.key, required this.points});
  final List<BudgetPointModel> points;
  @override
  Widget build(BuildContext context) {
    final total = points.fold<double>(0, (a, b) => a + b.amount);
    return Column(
      children: points
          .asMap()
          .entries
          .map(
            (e) => Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Column(
                children: [
                  Row(
                    children: [
                      Container(
                        width: 9,
                        height: 9,
                        color: palette[e.key % palette.length],
                      ),
                      const SizedBox(width: 6),
                      Expanded(
                        child: Text(
                          e.value.label,
                          style: const TextStyle(
                            fontSize: 9,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ),
                      Text(
                        '${money(e.value.amount)} ج.م',
                        style: const TextStyle(
                          fontSize: 9,
                          fontWeight: FontWeight.w900,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 3),
                  LinearProgressIndicator(
                    value: total <= 0 ? 0 : e.value.amount / total,
                    minHeight: 4,
                    color: palette[e.key % palette.length],
                  ),
                ],
              ),
            ),
          )
          .toList(),
    );
  }
}

class Metric extends StatelessWidget {
  const Metric({
    super.key,
    required this.title,
    required this.value,
    required this.color,
    required this.icon,
  });
  final String title;
  final double value;
  final Color color;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(13),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(height: 7),
          Text(
            '${money(value)} ج.م',
            style: TextStyle(fontWeight: FontWeight.w900, color: color),
          ),
          Text(
            title,
            style: const TextStyle(fontSize: 8, color: AppColors.gray500),
          ),
        ],
      ),
    ),
  );
}

class Section extends StatelessWidget {
  const Section({
    super.key,
    required this.title,
    required this.icon,
    required this.child,
  });
  final String title;
  final IconData icon;
  final Widget child;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, size: 19, color: AppColors.primary),
              const SizedBox(width: 7),
              Text(title, style: const TextStyle(fontWeight: FontWeight.w900)),
            ],
          ),
          const Divider(height: 22),
          child,
        ],
      ),
    ),
  );
}

class Info extends StatelessWidget {
  const Info(this.label, this.value, {super.key});
  final String label;
  final double value;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 3),
    child: Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: const TextStyle(fontSize: 9, color: AppColors.gray500),
          ),
        ),
        Text(
          '${money(value)} ج.م',
          style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w900),
        ),
      ],
    ),
  );
}

const palette = [
  AppColors.primary,
  AppColors.success,
  AppColors.warning,
  Color(0xFF7A5AF8),
  Color(0xFF06AED4),
  AppColors.error,
];
String money(double v) => NumberFormat('#,##0.##', 'ar').format(v);
String compact(double v) => v >= 1000000
    ? '${(v / 1000000).toStringAsFixed(1)}م'
    : v >= 1000
    ? '${(v / 1000).toStringAsFixed(0)}أ'
    : v.toStringAsFixed(0);
Color healthColor(String h) => h == 'Exceeded'
    ? AppColors.error
    : h == 'Warning'
    ? AppColors.warning
    : AppColors.success;
