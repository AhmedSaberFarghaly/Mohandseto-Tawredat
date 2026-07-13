import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/api/rfq_repository.dart';
import '../../core/theme/app_tokens.dart';

class RfqsScreen extends ConsumerStatefulWidget {
  const RfqsScreen({super.key});
  @override
  ConsumerState<RfqsScreen> createState() => _RfqsScreenState();
}

class _RfqsScreenState extends ConsumerState<RfqsScreen> {
  String? _status;
  @override
  Widget build(BuildContext context) {
    final data = ref.watch(rfqListProvider(_status));
    return Scaffold(
      appBar: AppBar(title: const Text('طلبات عروض الأسعار')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/rfqs/new'),
        icon: const Icon(Icons.add),
        label: const Text('طلب جديد'),
      ),
      body: Column(
        children: [
          SizedBox(
            height: 52,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.all(8),
              children: [
                _filter('الكل', null),
                _filter('مسودة', 'Draft'),
                _filter('قيد المراجعة', 'UnderReview'),
                _filter('وصل عرض', 'Quoted'),
                _filter('تفاوض', 'Negotiating'),
                _filter('مكتمل', 'Converted'),
              ],
            ),
          ),
          Expanded(
            child: data.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => Center(child: Text('$error')),
              data: (items) => items.isEmpty
                  ? const _RfqEmpty()
                  : RefreshIndicator(
                      onRefresh: () async {
                        ref.invalidate(rfqListProvider(_status));
                        await ref.read(rfqListProvider(_status).future);
                      },
                      child: ListView.builder(
                        padding: const EdgeInsets.fromLTRB(14, 8, 14, 90),
                        itemCount: items.length,
                        itemBuilder: (context, index) => _card(items[index]),
                      ),
                    ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _filter(String text, String? value) => Padding(
    padding: const EdgeInsetsDirectional.only(end: 7),
    child: ChoiceChip(
      label: Text(text),
      selected: _status == value,
      onSelected: (_) => setState(() => _status = value),
    ),
  );
  Widget _card(RfqListItem item) => Card(
    margin: const EdgeInsets.only(bottom: 10),
    child: InkWell(
      onTap: () => context.push('/rfqs/${item.id}'),
      borderRadius: BorderRadius.circular(AppRadius.lg),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    item.title,
                    style: const TextStyle(fontWeight: FontWeight.w900),
                  ),
                ),
                _statusChip(item.status),
              ],
            ),
            Text(
              item.number,
              style: const TextStyle(fontSize: 9, color: AppColors.gray400),
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                _mini(Icons.inventory_2_outlined, '${item.itemCount} أصناف'),
                const SizedBox(width: 14),
                _mini(
                  Icons.event_outlined,
                  DateFormat('d MMM yyyy', 'ar').format(item.requiredDate),
                ),
              ],
            ),
            if (item.latestTotal != null) ...[
              const Divider(height: 20),
              Text(
                '${_money(item.latestTotal!)} ج.م',
                style: const TextStyle(
                  fontSize: 18,
                  color: AppColors.primary,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ],
        ),
      ),
    ),
  );
  Widget _mini(IconData icon, String text) => Row(
    children: [
      Icon(icon, size: 15, color: AppColors.gray500),
      const SizedBox(width: 4),
      Text(text, style: const TextStyle(fontSize: 9, color: AppColors.gray500)),
    ],
  );
}

class CreateRfqScreen extends ConsumerStatefulWidget {
  const CreateRfqScreen({super.key});
  @override
  ConsumerState<CreateRfqScreen> createState() => _CreateRfqScreenState();
}

class _CreateRfqScreenState extends ConsumerState<CreateRfqScreen> {
  int _step = 0;
  bool _busy = false;
  RfqDetailModel? _rfq;
  final _title = TextEditingController(),
      _description = TextEditingController(),
      _governorate = TextEditingController(text: 'القاهرة');
  DateTime _required = DateTime.now().add(const Duration(days: 14)),
      _deadline = DateTime.now().add(const Duration(days: 5));
  @override
  void dispose() {
    _title.dispose();
    _description.dispose();
    _governorate.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Text(
        ['بيانات طلب العرض', 'الأصناف والمرفقات', 'المراجعة والإرسال'][_step],
      ),
    ),
    body: Column(
      children: [
        _steps(),
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: _step == 0
                ? _basic()
                : _step == 1
                ? _items()
                : _review(),
          ),
        ),
      ],
    ),
    bottomNavigationBar: _bottom(),
  );
  Widget _steps() => Padding(
    padding: const EdgeInsets.all(14),
    child: Row(
      children: List.generate(
        3,
        (i) => Expanded(
          child: Container(
            height: 4,
            margin: const EdgeInsets.symmetric(horizontal: 3),
            decoration: BoxDecoration(
              color: i <= _step ? AppColors.primary : AppColors.gray200,
              borderRadius: BorderRadius.circular(4),
            ),
          ),
        ),
      ),
    ),
  );
  Widget _basic() => Column(
    children: [
      TextField(
        controller: _title,
        decoration: const InputDecoration(
          labelText: 'اسم طلب عرض السعر *',
          prefixIcon: Icon(Icons.request_quote_outlined),
        ),
      ),
      const SizedBox(height: 10),
      TextField(
        controller: _description,
        maxLines: 3,
        decoration: const InputDecoration(labelText: 'وصف الاحتياج'),
      ),
      const SizedBox(height: 10),
      TextField(
        controller: _governorate,
        decoration: const InputDecoration(labelText: 'محافظة التوصيل'),
      ),
      const SizedBox(height: 8),
      _date('آخر موعد لاستلام العروض', _deadline, (v) => _deadline = v),
      _date('تاريخ الاحتياج', _required, (v) => _required = v),
    ],
  );
  Widget _date(String title, DateTime value, void Function(DateTime) set) =>
      ListTile(
        contentPadding: EdgeInsets.zero,
        title: Text(title),
        subtitle: Text(DateFormat('EEEE، d MMMM yyyy', 'ar').format(value)),
        trailing: const Icon(Icons.calendar_month),
        onTap: () async {
          final picked = await showDatePicker(
            context: context,
            firstDate: DateTime.now().add(const Duration(days: 1)),
            lastDate: DateTime.now().add(const Duration(days: 180)),
            initialDate: value,
          );
          if (picked != null) setState(() => set(picked));
        },
      );
  Widget _items() => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Row(
        children: [
          Expanded(
            child: OutlinedButton.icon(
              onPressed: _busy ? null : _catalogItem,
              icon: const Icon(Icons.storefront_outlined),
              label: const Text('من الكتالوج'),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: OutlinedButton.icon(
              onPressed: _busy ? null : () => _itemDialog(),
              icon: const Icon(Icons.edit_note),
              label: const Text('صنف حر'),
            ),
          ),
        ],
      ),
      const SizedBox(height: 8),
      SizedBox(
        width: double.infinity,
        child: FilledButton.tonalIcon(
          onPressed: _busy ? null : _upload,
          icon: const Icon(Icons.upload_file),
          label: const Text('رفع Excel أو PDF أو صورة'),
        ),
      ),
      const SizedBox(height: 16),
      if (_rfq!.items.isEmpty)
        const _Hint(
          icon: Icons.inventory_2_outlined,
          text: 'أضف أصنافًا من الكتالوج أو اكتبها أو ارفع ملف الاحتياجات',
        ),
      ..._rfq!.items.map(
        (i) => Card(
          color: i.reviewed ? Colors.white : AppColors.warningTint,
          child: ListTile(
            onTap: () => _itemDialog(item: i),
            leading: CircleAvatar(child: Text('${_rfq!.items.indexOf(i) + 1}')),
            title: Text(i.description),
            subtitle: Text(
              '${i.quantity.toStringAsFixed(i.quantity % 1 == 0 ? 0 : 2)} ${i.unit} • ${_source(i.source)}${i.reviewed ? '' : ' • يحتاج مراجعة'}',
            ),
            trailing: i.productId == null
                ? const Icon(Icons.link_off, color: AppColors.warning)
                : const Icon(Icons.link, color: AppColors.success),
          ),
        ),
      ),
      if (_rfq!.files.isNotEmpty) ...[
        const Padding(
          padding: EdgeInsets.only(top: 12, bottom: 5),
          child: Text(
            'المرفقات',
            style: TextStyle(fontWeight: FontWeight.w800),
          ),
        ),
        ..._rfq!.files.map(
          (f) => ListTile(
            leading: Icon(
              f.type == 'Excel'
                  ? Icons.table_chart_outlined
                  : f.type == 'Pdf'
                  ? Icons.picture_as_pdf_outlined
                  : Icons.image_outlined,
            ),
            title: Text(f.name),
            subtitle: Text(f.status),
            trailing: f.status == 'Completed'
                ? const Icon(Icons.check_circle, color: AppColors.success)
                : const CircularProgressIndicator(),
          ),
        ),
      ],
    ],
  );
  Widget _review() => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Card(
        child: Padding(
          padding: const EdgeInsets.all(15),
          child: Column(
            children: [
              _kv('اسم الطلب', _rfq!.title),
              _kv('عدد الأصناف', '${_rfq!.items.length}'),
              _kv('المرفقات', '${_rfq!.files.length}'),
              _kv(
                'آخر موعد للعروض',
                DateFormat('d MMM yyyy', 'ar').format(_deadline),
              ),
              _kv(
                'تاريخ الاحتياج',
                DateFormat('d MMM yyyy', 'ar').format(_required),
              ),
            ],
          ),
        ),
      ),
      if (_rfq!.items.any((i) => !i.reviewed))
        const _Hint(
          icon: Icons.warning_amber,
          text: 'توجد عناصر مستخرجة تحتاج مراجعة قبل الإرسال',
          warning: true,
        ),
      const SizedBox(height: 12),
      const Text(
        'بعد الإرسال سيقوم فريق التسعير بمراجعة الأصناف وتجميع أفضل عرض، ويمكنك متابعة الحالة والتفاوض من مركز RFQ.',
        style: TextStyle(color: AppColors.gray500, height: 1.6),
      ),
    ],
  );
  Widget _bottom() => SafeArea(
    child: Container(
      padding: const EdgeInsets.all(12),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: AppColors.gray200)),
      ),
      child: Row(
        children: [
          if (_step > 0)
            OutlinedButton(
              onPressed: _busy ? null : () => setState(() => _step--),
              child: const Text('السابق'),
            ),
          if (_step > 0) const SizedBox(width: 8),
          Expanded(
            child: FilledButton(
              onPressed: _busy ? null : _next,
              child: Text(
                _busy
                    ? 'جاري الحفظ...'
                    : _step == 2
                    ? 'إرسال طلب العرض'
                    : 'متابعة',
              ),
            ),
          ),
        ],
      ),
    ),
  );
  Future<void> _next() async {
    try {
      setState(() => _busy = true);
      if (_step == 0) {
        if (_title.text.trim().isEmpty || !_deadline.isBefore(_required)) {
          throw StateError('أكمل البيانات واختر موعد عروض يسبق تاريخ الاحتياج');
        }
        _rfq = await ref
            .read(rfqRepositoryProvider)
            .create(
              title: _title.text.trim(),
              description: _description.text.trim(),
              requiredDate: _required,
              deadline: _deadline,
              governorate: _governorate.text.trim(),
            );
        setState(() => _step = 1);
      } else if (_step == 1) {
        if (_rfq!.items.isEmpty) throw StateError('أضف صنفًا واحدًا على الأقل');
        setState(() => _step = 2);
      } else {
        _rfq = await ref.read(rfqRepositoryProvider).submit(_rfq!.id);
        ref.invalidate(rfqListProvider);
        if (mounted) context.go('/rfqs/${_rfq!.id}');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _catalogItem() async {
    final chosen = await showDialog<CatalogProduct>(
      context: context,
      builder: (ctx) => const _CatalogPicker(),
    );
    if (chosen != null) await _itemDialog(product: chosen);
  }

  Future<void> _itemDialog({
    RfqItemModel? item,
    CatalogProduct? product,
  }) async {
    final description = TextEditingController(
          text: product?.nameAr ?? item?.description ?? '',
        ),
        qty = TextEditingController(text: item?.quantity.toString() ?? '1'),
        unit = TextEditingController(
          text: product?.unitName ?? item?.unit ?? 'قطعة',
        ),
        spec = TextEditingController(text: item?.specifications ?? ''),
        brand = TextEditingController(
          text: item?.brand ?? product?.brandName ?? '',
        );
    bool alternatives = item?.alternatives ?? true;
    final ok = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setLocal) => Padding(
          padding: EdgeInsets.fromLTRB(
            18,
            0,
            18,
            MediaQuery.viewInsetsOf(ctx).bottom + 18,
          ),
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  item == null ? 'إضافة صنف' : 'مراجعة الصنف',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: description,
                  decoration: const InputDecoration(labelText: 'وصف الصنف *'),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: qty,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(
                          labelText: 'الكمية *',
                        ),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: TextField(
                        controller: unit,
                        decoration: const InputDecoration(
                          labelText: 'الوحدة *',
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: spec,
                  maxLines: 2,
                  decoration: const InputDecoration(labelText: 'المواصفات'),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: brand,
                  decoration: const InputDecoration(
                    labelText: 'العلامة المفضلة',
                  ),
                ),
                SwitchListTile(
                  value: alternatives,
                  onChanged: (v) => setLocal(() => alternatives = v),
                  title: const Text('السماح ببدائل مطابقة'),
                ),
                SizedBox(
                  width: double.infinity,
                  child: FilledButton(
                    onPressed: () => Navigator.pop(
                      ctx,
                      description.text.trim().isNotEmpty &&
                          (double.tryParse(qty.text) ?? 0) > 0,
                    ),
                    child: const Text('حفظ الصنف'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
    if (ok == true) {
      setState(() => _busy = true);
      try {
        final repo = ref.read(rfqRepositoryProvider);
        if (item == null) {
          _rfq = await repo.addItem(
            _rfq!.id,
            productId: product?.id,
            description: description.text.trim(),
            quantity: double.parse(qty.text),
            unit: unit.text.trim(),
            specifications: spec.text.trim(),
            brand: brand.text.trim(),
            alternatives: alternatives,
            source: product == null ? 'FreeText' : 'Catalog',
          );
        } else {
          _rfq = await repo.updateItem(
            _rfq!.id,
            item.id,
            productId: product?.id ?? item.productId,
            description: description.text.trim(),
            quantity: double.parse(qty.text),
            unit: unit.text.trim(),
            specifications: spec.text.trim(),
            brand: brand.text.trim(),
            alternatives: alternatives,
            source: item.source,
          );
        }
        setState(() {});
      } finally {
        if (mounted) setState(() => _busy = false);
      }
    }
    description.dispose();
    qty.dispose();
    unit.dispose();
    spec.dispose();
    brand.dispose();
  }

  Future<void> _upload() async {
    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: const [
        'xlsx',
        'xls',
        'csv',
        'pdf',
        'png',
        'jpg',
        'jpeg',
      ],
      withData: true,
    );
    if (result == null) return;
    setState(() => _busy = true);
    try {
      await ref
          .read(rfqRepositoryProvider)
          .upload(_rfq!.id, result.files.single);
      _rfq = await ref.read(rfqRepositoryProvider).detail(_rfq!.id);
      setState(() {});
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _kv(String k, String v) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 5),
    child: Row(
      children: [
        Expanded(
          child: Text(k, style: const TextStyle(color: AppColors.gray500)),
        ),
        Text(v, style: const TextStyle(fontWeight: FontWeight.w700)),
      ],
    ),
  );
}

class RfqDetailScreen extends ConsumerStatefulWidget {
  const RfqDetailScreen({super.key, required this.id});
  final String id;
  @override
  ConsumerState<RfqDetailScreen> createState() => _RfqDetailScreenState();
}

class _RfqDetailScreenState extends ConsumerState<RfqDetailScreen> {
  bool _busy = false;
  @override
  Widget build(BuildContext context) {
    final value = ref.watch(rfqDetailProvider(widget.id));
    return Scaffold(
      appBar: AppBar(
        title: const Text('تفاصيل طلب عرض السعر'),
        actions: [
          IconButton(
            onPressed: () => ref.invalidate(rfqDetailProvider(widget.id)),
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: value.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('$e')),
        data: (d) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(rfqDetailProvider(widget.id));
            await ref.read(rfqDetailProvider(widget.id).future);
          },
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 40),
            children: [
              _header(d),
              _timeline(d),
              _section(
                'الأصناف (${d.items.length})',
                Column(
                  children: d.items
                      .map(
                        (i) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: const Icon(Icons.inventory_2_outlined),
                          title: Text(i.description),
                          subtitle: Text(
                            '${i.quantity} ${i.unit}${i.productId == null ? ' • غير مربوط بالكتالوج' : ''}',
                          ),
                        ),
                      )
                      .toList(),
                ),
              ),
              if (d.versions.isNotEmpty) ...[
                _quote(d),
                if (d.versions.length > 1) _compare(d),
              ],
              if (d.negotiations.isNotEmpty ||
                  d.status == 'Negotiating' ||
                  d.status == 'Quoted')
                _negotiation(d),
            ],
          ),
        ),
      ),
    );
  }

  Widget _header(RfqDetailModel d) => Card(
    child: Padding(
      padding: const EdgeInsets.all(15),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  d.title,
                  style: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
              _statusChip(d.status),
            ],
          ),
          Text(
            d.number,
            style: const TextStyle(fontSize: 9, color: AppColors.gray400),
          ),
          const Divider(height: 22),
          Row(
            children: [
              Expanded(
                child: _metric(
                  'تاريخ الاحتياج',
                  DateFormat('d MMM', 'ar').format(d.requiredDate),
                ),
              ),
              Expanded(child: _metric('عدد الأصناف', '${d.items.length}')),
              Expanded(child: _metric('المرفقات', '${d.files.length}')),
            ],
          ),
        ],
      ),
    ),
  );
  Widget _timeline(RfqDetailModel d) {
    const states = [
      'UnderReview',
      'Quoted',
      'Negotiating',
      'Accepted',
      'Converted',
    ];
    var current = states.indexOf(d.status);
    if (d.status == 'Submitted') current = 0;
    if (current < 0) {
      current = d.status == 'Draft' || d.status == 'NeedsReview' ? -1 : 0;
    }
    return Card(
      margin: const EdgeInsets.only(top: 10),
      child: Padding(
        padding: const EdgeInsets.all(13),
        child: Row(
          children: List.generate(
            states.length,
            (i) => Expanded(
              child: Column(
                children: [
                  CircleAvatar(
                    radius: 12,
                    backgroundColor: i <= current
                        ? AppColors.primary
                        : AppColors.gray200,
                    child: i < current
                        ? const Icon(Icons.check, size: 13, color: Colors.white)
                        : Text(
                            '${i + 1}',
                            style: TextStyle(
                              fontSize: 8,
                              color: i <= current
                                  ? Colors.white
                                  : AppColors.gray500,
                            ),
                          ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    ['مراجعة', 'عرض', 'تفاوض', 'قبول', 'طلب'][i],
                    style: const TextStyle(fontSize: 7),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _quote(RfqDetailModel d) {
    final q = d.versions.first;
    return _section(
      'عرض السعر ${d.quoteNumber ?? ''}',
      Column(
        children: [
          if (q.expired)
            const _Hint(
              icon: Icons.timer_off,
              text: 'انتهت صلاحية هذا العرض. اطلب إعادة تسعير.',
              warning: true,
            ),
          ...q.items.map(
            (i) => ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(i.description),
              subtitle: Text(
                '${i.quantity} ${i.unit}${i.alternative ? ' • بديل مقترح' : ''}',
              ),
              trailing: Text('${_money(i.total)} ج.م'),
            ),
          ),
          const Divider(),
          _sum('الإجمالي قبل الضريبة', q.subtotal),
          _sum('الضريبة', q.tax),
          _sum('الشحن', q.shipping),
          _sum('الإجمالي', q.total, strong: true),
          _sum('مدة التوريد', '${q.deliveryDays} أيام'),
          const SizedBox(height: 10),
          Row(
            children: [
              IconButton(
                onPressed: () => _download(q),
                tooltip: 'تنزيل PDF',
                icon: const Icon(Icons.picture_as_pdf_outlined),
              ),
              IconButton(
                onPressed: () => _share(d, q),
                tooltip: 'مشاركة',
                icon: const Icon(Icons.share_outlined),
              ),
              const Spacer(),
              OutlinedButton(
                onPressed: _busy ? null : () => _decision(d, q, 'reject'),
                child: const Text('رفض'),
              ),
              const SizedBox(width: 6),
              OutlinedButton(
                onPressed: _busy ? null : () => _decision(d, q, 'requote'),
                child: const Text('طلب تعديل'),
              ),
              const SizedBox(width: 6),
              FilledButton(
                onPressed: _busy || q.expired
                    ? null
                    : () => _decision(d, q, 'accept'),
                child: const Text('قبول'),
              ),
            ],
          ),
          if (d.status == 'Accepted')
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _busy ? null : () => _convert(d),
                icon: const Icon(Icons.shopping_cart_checkout),
                label: const Text('تحويل العرض إلى طلب شراء'),
              ),
            ),
        ],
      ),
    );
  }

  Widget _compare(RfqDetailModel d) => _section(
    'مقارنة إصدارات العرض',
    SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: DataTable(
        columns: const [
          DataColumn(label: Text('الإصدار')),
          DataColumn(label: Text('الإجمالي')),
          DataColumn(label: Text('التوريد')),
          DataColumn(label: Text('التغيير')),
        ],
        rows: d.versions
            .map(
              (q) => DataRow(
                cells: [
                  DataCell(Text('V${q.version}')),
                  DataCell(Text(_money(q.total))),
                  DataCell(Text('${q.deliveryDays} أيام')),
                  DataCell(Text(q.summary ?? '—')),
                ],
              ),
            )
            .toList(),
      ),
    ),
  );
  Widget _negotiation(RfqDetailModel detail) => _section(
    'التفاوض والاستفسارات',
    Column(
      children: [
        ...detail.negotiations.map(
          (message) => Align(
            alignment: message.staff
                ? AlignmentDirectional.centerStart
                : AlignmentDirectional.centerEnd,
            child: Container(
              margin: const EdgeInsets.only(bottom: 7),
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: message.staff
                    ? AppColors.gray100
                    : AppColors.primaryTint,
                borderRadius: BorderRadius.circular(AppRadius.md),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(message.message),
                  if (message.proposed != null)
                    Text(
                      'المقترح: ${_money(message.proposed!)} ج.م',
                      style: const TextStyle(
                        fontWeight: FontWeight.w900,
                        color: AppColors.primary,
                      ),
                    ),
                  Text(
                    DateFormat(
                      'd MMM، HH:mm',
                      'ar',
                    ).format(message.createdAt.toLocal()),
                    style: const TextStyle(
                      fontSize: 7,
                      color: AppColors.gray400,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
        OutlinedButton.icon(
          onPressed: _busy ? null : () => _sendMessage(detail),
          icon: const Icon(Icons.chat_bubble_outline),
          label: const Text('إرسال رسالة أو عرض مقابل'),
        ),
      ],
    ),
  );
  Future<void> _decision(
    RfqDetailModel d,
    QuoteVersionModel q,
    String action,
  ) async {
    final c = TextEditingController();
    final ok = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (ctx) => Padding(
        padding: EdgeInsets.fromLTRB(
          18,
          0,
          18,
          MediaQuery.viewInsetsOf(ctx).bottom + 18,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              {
                'accept': 'قبول عرض السعر',
                'reject': 'رفض عرض السعر',
                'requote': 'طلب تعديل السعر',
              }[action]!,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w900),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: c,
              maxLines: 3,
              decoration: InputDecoration(
                labelText: action == 'accept' ? 'تعليق اختياري' : 'السبب *',
              ),
            ),
            const SizedBox(height: 10),
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: () => Navigator.pop(
                  ctx,
                  action == 'accept' || c.text.trim().isNotEmpty,
                ),
                child: const Text('تأكيد'),
              ),
            ),
          ],
        ),
      ),
    );
    if (ok == true) {
      await _run(
        () => ref
            .read(rfqRepositoryProvider)
            .decision(
              d.id,
              action,
              q.id,
              c.text.trim().isEmpty ? null : c.text.trim(),
            ),
      );
    }
    c.dispose();
  }

  Future<void> _sendMessage(RfqDetailModel d) async {
    final c = TextEditingController(), p = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('رسالة تفاوض'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: c,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'الرسالة *'),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: p,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(
                labelText: 'إجمالي مقترح (اختياري)',
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('إلغاء'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, c.text.trim().isNotEmpty),
            child: const Text('إرسال'),
          ),
        ],
      ),
    );
    if (ok == true) {
      await _run(
        () => ref
            .read(rfqRepositoryProvider)
            .negotiate(
              d.id,
              c.text.trim(),
              proposed: double.tryParse(p.text),
              type: p.text.isEmpty ? 'Message' : 'CounterOffer',
            ),
      );
    }
    c.dispose();
    p.dispose();
  }

  Future<void> _download(QuoteVersionModel q) async {
    try {
      final bytes = await ref
          .read(rfqRepositoryProvider)
          .quotePdf(widget.id, q.id);
      final saved = await FilePicker.saveFile(
        dialogTitle: 'حفظ عرض السعر',
        fileName: 'quote-${widget.id}-v${q.version}.pdf',
        type: FileType.custom,
        allowedExtensions: const ['pdf'],
        bytes: bytes,
      );
      if (saved != null && mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('تم حفظ عرض السعر')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  Future<void> _share(RfqDetailModel d, QuoteVersionModel q) async {
    await Clipboard.setData(
      ClipboardData(
        text:
            'عرض السعر ${d.quoteNumber} — الإصدار ${q.version} — الإجمالي ${_money(q.total)} ج.م — صالح حتى ${DateFormat('d/M/yyyy').format(q.validUntil)}',
      ),
    );
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('تم نسخ ملخص العرض للمشاركة')),
      );
    }
  }

  Future<void> _convert(RfqDetailModel detail) async {
    final options = await ref.read(rfqRepositoryProvider).conversionOptions();
    if (!mounted) return;
    String? branch = options.branches.isEmpty
        ? null
        : options.branches.first['id'] as String;
    String? center = options.centers.isEmpty
        ? null
        : options.centers.first['id'] as String;
    final firstReceiver = options.receivers.isEmpty
        ? null
        : options.receivers.first;
    final receiver = TextEditingController(
      text: firstReceiver?['name'] as String? ?? '',
    );
    final phone = TextEditingController(
      text: firstReceiver?['phone'] as String? ?? '',
    );
    final accepted = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setLocal) => AlertDialog(
          title: const Text('تحويل العرض إلى طلب'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<String>(
                value: branch,
                decoration: const InputDecoration(labelText: 'عنوان التوصيل'),
                items: options.branches
                    .map(
                      (item) => DropdownMenuItem(
                        value: item['id'] as String,
                        child: Text(item['name'] as String),
                      ),
                    )
                    .toList(),
                onChanged: (value) => setLocal(() => branch = value),
              ),
              const SizedBox(height: 8),
              DropdownButtonFormField<String>(
                value: center,
                decoration: const InputDecoration(labelText: 'مركز التكلفة'),
                items: options.centers
                    .map(
                      (item) => DropdownMenuItem(
                        value: item['id'] as String,
                        child: Text('${item['code']} — ${item['name']}'),
                      ),
                    )
                    .toList(),
                onChanged: (value) => setLocal(() => center = value),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: receiver,
                decoration: const InputDecoration(labelText: 'مسؤول الاستلام'),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: phone,
                decoration: const InputDecoration(labelText: 'الهاتف'),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('إلغاء'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(
                dialogContext,
                branch != null &&
                    center != null &&
                    receiver.text.isNotEmpty &&
                    phone.text.isNotEmpty,
              ),
              child: const Text('إنشاء الطلب'),
            ),
          ],
        ),
      ),
    );
    if (accepted == true) {
      setState(() => _busy = true);
      try {
        final order = await ref
            .read(rfqRepositoryProvider)
            .convert(
              detail.id,
              branchId: branch!,
              costCenterId: center!,
              receiverName: receiver.text,
              receiverPhone: phone.text,
            );
        ref.invalidate(rfqDetailProvider(widget.id));
        if (mounted) {
          await showDialog<void>(
            context: context,
            builder: (dialogContext) => AlertDialog(
              icon: const Icon(
                Icons.check_circle,
                color: AppColors.success,
                size: 42,
              ),
              title: const Text('تم إنشاء الطلب بنجاح'),
              content: Text('${order['orderNumber']}\n${order['status']}'),
              actions: [
                FilledButton(
                  onPressed: () => Navigator.pop(dialogContext),
                  child: const Text('تم'),
                ),
              ],
            ),
          );
        }
      } finally {
        if (mounted) setState(() => _busy = false);
      }
    }
    receiver.dispose();
    phone.dispose();
  }

  Future<void> _run(Future<RfqDetailModel> Function() action) async {
    setState(() => _busy = true);
    try {
      await action();
      ref.invalidate(rfqDetailProvider(widget.id));
      ref.invalidate(rfqListProvider);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Widget _section(String title, Widget child) => Card(
    margin: const EdgeInsets.only(top: 10),
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w900),
          ),
          const SizedBox(height: 10),
          child,
        ],
      ),
    ),
  );
  Widget _metric(String k, String v) => Column(
    children: [
      Text(v, style: const TextStyle(fontWeight: FontWeight.w800)),
      Text(k, style: const TextStyle(fontSize: 8, color: AppColors.gray500)),
    ],
  );
  Widget _sum(String k, Object v, {bool strong = false}) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 3),
    child: Row(
      children: [
        Expanded(
          child: Text(
            k,
            style: TextStyle(
              fontWeight: strong ? FontWeight.w900 : FontWeight.w400,
            ),
          ),
        ),
        Text(
          v is double ? '${_money(v)} ج.م' : '$v',
          style: TextStyle(
            fontSize: strong ? 17 : 12,
            fontWeight: FontWeight.w900,
            color: strong ? AppColors.primary : null,
          ),
        ),
      ],
    ),
  );
}

class _CatalogPicker extends ConsumerStatefulWidget {
  const _CatalogPicker();
  @override
  ConsumerState<_CatalogPicker> createState() => _CatalogPickerState();
}

class _CatalogPickerState extends ConsumerState<_CatalogPicker> {
  String _query = '';
  @override
  Widget build(BuildContext context) {
    final data = ref.watch(
      productFeedProvider(CatalogQuery(q: _query, pageSize: 15)),
    );
    return AlertDialog(
      title: const Text('اختيار من الكتالوج'),
      content: SizedBox(
        width: 520,
        height: 480,
        child: Column(
          children: [
            TextField(
              onChanged: (v) => setState(() => _query = v),
              decoration: const InputDecoration(
                prefixIcon: Icon(Icons.search),
                hintText: 'ابحث عن منتج',
              ),
            ),
            const SizedBox(height: 8),
            Expanded(
              child: data.when(
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (e, _) => Center(child: Text('$e')),
                data: (page) => ListView.builder(
                  itemCount: page.items.length,
                  itemBuilder: (c, i) {
                    final p = page.items[i];
                    return ListTile(
                      onTap: () => Navigator.pop(context, p),
                      leading: const Icon(Icons.inventory_2_outlined),
                      title: Text(p.nameAr),
                      subtitle: Text('${p.sku} • ${p.unitName}'),
                      trailing: Text('${_money(p.price)} ج.م'),
                    );
                  },
                ),
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('إلغاء'),
        ),
      ],
    );
  }
}

class _RfqEmpty extends StatelessWidget {
  const _RfqEmpty();
  @override
  Widget build(BuildContext context) => ListView(
    children: const [
      Padding(
        padding: EdgeInsets.only(top: 110),
        child: Column(
          children: [
            Icon(
              Icons.request_quote_outlined,
              size: 70,
              color: AppColors.gray300,
            ),
            SizedBox(height: 12),
            Text(
              'لا توجد طلبات عروض أسعار',
              style: TextStyle(fontWeight: FontWeight.w900),
            ),
            Text(
              'أنشئ طلبًا جديدًا من ملف أو من الكتالوج',
              style: TextStyle(fontSize: 10, color: AppColors.gray500),
            ),
          ],
        ),
      ),
    ],
  );
}

class _Hint extends StatelessWidget {
  const _Hint({required this.icon, required this.text, this.warning = false});
  final IconData icon;
  final String text;
  final bool warning;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(13),
    decoration: BoxDecoration(
      color: warning ? AppColors.warningTint : AppColors.gray100,
      borderRadius: BorderRadius.circular(AppRadius.md),
    ),
    child: Row(
      children: [
        Icon(icon, color: warning ? AppColors.warning : AppColors.gray400),
        const SizedBox(width: 8),
        Expanded(child: Text(text, style: const TextStyle(fontSize: 10))),
      ],
    ),
  );
}

Widget _statusChip(String s) => Chip(
  label: Text(_statusName(s), style: const TextStyle(fontSize: 8)),
  side: BorderSide.none,
  backgroundColor: _statusColor(s).withValues(alpha: .12),
);
String _statusName(String s) =>
    {
      'Draft': 'مسودة',
      'NeedsReview': 'مراجعة العناصر',
      'UnderReview': 'قيد المراجعة',
      'ClarificationRequested': 'مطلوب توضيح',
      'Quoted': 'وصل عرض',
      'Negotiating': 'تفاوض',
      'Accepted': 'عرض مقبول',
      'Rejected': 'مرفوض',
      'Converted': 'تم التحويل لطلب',
      'Expired': 'منتهي',
    }[s] ??
    s;
Color _statusColor(String s) =>
    {
      'Draft': AppColors.gray500,
      'NeedsReview': AppColors.warning,
      'UnderReview': AppColors.primary,
      'Quoted': AppColors.success,
      'Negotiating': AppColors.warning,
      'Accepted': AppColors.success,
      'Rejected': AppColors.error,
      'Converted': AppColors.success,
      'Expired': AppColors.error,
    }[s] ??
    AppColors.primary;
String _source(String s) =>
    {
      'Catalog': 'الكتالوج',
      'FreeText': 'إدخال يدوي',
      'Excel': 'Excel',
      'Pdf': 'PDF',
      'Image': 'صورة',
    }[s] ??
    s;
String _money(double v) => NumberFormat('#,##0.00', 'ar').format(v);
