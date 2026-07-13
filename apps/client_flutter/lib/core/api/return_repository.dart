import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

class EligibleReturnItem {
  const EligibleReturnItem({
    required this.id,
    required this.sku,
    required this.name,
    required this.ordered,
    required this.returned,
    required this.eligible,
    required this.unitPrice,
  });
  factory EligibleReturnItem.fromJson(Map<String, dynamic> j) =>
      EligibleReturnItem(
        id: j['orderItemId'] as String,
        sku: j['sku'] as String,
        name: j['nameAr'] as String,
        ordered: j['orderedQuantity'] as int,
        returned: j['returnedQuantity'] as int,
        eligible: j['eligibleQuantity'] as int,
        unitPrice: (j['unitPrice'] as num).toDouble(),
      );
  final String id, sku, name;
  final int ordered, returned, eligible;
  final double unitPrice;
}

class EligibleReturnOrder {
  const EligibleReturnOrder({
    required this.id,
    required this.number,
    required this.deliveredAt,
    required this.eligibleUntil,
    required this.address,
    required this.items,
  });
  factory EligibleReturnOrder.fromJson(Map<String, dynamic> j) =>
      EligibleReturnOrder(
        id: j['orderId'] as String,
        number: j['number'] as String,
        deliveredAt: DateTime.parse(j['deliveredAt'] as String),
        eligibleUntil: DateTime.parse(j['eligibleUntil'] as String),
        address: j['deliveryAddress'] as String,
        items: (j['items'] as List)
            .map((e) => EligibleReturnItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
  final String id, number, address;
  final DateTime deliveredAt, eligibleUntil;
  final List<EligibleReturnItem> items;
}

class ReturnListItem {
  const ReturnListItem({
    required this.id,
    required this.number,
    required this.orderNumber,
    required this.status,
    required this.resolution,
    required this.requestedTotal,
    required this.approvedTotal,
    required this.itemCount,
    required this.createdAt,
    required this.pickupAt,
  });
  factory ReturnListItem.fromJson(Map<String, dynamic> j) => ReturnListItem(
    id: j['id'] as String,
    number: j['number'] as String,
    orderNumber: j['orderNumber'] as String,
    status: j['status'] as String,
    resolution: j['resolution'] as String,
    requestedTotal: (j['requestedTotal'] as num).toDouble(),
    approvedTotal: (j['approvedTotal'] as num?)?.toDouble(),
    itemCount: j['itemCount'] as int,
    createdAt: DateTime.parse(j['createdAt'] as String),
    pickupAt: j['pickupAt'] == null
        ? null
        : DateTime.parse(j['pickupAt'] as String),
  );
  final String id, number, orderNumber, status, resolution;
  final double requestedTotal;
  final double? approvedTotal;
  final int itemCount;
  final DateTime createdAt;
  final DateTime? pickupAt;
}

class ReturnLineModel {
  const ReturnLineModel({
    required this.id,
    required this.orderItemId,
    required this.sku,
    required this.name,
    required this.quantity,
    required this.reason,
    required this.description,
    required this.unitRefund,
    required this.total,
    required this.eligible,
    required this.eligibilityNote,
    required this.inspectionPassed,
  });
  factory ReturnLineModel.fromJson(Map<String, dynamic> j) => ReturnLineModel(
    id: j['id'] as String,
    orderItemId: j['orderItemId'] as String,
    sku: j['sku'] as String,
    name: j['nameAr'] as String,
    quantity: j['quantity'] as int,
    reason: j['reason'] as String,
    description: j['description'] as String?,
    unitRefund: (j['unitRefund'] as num).toDouble(),
    total: (j['lineRefund'] as num).toDouble(),
    eligible: j['isEligible'] as bool,
    eligibilityNote: j['eligibilityNote'] as String?,
    inspectionPassed: j['inspectionPassed'] as bool?,
  );
  final String id, orderItemId, sku, name, reason;
  final String? description, eligibilityNote;
  final int quantity;
  final double unitRefund, total;
  final bool eligible;
  final bool? inspectionPassed;
}

class ReturnHistoryModel {
  const ReturnHistoryModel(this.status, this.note, this.at);
  factory ReturnHistoryModel.fromJson(Map<String, dynamic> j) =>
      ReturnHistoryModel(
        j['status'] as String,
        j['note'] as String?,
        DateTime.parse(j['at'] as String),
      );
  final String status;
  final String? note;
  final DateTime at;
}

class ReturnDetailModel {
  const ReturnDetailModel({
    required this.id,
    required this.number,
    required this.orderId,
    required this.orderNumber,
    required this.status,
    required this.resolution,
    required this.refundMethod,
    required this.requestedTotal,
    required this.approvedTotal,
    required this.pickupAddress,
    required this.pickupAt,
    required this.pickupWindow,
    required this.driverName,
    required this.driverPhone,
    required this.latitude,
    required this.longitude,
    required this.rejectionReason,
    required this.inspectionNotes,
    required this.submittedAt,
    required this.receivedAt,
    required this.completedAt,
    required this.items,
    required this.history,
    required this.attachmentCount,
    required this.canEdit,
    required this.canCancel,
    required this.canTrack,
  });
  factory ReturnDetailModel.fromJson(Map<String, dynamic> j) =>
      ReturnDetailModel(
        id: j['id'] as String,
        number: j['number'] as String,
        orderId: j['orderId'] as String,
        orderNumber: j['orderNumber'] as String,
        status: j['status'] as String,
        resolution: j['resolution'] as String,
        refundMethod: j['refundMethod'] as String?,
        requestedTotal: (j['requestedTotal'] as num).toDouble(),
        approvedTotal: (j['approvedTotal'] as num?)?.toDouble(),
        pickupAddress: j['pickupAddress'] as String,
        pickupAt: j['pickupAt'] == null
            ? null
            : DateTime.parse(j['pickupAt'] as String),
        pickupWindow: j['pickupWindow'] as String?,
        driverName: j['pickupDriverName'] as String?,
        driverPhone: j['pickupDriverPhone'] as String?,
        latitude: (j['pickupLatitude'] as num?)?.toDouble(),
        longitude: (j['pickupLongitude'] as num?)?.toDouble(),
        rejectionReason: j['rejectionReason'] as String?,
        inspectionNotes: j['inspectionNotes'] as String?,
        submittedAt: j['submittedAt'] == null
            ? null
            : DateTime.parse(j['submittedAt'] as String),
        receivedAt: j['receivedAt'] == null
            ? null
            : DateTime.parse(j['receivedAt'] as String),
        completedAt: j['completedAt'] == null
            ? null
            : DateTime.parse(j['completedAt'] as String),
        items: (j['items'] as List)
            .map((e) => ReturnLineModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        history: (j['history'] as List)
            .map((e) => ReturnHistoryModel.fromJson(e as Map<String, dynamic>))
            .toList(),
        attachmentCount: (j['attachments'] as List).length,
        canEdit: j['canEdit'] as bool,
        canCancel: j['canCancel'] as bool,
        canTrack: j['canTrackPickup'] as bool,
      );
  final String id,
      number,
      orderId,
      orderNumber,
      status,
      resolution,
      pickupAddress;
  final String? refundMethod,
      pickupWindow,
      driverName,
      driverPhone,
      rejectionReason,
      inspectionNotes;
  final double requestedTotal;
  final double? approvedTotal, latitude, longitude;
  final DateTime? pickupAt, submittedAt, receivedAt, completedAt;
  final List<ReturnLineModel> items;
  final List<ReturnHistoryModel> history;
  final int attachmentCount;
  final bool canEdit, canCancel, canTrack;
}

class ReturnItemInput {
  const ReturnItemInput(
    this.orderItemId,
    this.quantity,
    this.reason,
    this.description,
  );
  final String orderItemId, reason;
  final int quantity;
  final String? description;
  Map<String, dynamic> toJson() => {
    'orderItemId': orderItemId,
    'quantity': quantity,
    'reason': reason,
    'description': description,
  };
}

class ReturnRepository {
  ReturnRepository(this._api);
  final ApiClient _api;
  Future<List<EligibleReturnOrder>> eligible() async =>
      ((await _api.dio.get('/api/returns/eligible-orders')).data as List)
          .map((e) => EligibleReturnOrder.fromJson(e as Map<String, dynamic>))
          .toList();
  Future<List<ReturnListItem>> list([String? status]) async =>
      ((await _api.dio.get(
                '/api/returns',
                queryParameters: {if (status != null) 'status': status},
              )).data
              as List)
          .map((e) => ReturnListItem.fromJson(e as Map<String, dynamic>))
          .toList();
  Future<ReturnDetailModel> detail(String id) async =>
      ReturnDetailModel.fromJson(
        (await _api.dio.get('/api/returns/$id')).data as Map<String, dynamic>,
      );
  Future<ReturnDetailModel> create(
    String orderId,
    String resolution,
    String? refundMethod,
    String address,
    List<ReturnItemInput> items,
  ) async => ReturnDetailModel.fromJson(
    (await _api.dio.post(
          '/api/returns',
          data: {
            'orderId': orderId,
            'resolution': resolution,
            'refundMethod': refundMethod,
            'pickupAddress': address,
            'items': items.map((e) => e.toJson()).toList(),
          },
        )).data
        as Map<String, dynamic>,
  );
  Future<void> upload(String id, PlatformFile file) async {
    final upload = file.bytes != null
        ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
        : await MultipartFile.fromFile(file.path!, filename: file.name);
    await _api.dio.post(
      '/api/returns/$id/attachments',
      data: FormData.fromMap({'file': upload}),
    );
  }

  Future<ReturnDetailModel> submit(String id) async =>
      ReturnDetailModel.fromJson(
        (await _api.dio.post('/api/returns/$id/submit')).data
            as Map<String, dynamic>,
      );
  Future<ReturnDetailModel> cancel(String id) async =>
      ReturnDetailModel.fromJson(
        (await _api.dio.post('/api/returns/$id/cancel')).data
            as Map<String, dynamic>,
      );
}

final returnRepositoryProvider = Provider(
  (ref) => ReturnRepository(ref.watch(apiClientProvider)),
);
final returnListProvider = FutureProvider.family<List<ReturnListItem>, String?>(
  (ref, status) => ref.watch(returnRepositoryProvider).list(status),
);
final eligibleReturnsProvider = FutureProvider(
  (ref) => ref.watch(returnRepositoryProvider).eligible(),
);
final returnDetailProvider = FutureProvider.family<ReturnDetailModel, String>(
  (ref, id) => ref.watch(returnRepositoryProvider).detail(id),
);
