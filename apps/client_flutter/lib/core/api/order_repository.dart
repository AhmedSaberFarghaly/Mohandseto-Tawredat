import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

class OrderQuery {
  const OrderQuery({this.search, this.status});
  final String? search, status;
  @override
  bool operator ==(Object other) =>
      other is OrderQuery && other.search == search && other.status == status;
  @override
  int get hashCode => Object.hash(search, status);
}

class OrderListItem {
  const OrderListItem({
    required this.id,
    required this.number,
    required this.status,
    required this.total,
    required this.itemCount,
    required this.requiredDate,
    required this.createdAt,
    required this.canCancel,
    required this.canTrack,
  });
  factory OrderListItem.fromJson(Map<String, dynamic> j) => OrderListItem(
    id: j['id'] as String,
    number: j['number'] as String,
    status: j['status'] as String,
    total: (j['total'] as num).toDouble(),
    itemCount: j['itemCount'] as int,
    requiredDate: DateTime.parse(j['requiredDate'] as String),
    createdAt: DateTime.parse(j['createdAt'] as String),
    canCancel: j['canCancel'] as bool,
    canTrack: j['canTrack'] as bool,
  );
  final String id, number, status;
  final double total;
  final int itemCount;
  final DateTime requiredDate, createdAt;
  final bool canCancel, canTrack;
}

class OrderLineModel {
  const OrderLineModel({
    required this.id,
    required this.productId,
    required this.sku,
    required this.name,
    required this.variant,
    required this.quantity,
    required this.unitPrice,
    required this.total,
    required this.note,
    required this.rating,
  });
  factory OrderLineModel.fromJson(Map<String, dynamic> j) => OrderLineModel(
    id: j['id'] as String,
    productId: j['productId'] as String,
    sku: j['sku'] as String,
    name: j['nameAr'] as String,
    variant: j['variantName'] as String?,
    quantity: j['quantity'] as int,
    unitPrice: (j['unitPrice'] as num).toDouble(),
    total: (j['lineTotal'] as num).toDouble(),
    note: j['customerNote'] as String?,
    rating: j['rating'] as int?,
  );
  final String id, productId, sku, name;
  final String? variant, note;
  final int quantity;
  final double unitPrice, total;
  final int? rating;
}

class OrderHistoryModel {
  const OrderHistoryModel({
    required this.status,
    required this.note,
    required this.at,
  });
  factory OrderHistoryModel.fromJson(Map<String, dynamic> j) =>
      OrderHistoryModel(
        status: j['status'] as String,
        note: j['note'] as String?,
        at: DateTime.parse(j['at'] as String),
      );
  final String status;
  final String? note;
  final DateTime at;
}

class ShipmentEventModel {
  const ShipmentEventModel({
    required this.status,
    required this.description,
    required this.location,
    required this.latitude,
    required this.longitude,
    required this.at,
  });
  factory ShipmentEventModel.fromJson(Map<String, dynamic> j) =>
      ShipmentEventModel(
        status: j['status'] as String,
        description: j['descriptionAr'] as String,
        location: j['location'] as String?,
        latitude: (j['latitude'] as num?)?.toDouble(),
        longitude: (j['longitude'] as num?)?.toDouble(),
        at: DateTime.parse(j['at'] as String),
      );
  final String status, description;
  final String? location;
  final double? latitude, longitude;
  final DateTime at;
}

class ShipmentModel {
  const ShipmentModel({
    required this.id,
    required this.number,
    required this.carrier,
    required this.trackingNumber,
    required this.status,
    required this.driverName,
    required this.driverPhone,
    required this.latitude,
    required this.longitude,
    required this.eta,
    required this.deliveredAt,
    required this.events,
  });
  factory ShipmentModel.fromJson(Map<String, dynamic> j) => ShipmentModel(
    id: j['id'] as String,
    number: j['number'] as String,
    carrier: j['carrierName'] as String,
    trackingNumber: j['trackingNumber'] as String?,
    status: j['status'] as String,
    driverName: j['driverName'] as String?,
    driverPhone: j['driverPhone'] as String?,
    latitude: (j['driverLatitude'] as num?)?.toDouble(),
    longitude: (j['driverLongitude'] as num?)?.toDouble(),
    eta: j['estimatedArrival'] == null
        ? null
        : DateTime.parse(j['estimatedArrival'] as String),
    deliveredAt: j['deliveredAt'] == null
        ? null
        : DateTime.parse(j['deliveredAt'] as String),
    events: (j['events'] as List)
        .map((e) => ShipmentEventModel.fromJson(e as Map<String, dynamic>))
        .toList(),
  );
  final String id, number, carrier, status;
  final String? trackingNumber, driverName, driverPhone;
  final double? latitude, longitude;
  final DateTime? eta, deliveredAt;
  final List<ShipmentEventModel> events;
}

class OrderIssueModel {
  const OrderIssueModel({
    required this.id,
    required this.itemId,
    required this.type,
    required this.quantity,
    required this.description,
    required this.status,
    required this.createdAt,
    required this.hasPhoto,
  });
  factory OrderIssueModel.fromJson(Map<String, dynamic> j) => OrderIssueModel(
    id: j['id'] as String,
    itemId: j['orderItemId'] as String?,
    type: j['type'] as String,
    quantity: j['affectedQuantity'] as int?,
    description: j['description'] as String,
    status: j['status'] as String,
    createdAt: DateTime.parse(j['createdAt'] as String),
    hasPhoto: j['hasPhoto'] as bool,
  );
  final String id, type, description, status;
  final String? itemId;
  final int? quantity;
  final DateTime createdAt;
  final bool hasPhoto;
}

class OrderDetailModel {
  const OrderDetailModel({
    required this.id,
    required this.number,
    required this.status,
    required this.subtotal,
    required this.savings,
    required this.discount,
    required this.tax,
    required this.shipping,
    required this.total,
    required this.branchName,
    required this.address,
    required this.receiverName,
    required this.receiverPhone,
    required this.requiredDate,
    required this.timeSlot,
    required this.shippingMethod,
    required this.paymentMethod,
    required this.purchaseOrder,
    required this.internalReference,
    required this.costCenterCode,
    required this.costCenterName,
    required this.projectCode,
    required this.projectName,
    required this.department,
    required this.note,
    required this.splitDelivery,
    required this.requiresApproval,
    required this.canCancel,
    required this.canTrack,
    required this.items,
    required this.history,
    required this.shipments,
    required this.issues,
    required this.deliveryRating,
    required this.serviceRating,
    required this.ratingComment,
    required this.scheduleCount,
    required this.proofCount,
  });
  factory OrderDetailModel.fromJson(Map<String, dynamic> j) {
    final rating = j['rating'] as Map<String, dynamic>?;
    return OrderDetailModel(
      id: j['id'] as String,
      number: j['number'] as String,
      status: j['status'] as String,
      subtotal: (j['subtotal'] as num).toDouble(),
      savings: (j['savings'] as num).toDouble(),
      discount: (j['couponDiscount'] as num).toDouble(),
      tax: (j['taxIncluded'] as num).toDouble(),
      shipping: (j['shipping'] as num).toDouble(),
      total: (j['total'] as num).toDouble(),
      branchName: j['branchName'] as String,
      address: j['deliveryAddress'] as String,
      receiverName: j['receiverName'] as String,
      receiverPhone: j['receiverPhone'] as String,
      requiredDate: DateTime.parse(j['requiredDate'] as String),
      timeSlot: j['timeSlot'] as String?,
      shippingMethod: j['shippingMethod'] as String,
      paymentMethod: j['paymentMethod'] as String,
      purchaseOrder: j['purchaseOrderNumber'] as String?,
      internalReference: j['internalReference'] as String?,
      costCenterCode: j['costCenterCode'] as String?,
      costCenterName: j['costCenterName'] as String?,
      projectCode: j['projectCode'] as String?,
      projectName: j['projectName'] as String?,
      department: j['requestingDepartment'] as String?,
      note: j['orderNote'] as String?,
      splitDelivery: j['allowSplitDelivery'] as bool,
      requiresApproval: j['requiresApproval'] as bool,
      canCancel: j['canCancel'] as bool,
      canTrack: j['canTrack'] as bool,
      items: (j['items'] as List)
          .map((e) => OrderLineModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      history: (j['history'] as List)
          .map((e) => OrderHistoryModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      shipments: (j['shipments'] as List)
          .map((e) => ShipmentModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      issues: (j['issues'] as List)
          .map((e) => OrderIssueModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      deliveryRating: rating?['deliveryRating'] as int?,
      serviceRating: rating?['serviceRating'] as int?,
      ratingComment: rating?['comment'] as String?,
      scheduleCount: (j['schedules'] as List).length,
      proofCount: (j['proofs'] as List).length,
    );
  }
  final String id,
      number,
      status,
      branchName,
      address,
      receiverName,
      receiverPhone,
      shippingMethod,
      paymentMethod;
  final String? timeSlot,
      purchaseOrder,
      internalReference,
      costCenterCode,
      costCenterName,
      projectCode,
      projectName,
      department,
      note,
      ratingComment;
  final double subtotal, savings, discount, tax, shipping, total;
  final DateTime requiredDate;
  final bool splitDelivery, requiresApproval, canCancel, canTrack;
  final List<OrderLineModel> items;
  final List<OrderHistoryModel> history;
  final List<ShipmentModel> shipments;
  final List<OrderIssueModel> issues;
  final int? deliveryRating, serviceRating;
  final int scheduleCount, proofCount;
}

class DeliveryCodeModel {
  const DeliveryCodeModel(this.expiresAt, this.developmentCode);
  factory DeliveryCodeModel.fromJson(Map<String, dynamic> j) =>
      DeliveryCodeModel(
        DateTime.parse(j['expiresAt'] as String),
        j['developmentCode'] as String?,
      );
  final DateTime expiresAt;
  final String? developmentCode;
}

class OrderRepository {
  OrderRepository(this._api);
  final ApiClient _api;
  Future<List<OrderListItem>> list(OrderQuery query) async =>
      ((await _api.dio.get(
                '/api/orders',
                queryParameters: {
                  if (query.search?.isNotEmpty == true) 'search': query.search,
                  if (query.status != null) 'status': query.status,
                },
              )).data
              as List)
          .map((e) => OrderListItem.fromJson(e as Map<String, dynamic>))
          .toList();
  Future<OrderDetailModel> detail(String id) async => OrderDetailModel.fromJson(
    (await _api.dio.get('/api/orders/$id')).data as Map<String, dynamic>,
  );
  Future<OrderDetailModel> cancel(
    String id,
    String reason,
    String? details,
  ) async => OrderDetailModel.fromJson(
    (await _api.dio.post(
          '/api/orders/$id/cancel',
          data: {'reason': reason, 'details': details},
        )).data
        as Map<String, dynamic>,
  );
  Future<Map<String, dynamic>> reorder(String id) async =>
      (await _api.dio.post('/api/orders/$id/reorder')).data
          as Map<String, dynamic>;
  Future<void> schedule(
    String id,
    String frequency,
    int interval,
    DateTime next,
    bool approval,
  ) async => _api.dio.post(
    '/api/orders/$id/recurring',
    data: {
      'frequency': frequency,
      'interval': interval,
      'nextRunAt': next.toUtc().toIso8601String(),
      'requireApprovalEachRun': approval,
    },
  );
  Future<DeliveryCodeModel> requestCode(String id) async =>
      DeliveryCodeModel.fromJson(
        (await _api.dio.post('/api/orders/$id/delivery-code')).data
            as Map<String, dynamic>,
      );
  Future<OrderDetailModel> confirmCode(
    String id,
    String code,
    String recipient,
  ) async => OrderDetailModel.fromJson(
    (await _api.dio.post(
          '/api/orders/$id/confirm-delivery',
          data: {'code': code, 'recipientName': recipient},
        )).data
        as Map<String, dynamic>,
  );
  Future<void> rate(
    String id,
    int delivery,
    int service,
    String? comment,
  ) async => _api.dio.put(
    '/api/orders/$id/rating',
    data: {
      'deliveryRating': delivery,
      'serviceRating': service,
      'comment': comment,
    },
  );
  Future<void> rateItem(
    String id,
    String itemId,
    int rating,
    String? comment,
  ) async => _api.dio.put(
    '/api/orders/$id/items/$itemId/rating',
    data: {'rating': rating, 'comment': comment},
  );
  Future<void> issue(
    String id,
    String type,
    String? itemId,
    int? quantity,
    String description,
    PlatformFile? file,
  ) async {
    MultipartFile? upload;
    if (file != null) {
      upload = file.bytes != null
          ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
          : await MultipartFile.fromFile(file.path!, filename: file.name);
    }
    await _api.dio.post(
      '/api/orders/$id/issues',
      data: FormData.fromMap({
        'type': type,
        if (itemId != null) 'orderItemId': itemId,
        if (quantity != null) 'affectedQuantity': quantity,
        'description': description,
        if (upload != null) 'photo': upload,
      }),
    );
  }

  Future<void> uploadProof(
    String id,
    String type,
    String recipient,
    PlatformFile file,
  ) async {
    final upload = file.bytes != null
        ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
        : await MultipartFile.fromFile(file.path!, filename: file.name);
    await _api.dio.post(
      '/api/orders/$id/proofs',
      data: FormData.fromMap({
        'type': type,
        'recipientName': recipient,
        'file': upload,
      }),
    );
  }
}

final orderRepositoryProvider = Provider(
  (ref) => OrderRepository(ref.watch(apiClientProvider)),
);
final orderListProvider =
    FutureProvider.family<List<OrderListItem>, OrderQuery>(
      (ref, query) => ref.watch(orderRepositoryProvider).list(query),
    );
final orderDetailProvider = FutureProvider.family<OrderDetailModel, String>(
  (ref, id) => ref.watch(orderRepositoryProvider).detail(id),
);
