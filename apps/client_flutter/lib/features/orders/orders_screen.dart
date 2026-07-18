import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../core/widgets/skeleton.dart';
import '../../core/api/order_repository.dart';
import '../../core/theme/app_tokens.dart';

class OrdersScreen extends ConsumerStatefulWidget {
  const OrdersScreen({super.key});
  @override
  ConsumerState<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends ConsumerState<OrdersScreen> {
  final search = TextEditingController();
  String? status;
  OrderQuery get query =>
      OrderQuery(search: search.text.trim(), status: status);
  @override
  void dispose() {
    search.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final result = ref.watch(orderListProvider(query));
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            _OrdersHeader(count: result.value?.length),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 10),
              child: TextField(
                controller: search,
                textInputAction: TextInputAction.search,
                decoration: InputDecoration(
                  hintText: 'ابحث برقم الطلب أو المنتج',
                  prefixIcon: const Icon(Icons.search_rounded),
                  suffixIcon: search.text.isEmpty
                      ? null
                      : IconButton(
                          onPressed: () {
                            search.clear();
                            setState(() {});
                          },
                          icon: const Icon(Icons.close_rounded),
                        ),
                ),
                onChanged: (_) => setState(() {}),
                onSubmitted: (_) => setState(() {}),
              ),
            ),
            SizedBox(
              height: 48,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 16),
                children: [
                  _filter('كل الطلبات', null),
                  _filter('قيد المراجعة', 'PendingApproval'),
                  _filter('قيد التجهيز', 'Processing'),
                  _filter('تم الشحن', 'Shipped'),
                  _filter('قيد التوصيل', 'OutForDelivery'),
                  _filter('مكتمل', 'Delivered'),
                ],
              ),
            ),
            Expanded(
              child: result.when(
                loading: () => const ListSkeleton(),
                error: (e, _) => _Error(
                  error: e,
                  retry: () => ref.invalidate(orderListProvider(query)),
                ),
                data: (items) => items.isEmpty
                    ? const _Empty()
                    : RefreshIndicator(
                        onRefresh: () async {
                          ref.invalidate(orderListProvider(query));
                          await ref.read(orderListProvider(query).future);
                        },
                        child: ListView.builder(
                          padding: const EdgeInsets.fromLTRB(16, 12, 16, 110),
                          itemCount: items.length,
                          itemBuilder: (_, i) => _OrderCard(order: items[i]),
                        ),
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _filter(String label, String? value) => Padding(
    padding: const EdgeInsetsDirectional.only(end: 7),
    child: ChoiceChip(
      label: Text(label),
      selected: status == value,
      onSelected: (_) => setState(() => status = value),
    ),
  );
}

class _OrdersHeader extends StatelessWidget {
  const _OrdersHeader({this.count});
  final int? count;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
    child: Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'طلباتي',
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                count == null ? 'تابع مشتريات شركتك' : '$count طلب في القائمة',
                style: const TextStyle(
                  color: AppColors.gray500,
                  fontSize: 10.5,
                ),
              ),
            ],
          ),
        ),
        IconButton.filledTonal(
          onPressed: () => context.push('/finance'),
          icon: const Icon(Icons.account_balance_wallet_outlined),
          tooltip: 'الفواتير والمدفوعات',
        ),
        const SizedBox(width: 7),
        IconButton.filledTonal(
          onPressed: () => context.push('/returns'),
          icon: const Icon(Icons.assignment_return_outlined),
          tooltip: 'مركز المرتجعات',
        ),
      ],
    ),
  );
}

class _OrderCard extends StatelessWidget {
  const _OrderCard({required this.order});
  final OrderListItem order;
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 14),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: InkWell(
      borderRadius: BorderRadius.circular(AppRadius.xl),
      onTap: () => context.push('/orders/${order.id}'),
      child: Padding(
        padding: const EdgeInsets.all(15),
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
                        'طلب #${order.number}',
                        style: const TextStyle(
                          fontWeight: FontWeight.w700,
                          fontSize: 14,
                        ),
                      ),
                      Text(
                        DateFormat('d MMMM yyyy', 'ar').format(order.createdAt),
                        style: const TextStyle(
                          color: AppColors.gray500,
                          fontSize: 11,
                        ),
                      ),
                    ],
                  ),
                ),
                _Status(status: order.status),
              ],
            ),
            const SizedBox(height: 13),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: LinearProgressIndicator(
                value: _orderProgress(order.status),
                minHeight: 6,
                backgroundColor: AppColors.gray150,
                color: statusColor(order.status),
              ),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                const Icon(
                  Icons.inventory_2_outlined,
                  size: 16,
                  color: AppColors.gray500,
                ),
                Text(
                  ' ${order.itemCount} أصناف',
                  style: const TextStyle(
                    fontSize: 11,
                    color: AppColors.gray500,
                  ),
                ),
                const SizedBox(width: 18),
                const Icon(
                  Icons.event_outlined,
                  size: 16,
                  color: AppColors.gray500,
                ),
                Text(
                  ' ${DateFormat('d MMM', 'ar').format(order.requiredDate)}',
                  style: const TextStyle(
                    fontSize: 11,
                    color: AppColors.gray500,
                  ),
                ),
              ],
            ),
            const Divider(height: 24),
            Row(
              children: [
                Expanded(
                  child: Text(
                    '${money(order.total)} ج.م',
                    style: const TextStyle(
                      fontSize: 18,
                      color: AppColors.primary,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
                if (order.canTrack) ...[
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 10,
                      vertical: 7,
                    ),
                    decoration: BoxDecoration(
                      color: AppColors.successTint,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          Icons.location_searching_rounded,
                          color: AppColors.success,
                          size: 16,
                        ),
                        SizedBox(width: 5),
                        Text(
                          'تتبع الشحنة',
                          style: TextStyle(
                            color: AppColors.success,
                            fontSize: 10,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                ] else
                  const Icon(
                    Icons.arrow_back_ios_new_rounded,
                    color: AppColors.gray400,
                    size: 14,
                  ),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

double _orderProgress(String status) => switch (status) {
  'PendingApproval' => .12,
  'Confirmed' => .24,
  'Processing' || 'Picking' => .42,
  'Packing' => .58,
  'Shipped' => .72,
  'OutForDelivery' => .88,
  'Delivered' || 'Completed' => 1,
  'Cancelled' => 1,
  _ => .2,
};

class OrderDetailScreen extends ConsumerWidget {
  const OrderDetailScreen({super.key, required this.id});
  final String id;
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final result = ref.watch(orderDetailProvider(id));
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('تتبع الطلب'),
        actions: [
          IconButton(
            tooltip: 'مركز الدعم',
            onPressed: () => context.push('/support'),
            icon: const Icon(Icons.support_agent_outlined),
          ),
        ],
      ),
      body: result.when(
        loading: () => const ListSkeleton(),
        error: (e, _) => _Error(
          error: e,
          retry: () => ref.invalidate(orderDetailProvider(id)),
        ),
        data: (o) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(orderDetailProvider(id));
            await ref.read(orderDetailProvider(id).future);
          },
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 120),
            children: [
              _Hero(order: o),
              const SizedBox(height: 14),
              _OrderJourney(order: o),
              const SizedBox(height: 14),
              _Timeline(order: o),
              if (o.shipments.isNotEmpty) ...[
                const SizedBox(height: 14),
                ...o.shipments.map((s) => _Shipment(shipment: s)),
              ],
              const SizedBox(height: 14),
              Section(
                title: 'المنتجات',
                icon: Icons.inventory_2_outlined,
                child: Column(
                  children: o.items.map((i) => _Line(line: i)).toList(),
                ),
              ),
              const SizedBox(height: 14),
              _Delivery(order: o),
              const SizedBox(height: 14),
              _Company(order: o),
              const SizedBox(height: 14),
              _Summary(order: o),
              if (o.issues.isNotEmpty) ...[
                const SizedBox(height: 14),
                _Issues(items: o.issues),
              ],
              const SizedBox(height: 18),
              _Actions(order: o),
            ],
          ),
        ),
      ),
    );
  }
}

class _Hero extends StatelessWidget {
  const _Hero({required this.order});
  final OrderDetailModel order;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(19),
    decoration: BoxDecoration(
      gradient: const LinearGradient(
        colors: [AppColors.primary, AppColors.primaryDark],
      ),
      borderRadius: BorderRadius.circular(20),
      boxShadow: AppShadows.floating,
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: .14),
                borderRadius: BorderRadius.circular(14),
              ),
              child: const Icon(
                Icons.local_shipping_outlined,
                color: Colors.white,
                size: 24,
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'طلب #${order.number}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w700,
                      fontSize: 15,
                    ),
                  ),
                  Text(
                    DateFormat('d MMMM yyyy، h:mm a', 'ar').format(
                      (order.history.firstOrNull?.at ?? order.requiredDate)
                          .toLocal(),
                    ),
                    style: const TextStyle(
                      color: Colors.white70,
                      fontSize: 9.5,
                    ),
                  ),
                ],
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: .16),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Text(
                statusAr(order.status),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        Text(
          '${money(order.total)} ج.م',
          style: const TextStyle(
            color: Colors.white,
            fontSize: 26,
            fontWeight: FontWeight.w700,
          ),
        ),
        Text(
          '${order.items.length} أصناف  •  التسليم المتوقع ${DateFormat('d MMMM yyyy', 'ar').format(order.requiredDate)}',
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(color: Colors.white70, fontSize: 11),
        ),
      ],
    ),
  );
}

class _OrderJourney extends StatelessWidget {
  const _OrderJourney({required this.order});
  final OrderDetailModel order;

  static const _stages = [
    (Icons.check_circle_outline_rounded, 'تم التأكيد'),
    (Icons.inventory_2_outlined, 'التجهيز'),
    (Icons.local_shipping_outlined, 'الشحن'),
    (Icons.home_work_outlined, 'التسليم'),
  ];

  @override
  Widget build(BuildContext context) {
    final progress = _orderProgress(order.status);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(AppRadius.xl),
        border: Border.all(color: AppColors.gray150),
        boxShadow: AppShadows.soft,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Text(
                'رحلة طلبك',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14),
              ),
              const Spacer(),
              Text(
                '${(progress * 100).round()}%',
                style: const TextStyle(
                  color: AppColors.primary,
                  fontWeight: FontWeight.w700,
                  fontSize: 11,
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          ClipRRect(
            borderRadius: BorderRadius.circular(8),
            child: LinearProgressIndicator(
              value: progress,
              minHeight: 7,
              backgroundColor: AppColors.gray150,
              color: statusColor(order.status),
            ),
          ),
          const SizedBox(height: 13),
          Row(
            children: _stages.indexed.map((entry) {
              final reached = progress >= ((entry.$1 + 1) / 4);
              return Expanded(
                child: Column(
                  children: [
                    Container(
                      width: 38,
                      height: 38,
                      decoration: BoxDecoration(
                        color: reached
                            ? AppColors.primaryTint
                            : AppColors.gray100,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        entry.$2.$1,
                        color: reached ? AppColors.primary : AppColors.gray400,
                        size: 20,
                      ),
                    ),
                    const SizedBox(height: 5),
                    Text(
                      entry.$2.$2,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: reached ? AppColors.gray800 : AppColors.gray400,
                        fontSize: 8.5,
                        fontWeight: reached ? FontWeight.w700 : FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }
}

class _Timeline extends StatelessWidget {
  const _Timeline({required this.order});
  final OrderDetailModel order;
  @override
  Widget build(BuildContext context) => Section(
    title: 'حالة الطلب',
    icon: Icons.route_outlined,
    child: Column(
      children: order.history.asMap().entries.map((x) {
        final h = x.value, last = x.key == order.history.length - 1;
        return Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 25,
              child: Column(
                children: [
                  Container(
                    width: 16,
                    height: 16,
                    decoration: const BoxDecoration(
                      shape: BoxShape.circle,
                      color: AppColors.primary,
                    ),
                    child: const Icon(
                      Icons.check,
                      color: Colors.white,
                      size: 10,
                    ),
                  ),
                  if (!last)
                    Container(
                      width: 2,
                      height: 44,
                      color: AppColors.primaryTint,
                    ),
                ],
              ),
            ),
            const SizedBox(width: 7),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      statusAr(h.status),
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    if (h.note != null)
                      Text(
                        h.note!,
                        style: const TextStyle(
                          fontSize: 11,
                          color: AppColors.gray500,
                        ),
                      ),
                    Text(
                      DateFormat('d MMM، h:mm a', 'ar').format(h.at.toLocal()),
                      style: const TextStyle(
                        fontSize: 10.5,
                        color: AppColors.gray400,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        );
      }).toList(),
    ),
  );
}

class _Shipment extends StatelessWidget {
  const _Shipment({required this.shipment});
  final ShipmentModel shipment;
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 10),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(
                Icons.local_shipping_outlined,
                color: AppColors.primary,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'الشحنة ${shipment.number}',
                      style: const TextStyle(fontWeight: FontWeight.w700),
                    ),
                    Text(
                      '${shipment.carrier}${shipment.trackingNumber == null ? '' : ' • ${shipment.trackingNumber}'}',
                      style: const TextStyle(
                        fontSize: 10.5,
                        color: AppColors.gray500,
                      ),
                    ),
                  ],
                ),
              ),
              _Status(status: shipment.status),
            ],
          ),
          if (shipment.status == 'OutForDelivery') ...[
            const SizedBox(height: 10),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
              decoration: BoxDecoration(
                color: AppColors.successTint,
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Row(
                children: [
                  SizedBox(
                    width: 8,
                    height: 8,
                    child: DecoratedBox(
                      decoration: BoxDecoration(
                        color: AppColors.success,
                        shape: BoxShape.circle,
                      ),
                    ),
                  ),
                  SizedBox(width: 7),
                  Text(
                    'تحديث مباشر لموقع الشحنة',
                    style: TextStyle(
                      color: AppColors.success,
                      fontSize: 9.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ],
          if (shipment.latitude != null) ...[
            const SizedBox(height: 14),
            Container(
              height: 155,
              clipBehavior: Clip.antiAlias,
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFFEAF2FF), Color(0xFFDDE8F2)],
                  begin: Alignment.topRight,
                  end: Alignment.bottomLeft,
                ),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Stack(
                children: [
                  Positioned.fill(child: CustomPaint(painter: _MapPainter())),
                  const Center(
                    child: CircleAvatar(
                      backgroundColor: AppColors.primary,
                      child: Icon(
                        Icons.local_shipping,
                        color: Colors.white,
                        size: 19,
                      ),
                    ),
                  ),
                  Positioned(
                    bottom: 9,
                    left: 9,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 5,
                      ),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(9),
                        boxShadow: AppShadows.soft,
                      ),
                      child: Text(
                        '${shipment.latitude!.toStringAsFixed(4)}, ${shipment.longitude!.toStringAsFixed(4)}',
                        style: const TextStyle(fontSize: 9.5),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
          if (shipment.driverName != null) ...[
            const SizedBox(height: 12),
            Row(
              children: [
                const CircleAvatar(child: Icon(Icons.person_outline)),
                const SizedBox(width: 8),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        shipment.driverName!,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      Text(
                        shipment.driverPhone ?? '',
                        style: const TextStyle(
                          fontSize: 10.5,
                          color: AppColors.gray500,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(
                        'رقم السائق: ${shipment.driverPhone ?? 'غير متاح'}',
                      ),
                    ),
                  ),
                  style: IconButton.styleFrom(
                    backgroundColor: AppColors.primaryTint,
                  ),
                  icon: const Icon(
                    Icons.phone_rounded,
                    color: AppColors.primary,
                  ),
                ),
              ],
            ),
          ],
          if (shipment.eta != null)
            Row(
              children: [
                const Icon(Icons.schedule, color: AppColors.warning, size: 16),
                Text(
                  ' الوصول المتوقع ${DateFormat('h:mm a', 'ar').format(shipment.eta!.toLocal())}',
                  style: const TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          const Divider(height: 22),
          ...shipment.events.reversed
              .take(3)
              .map(
                (e) => Padding(
                  padding: const EdgeInsets.only(bottom: 7),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(
                        Icons.check_circle,
                        size: 15,
                        color: AppColors.success,
                      ),
                      const SizedBox(width: 6),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              e.description,
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                            if (e.location != null)
                              Text(
                                e.location!,
                                style: const TextStyle(
                                  fontSize: 9.5,
                                  color: AppColors.gray500,
                                ),
                              ),
                          ],
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

class _MapPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3;
    final path = Path()
      ..moveTo(0, size.height * .7)
      ..quadraticBezierTo(
        size.width * .3,
        size.height * .1,
        size.width * .52,
        size.height * .58,
      )
      ..quadraticBezierTo(
        size.width * .75,
        size.height * .9,
        size.width,
        size.height * .2,
      );
    canvas.drawPath(path, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

class _Line extends StatelessWidget {
  const _Line({required this.line});
  final OrderLineModel line;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 9),
    child: Row(
      children: [
        Container(
          width: 52,
          height: 52,
          decoration: BoxDecoration(
            color: AppColors.gray100,
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Icon(
            Icons.inventory_2_outlined,
            color: AppColors.primary,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                line.name,
                maxLines: 2,
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                '${line.sku} • الكمية ${line.quantity}',
                style: const TextStyle(fontSize: 9.5, color: AppColors.gray500),
              ),
              if (line.rating != null) _Stars(value: line.rating!),
            ],
          ),
        ),
        Text(
          '${money(line.total)} ج.م',
          style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 11),
        ),
      ],
    ),
  );
}

class _Delivery extends StatelessWidget {
  const _Delivery({required this.order});
  final OrderDetailModel order;
  @override
  Widget build(BuildContext context) => Section(
    title: 'بيانات التوصيل',
    icon: Icons.location_on_outlined,
    child: Column(
      children: [
        Info(
          icon: Icons.business_outlined,
          title: order.branchName,
          subtitle: order.address,
        ),
        Info(
          icon: Icons.person_outline,
          title: order.receiverName,
          subtitle: order.receiverPhone,
        ),
        Info(
          icon: Icons.calendar_month_outlined,
          title: DateFormat(
            'EEEE، d MMMM yyyy',
            'ar',
          ).format(order.requiredDate),
          subtitle: order.timeSlot ?? shippingAr(order.shippingMethod),
        ),
        if (order.splitDelivery)
          const Info(
            icon: Icons.call_split_rounded,
            title: 'مسموح بتقسيم الطلب',
            subtitle: 'قد يصل في أكثر من شحنة',
          ),
      ],
    ),
  );
}

class _Company extends StatelessWidget {
  const _Company({required this.order});
  final OrderDetailModel order;
  @override
  Widget build(BuildContext context) {
    final rows = <Widget>[];
    if (order.costCenterCode != null) {
      rows.add(
        Info(
          icon: Icons.account_balance_wallet_outlined,
          title:
              '${order.costCenterName ?? 'مركز التكلفة'} (${order.costCenterCode})',
          subtitle: 'مركز التكلفة',
        ),
      );
    }
    if (order.projectCode != null) {
      rows.add(
        Info(
          icon: Icons.work_outline,
          title: '${order.projectName ?? 'المشروع'} (${order.projectCode})',
          subtitle: 'المشروع',
        ),
      );
    }
    if (order.purchaseOrder != null) {
      rows.add(
        Info(
          icon: Icons.description_outlined,
          title: order.purchaseOrder!,
          subtitle: 'أمر الشراء',
        ),
      );
    }
    if (order.department != null) {
      rows.add(
        Info(
          icon: Icons.groups_outlined,
          title: order.department!,
          subtitle: 'القسم الطالب',
        ),
      );
    }
    if (order.note != null) {
      rows.add(
        Info(icon: Icons.notes, title: order.note!, subtitle: 'ملاحظات'),
      );
    }
    return Section(
      title: 'بيانات الشركة والدفع',
      icon: Icons.apartment_outlined,
      child: rows.isEmpty
          ? Text(paymentAr(order.paymentMethod))
          : Column(children: rows),
    );
  }
}

class _Summary extends StatelessWidget {
  const _Summary({required this.order});
  final OrderDetailModel order;
  @override
  Widget build(BuildContext context) => Section(
    title: 'ملخص المبلغ',
    icon: Icons.receipt_long_outlined,
    child: Column(
      children: [
        Amount('قيمة المنتجات', order.subtotal),
        if (order.savings > 0) Amount('وفرت', -order.savings, green: true),
        if (order.discount > 0)
          Amount('خصم الكوبون', -order.discount, green: true),
        Amount('الضريبة شاملة', order.tax),
        Amount('الشحن', order.shipping),
        const Divider(),
        Amount('الإجمالي', order.total, total: true),
      ],
    ),
  );
}

class _Issues extends StatelessWidget {
  const _Issues({required this.items});
  final List<OrderIssueModel> items;
  @override
  Widget build(BuildContext context) => Section(
    title: 'بلاغات الطلب',
    icon: Icons.report_problem_outlined,
    child: Column(
      children: items
          .map(
            (i) => ListTile(
              contentPadding: EdgeInsets.zero,
              leading: const CircleAvatar(
                backgroundColor: AppColors.warningTint,
                child: Icon(
                  Icons.report_problem_outlined,
                  color: AppColors.warning,
                ),
              ),
              title: Text(
                issueAr(i.type),
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
              subtitle: Text(i.description, maxLines: 2),
              trailing: Text(
                issueStatusAr(i.status),
                style: const TextStyle(
                  fontSize: 10.5,
                  color: AppColors.primary,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          )
          .toList(),
    ),
  );
}

class _Actions extends ConsumerWidget {
  const _Actions({required this.order});
  final OrderDetailModel order;
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final repo = ref.read(orderRepositoryProvider);
    final shipped = [
      'Shipped',
      'OutForDelivery',
      'PartiallyDelivered',
      'Delivered',
      'Completed',
    ].contains(order.status);
    final delivered = ['Delivered', 'Completed'].contains(order.status);
    void refresh() {
      ref.invalidate(orderDetailProvider(order.id));
      ref.invalidate(orderListProvider);
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if ([
          'OutForDelivery',
          'PartiallyDelivered',
        ].contains(order.status)) ...[
          FilledButton.icon(
            onPressed: () => deliveryCode(context, ref, order),
            icon: const Icon(Icons.pin_outlined),
            label: const Text('تأكيد استلام الطلب'),
          ),
          const SizedBox(height: 8),
          OutlinedButton.icon(
            onPressed: () => proofDialog(context, repo, order.id, refresh),
            icon: const Icon(Icons.draw_outlined),
            label: const Text('رفع توقيع أو إثبات استلام'),
          ),
          const SizedBox(height: 8),
        ],
        if (shipped)
          OutlinedButton.icon(
            onPressed: () => issueDialog(context, repo, order, refresh),
            icon: const Icon(Icons.report_problem_outlined),
            label: const Text('الإبلاغ عن مشكلة في التوصيل'),
          ),
        if (delivered) ...[
          const SizedBox(height: 8),
          FilledButton.tonalIcon(
            onPressed: () => ratingDialog(context, repo, order, refresh),
            icon: const Icon(Icons.star_outline),
            label: Text(
              order.deliveryRating == null
                  ? 'قيّم الطلب والخدمة'
                  : 'تعديل التقييم',
            ),
          ),
        ],
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: () async {
            try {
              await repo.reorder(order.id);
              if (context.mounted) context.push('/cart');
            } catch (e) {
              if (context.mounted) message(context, e);
            }
          },
          icon: const Icon(Icons.replay),
          label: const Text('إعادة الطلب'),
        ),
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: () => scheduleDialog(context, repo, order.id, refresh),
          icon: const Icon(Icons.event_repeat),
          label: Text(
            order.scheduleCount == 0
                ? 'جدولة طلب متكرر'
                : 'إضافة جدول تكرار آخر',
          ),
        ),
        if (order.canCancel) ...[
          const SizedBox(height: 12),
          TextButton.icon(
            style: TextButton.styleFrom(foregroundColor: AppColors.error),
            onPressed: () => cancelDialog(context, repo, order.id, refresh),
            icon: const Icon(Icons.cancel_outlined),
            label: const Text('إلغاء الطلب'),
          ),
        ],
      ],
    );
  }
}

Future<void> deliveryCode(
  BuildContext context,
  WidgetRef ref,
  OrderDetailModel order,
) async {
  final repo = ref.read(orderRepositoryProvider),
      code = TextEditingController(),
      name = TextEditingController(text: order.receiverName);
  try {
    final result = await repo.requestCode(order.id);
    if (result.developmentCode != null) code.text = result.developmentCode!;
    if (!context.mounted) return;
    await showDialog(
      context: context,
      builder: (dialog) => AlertDialog(
        title: const Text('تأكيد الاستلام'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(
              Icons.verified_user_outlined,
              size: 48,
              color: AppColors.primary,
            ),
            const Text('أدخل رمز الاستلام المرسل للمستلم'),
            const SizedBox(height: 10),
            TextField(
              controller: code,
              maxLength: 6,
              keyboardType: TextInputType.number,
              textAlign: TextAlign.center,
              decoration: InputDecoration(
                labelText: 'رمز الاستلام',
                helperText: result.developmentCode == null
                    ? null
                    : 'رمز التطوير: ${result.developmentCode}',
              ),
            ),
            TextField(
              controller: name,
              decoration: const InputDecoration(labelText: 'اسم المستلم'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(dialog),
            child: const Text('لاحقًا'),
          ),
          FilledButton(
            onPressed: () async {
              try {
                await repo.confirmCode(order.id, code.text, name.text);
                ref.invalidate(orderDetailProvider(order.id));
                ref.invalidate(orderListProvider);
                if (!dialog.mounted) return;
                Navigator.pop(dialog);
              } catch (e) {
                message(dialog, e);
              }
            },
            child: const Text('تأكيد'),
          ),
        ],
      ),
    );
  } catch (e) {
    if (context.mounted) message(context, e);
  }
}

Future<void> cancelDialog(
  BuildContext context,
  OrderRepository repo,
  String id,
  VoidCallback done,
) async {
  final details = TextEditingController();
  String reason = 'تغير الاحتياج';
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (context, set) => Padding(
        padding: EdgeInsets.fromLTRB(
          18,
          0,
          18,
          MediaQuery.viewInsetsOf(context).bottom + 24,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'إلغاء الطلب',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
            ),
            const Text(
              'الإلغاء متاح قبل الشحن فقط',
              style: TextStyle(color: AppColors.gray500),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField(
              value: reason,
              decoration: const InputDecoration(labelText: 'السبب'),
              items: [
                'تغير الاحتياج',
                'تم الطلب بالخطأ',
                'تأخر التسليم',
                'سبب آخر',
              ].map((x) => DropdownMenuItem(value: x, child: Text(x))).toList(),
              onChanged: (v) => set(() => reason = v!),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: details,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'تفاصيل إضافية'),
            ),
            const SizedBox(height: 14),
            FilledButton(
              style: FilledButton.styleFrom(backgroundColor: AppColors.error),
              onPressed: () async {
                try {
                  await repo.cancel(id, reason, details.text);
                  done();
                  if (!sheet.mounted) return;
                  Navigator.pop(sheet);
                } catch (e) {
                  message(sheet, e);
                }
              },
              child: const Text('تأكيد الإلغاء'),
            ),
          ],
        ),
      ),
    ),
  );
}

Future<void> scheduleDialog(
  BuildContext context,
  OrderRepository repo,
  String id,
  VoidCallback done,
) async {
  String frequency = 'Monthly';
  int interval = 1;
  bool approval = true;
  await showModalBottomSheet(
    context: context,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (context, set) => Padding(
        padding: const EdgeInsets.fromLTRB(18, 0, 18, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'جدولة طلب متكرر',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField(
              value: frequency,
              decoration: const InputDecoration(labelText: 'التكرار'),
              items: const [
                DropdownMenuItem(value: 'Weekly', child: Text('أسبوعي')),
                DropdownMenuItem(value: 'Monthly', child: Text('شهري')),
                DropdownMenuItem(value: 'Quarterly', child: Text('ربع سنوي')),
              ],
              onChanged: (v) => set(() => frequency = v!),
            ),
            const SizedBox(height: 9),
            DropdownButtonFormField(
              value: interval,
              decoration: const InputDecoration(labelText: 'كل'),
              items: List.generate(
                6,
                (i) => DropdownMenuItem(
                  value: i + 1,
                  child: Text('${i + 1} فترة'),
                ),
              ),
              onChanged: (v) => set(() => interval = v!),
            ),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              value: approval,
              onChanged: (v) => set(() => approval = v),
              title: const Text('موافقة داخلية في كل مرة'),
            ),
            const SizedBox(height: 8),
            FilledButton(
              onPressed: () async {
                final days = frequency == 'Weekly'
                    ? 7
                    : frequency == 'Monthly'
                    ? 30
                    : 90;
                try {
                  await repo.schedule(
                    id,
                    frequency,
                    interval,
                    DateTime.now().add(Duration(days: days * interval)),
                    approval,
                  );
                  done();
                  if (!sheet.mounted) return;
                  Navigator.pop(sheet);
                } catch (e) {
                  message(sheet, e);
                }
              },
              child: const Text('حفظ الجدولة'),
            ),
          ],
        ),
      ),
    ),
  );
}

Future<void> proofDialog(
  BuildContext context,
  OrderRepository repo,
  String id,
  VoidCallback done,
) async {
  final picked = await FilePicker.pickFiles(
    type: FileType.custom,
    allowedExtensions: ['jpg', 'jpeg', 'png', 'pdf'],
    withData: true,
  );
  if (picked == null || !context.mounted) return;
  final name = TextEditingController();
  String type = picked.files.single.extension?.toLowerCase() == 'pdf'
      ? 'Document'
      : 'Signature';
  await showDialog(
    context: context,
    builder: (dialog) => AlertDialog(
      title: const Text('إثبات الاستلام'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          DropdownButtonFormField(
            value: type,
            decoration: const InputDecoration(labelText: 'نوع الإثبات'),
            items: const [
              DropdownMenuItem(value: 'Signature', child: Text('توقيع')),
              DropdownMenuItem(value: 'Photo', child: Text('صورة')),
              DropdownMenuItem(value: 'Document', child: Text('مستند')),
            ],
            onChanged: (v) => type = v!,
          ),
          const SizedBox(height: 9),
          TextField(
            controller: name,
            decoration: const InputDecoration(labelText: 'اسم المستلم'),
          ),
          const SizedBox(height: 7),
          Text(picked.files.single.name),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(dialog),
          child: const Text('إلغاء'),
        ),
        FilledButton(
          onPressed: () async {
            try {
              await repo.uploadProof(id, type, name.text, picked.files.single);
              done();
              if (!dialog.mounted) return;
              Navigator.pop(dialog);
            } catch (e) {
              message(dialog, e);
            }
          },
          child: const Text('رفع'),
        ),
      ],
    ),
  );
}

Future<void> issueDialog(
  BuildContext context,
  OrderRepository repo,
  OrderDetailModel order,
  VoidCallback done,
) async {
  String type = 'MissingItem';
  String? itemId;
  PlatformFile? photo;
  final description = TextEditingController(),
      quantity = TextEditingController(text: '1');
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (context, set) => Padding(
        padding: EdgeInsets.fromLTRB(
          18,
          0,
          18,
          MediaQuery.viewInsetsOf(context).bottom + 24,
        ),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text(
                'الإبلاغ عن مشكلة',
                style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 6,
                children:
                    {
                          'MissingItem': 'ناقص',
                          'WrongItem': 'خاطئ',
                          'DamagedItem': 'تالف',
                          'QuantityMismatch': 'كمية',
                        }.entries
                        .map(
                          (x) => ChoiceChip(
                            label: Text(x.value),
                            selected: type == x.key,
                            onSelected: (_) => set(() => type = x.key),
                          ),
                        )
                        .toList(),
              ),
              const SizedBox(height: 10),
              DropdownButtonFormField<String?>(
                value: itemId,
                decoration: const InputDecoration(labelText: 'الصنف المتأثر'),
                items: order.items
                    .map(
                      (x) => DropdownMenuItem(
                        value: x.id,
                        child: Text(x.name, overflow: TextOverflow.ellipsis),
                      ),
                    )
                    .toList(),
                onChanged: (v) => set(() => itemId = v),
              ),
              const SizedBox(height: 9),
              TextField(
                controller: quantity,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(labelText: 'الكمية'),
              ),
              const SizedBox(height: 9),
              TextField(
                controller: description,
                maxLines: 3,
                decoration: const InputDecoration(labelText: 'وصف المشكلة *'),
              ),
              const SizedBox(height: 9),
              OutlinedButton.icon(
                onPressed: () async {
                  final result = await FilePicker.pickFiles(
                    type: FileType.image,
                    withData: true,
                  );
                  if (result != null) set(() => photo = result.files.single);
                },
                icon: const Icon(Icons.add_a_photo_outlined),
                label: Text(photo?.name ?? 'إرفاق صورة'),
              ),
              const SizedBox(height: 12),
              FilledButton(
                onPressed: () async {
                  if (description.text.trim().isEmpty) return;
                  try {
                    await repo.issue(
                      order.id,
                      type,
                      itemId,
                      int.tryParse(quantity.text),
                      description.text,
                      photo,
                    );
                    done();
                    if (!sheet.mounted) return;
                    Navigator.pop(sheet);
                  } catch (e) {
                    message(sheet, e);
                  }
                },
                child: const Text('إرسال البلاغ'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

Future<void> ratingDialog(
  BuildContext context,
  OrderRepository repo,
  OrderDetailModel order,
  VoidCallback done,
) async {
  int delivery = order.deliveryRating ?? 5, service = order.serviceRating ?? 5;
  final comment = TextEditingController(text: order.ratingComment);
  await showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (sheet) => StatefulBuilder(
      builder: (context, set) => Padding(
        padding: EdgeInsets.fromLTRB(
          18,
          0,
          18,
          MediaQuery.viewInsetsOf(context).bottom + 24,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'قيّم تجربتك',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 12),
            const Text('التوصيل', textAlign: TextAlign.center),
            RatingInput(
              value: delivery,
              changed: (v) => set(() => delivery = v),
            ),
            const Text('الخدمة', textAlign: TextAlign.center),
            RatingInput(value: service, changed: (v) => set(() => service = v)),
            TextField(
              controller: comment,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'اكتب رأيك'),
            ),
            const SizedBox(height: 12),
            FilledButton(
              onPressed: () async {
                try {
                  await repo.rate(order.id, delivery, service, comment.text);
                  done();
                  if (!sheet.mounted) return;
                  Navigator.pop(sheet);
                } catch (e) {
                  message(sheet, e);
                }
              },
              child: const Text('إرسال التقييم'),
            ),
          ],
        ),
      ),
    ),
  );
}

void message(BuildContext context, Object error) => ScaffoldMessenger.of(
  context,
).showSnackBar(SnackBar(content: Text('$error')));

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
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(AppRadius.xl),
      border: Border.all(color: AppColors.gray150),
      boxShadow: AppShadows.soft,
    ),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, color: AppColors.primary, size: 19),
              const SizedBox(width: 7),
              Text(
                title,
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w700,
                ),
              ),
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
  const Info({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
  });
  final IconData icon;
  final String title, subtitle;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 11),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        CircleAvatar(
          radius: 17,
          backgroundColor: AppColors.primaryTint,
          child: Icon(icon, size: 17, color: AppColors.primary),
        ),
        const SizedBox(width: 9),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
              Text(
                subtitle,
                style: const TextStyle(
                  fontSize: 10.5,
                  color: AppColors.gray500,
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}

class Amount extends StatelessWidget {
  const Amount(
    this.label,
    this.value, {
    this.green = false,
    this.total = false,
    super.key,
  });
  final String label;
  final double value;
  final bool green, total;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      children: [
        Expanded(
          child: Text(
            label,
            style: TextStyle(
              fontWeight: total ? FontWeight.w700 : FontWeight.normal,
            ),
          ),
        ),
        Text(
          '${value < 0 ? '-' : ''}${money(value.abs())} ج.م',
          style: TextStyle(
            fontSize: total ? 17 : 11,
            fontWeight: FontWeight.w700,
            color: green
                ? AppColors.success
                : total
                ? AppColors.primary
                : AppColors.gray800,
          ),
        ),
      ],
    ),
  );
}

class RatingInput extends StatelessWidget {
  const RatingInput({super.key, required this.value, required this.changed});
  final int value;
  final ValueChanged<int> changed;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisAlignment: MainAxisAlignment.center,
    children: List.generate(
      5,
      (i) => IconButton(
        onPressed: () => changed(i + 1),
        icon: Icon(
          i < value ? Icons.star : Icons.star_border,
          color: AppColors.warning,
          size: 29,
        ),
      ),
    ),
  );
}

class _Stars extends StatelessWidget {
  const _Stars({required this.value});
  final int value;
  @override
  Widget build(BuildContext context) => Row(
    mainAxisSize: MainAxisSize.min,
    children: List.generate(
      5,
      (i) => Icon(
        i < value ? Icons.star : Icons.star_border,
        size: 12,
        color: AppColors.warning,
      ),
    ),
  );
}

class _Status extends StatelessWidget {
  const _Status({required this.status});
  final String status;
  @override
  Widget build(BuildContext context) {
    final color = statusColor(status);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        statusAr(status),
        style: TextStyle(
          fontSize: 9.5,
          fontWeight: FontWeight.w700,
          color: color,
        ),
      ),
    );
  }
}

class _Empty extends StatelessWidget {
  const _Empty();
  @override
  Widget build(BuildContext context) => const Center(
    child: Padding(
      padding: EdgeInsets.all(30),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.receipt_long_outlined, size: 70, color: AppColors.gray300),
          SizedBox(height: 12),
          Text(
            'لا توجد طلبات بعد',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
          ),
          Text(
            'عند إتمام أول طلب سيظهر هنا ويمكنك متابعة كل تفاصيله',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.gray500),
          ),
        ],
      ),
    ),
  );
}

class _Error extends StatelessWidget {
  const _Error({required this.error, required this.retry});
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
            size: 50,
            color: AppColors.error,
          ),
          const SizedBox(height: 8),
          Text('$error', textAlign: TextAlign.center),
          const SizedBox(height: 8),
          OutlinedButton(onPressed: retry, child: const Text('إعادة المحاولة')),
        ],
      ),
    ),
  );
}

String money(double value) => NumberFormat('#,##0.##', 'ar').format(value);
String statusAr(String s) =>
    const {
      'PendingApproval': 'بانتظار الموافقة',
      'Confirmed': 'تم التأكيد',
      'Processing': 'قيد المراجعة',
      'Picking': 'جاري جمع المنتجات',
      'Packing': 'جاهز للشحن',
      'Shipped': 'تم الشحن',
      'OutForDelivery': 'خرج للتوصيل',
      'PartiallyDelivered': 'تسليم جزئي',
      'Delivered': 'تم التسليم',
      'Completed': 'مكتمل',
      'Delayed': 'متأخر',
      'Cancelled': 'ملغي',
      'Created': 'تم إنشاء الشحنة',
    }[s] ??
    s;
Color statusColor(String s) => switch (s) {
  'Delivered' || 'Completed' => AppColors.success,
  'Cancelled' => AppColors.error,
  'Delayed' || 'PendingApproval' => AppColors.warning,
  _ => AppColors.primary,
};
String shippingAr(String s) =>
    const {
      'Standard': 'شحن عادي',
      'Express': 'شحن سريع',
      'Pickup': 'استلام من الفرع',
    }[s] ??
    s;
String paymentAr(String s) =>
    const {
      'CreditLine': 'حد ائتماني',
      'BankTransfer': 'تحويل بنكي',
      'CashOnDelivery': 'الدفع عند الاستلام',
      'MonthlyInvoice': 'فاتورة شهرية',
      'Card': 'بطاقة بنكية',
      'Partial': 'دفع جزئي',
    }[s] ??
    s;
String issueAr(String s) =>
    const {
      'MissingItem': 'صنف ناقص',
      'WrongItem': 'صنف خاطئ',
      'DamagedItem': 'صنف تالف',
      'QuantityMismatch': 'اختلاف في الكمية',
      'Other': 'مشكلة أخرى',
    }[s] ??
    s;
String issueStatusAr(String s) =>
    const {
      'Open': 'قيد المراجعة',
      'UnderReview': 'جاري الحل',
      'Resolved': 'تم الحل',
      'Rejected': 'مرفوض',
    }[s] ??
    s;
