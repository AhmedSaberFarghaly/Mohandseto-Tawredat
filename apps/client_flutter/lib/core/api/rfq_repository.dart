import 'dart:typed_data';
import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';

class RfqListItem {
  const RfqListItem({
    required this.id,
    required this.number,
    required this.title,
    required this.status,
    required this.itemCount,
    required this.requiredDate,
    required this.deadline,
    required this.latestTotal,
    required this.createdAt,
  });
  factory RfqListItem.fromJson(Map<String, dynamic> j) => RfqListItem(
    id: j['id'] as String,
    number: j['number'] as String,
    title: j['title'] as String,
    status: j['status'] as String,
    itemCount: j['itemCount'] as int,
    requiredDate: DateTime.parse(j['requiredDate'] as String),
    deadline: DateTime.parse(j['quoteDeadline'] as String),
    latestTotal: (j['latestQuoteTotal'] as num?)?.toDouble(),
    createdAt: DateTime.parse(j['createdAt'] as String),
  );
  final String id, number, title, status;
  final int itemCount;
  final DateTime requiredDate, deadline, createdAt;
  final double? latestTotal;
}

class RfqItemModel {
  const RfqItemModel({
    required this.id,
    required this.productId,
    required this.description,
    required this.quantity,
    required this.unit,
    required this.specifications,
    required this.brand,
    required this.alternatives,
    required this.source,
    required this.confidence,
    required this.reviewed,
  });
  factory RfqItemModel.fromJson(Map<String, dynamic> j) => RfqItemModel(
    id: j['id'] as String,
    productId: j['productId'] as String?,
    description: j['description'] as String,
    quantity: (j['quantity'] as num).toDouble(),
    unit: j['unitName'] as String,
    specifications: j['specifications'] as String?,
    brand: j['preferredBrand'] as String?,
    alternatives: j['allowAlternatives'] as bool,
    source: j['source'] as String,
    confidence: (j['confidence'] as num).toDouble(),
    reviewed: j['isReviewed'] as bool,
  );
  final String id, description, unit, source;
  final String? productId, specifications, brand;
  final double quantity, confidence;
  final bool alternatives, reviewed;
}

class RfqFileModel {
  const RfqFileModel({
    required this.id,
    required this.name,
    required this.type,
    required this.status,
    required this.error,
  });
  factory RfqFileModel.fromJson(Map<String, dynamic> j) => RfqFileModel(
    id: j['id'] as String,
    name: j['name'] as String,
    type: j['type'] as String,
    status: j['extractionStatus'] as String,
    error: j['error'] as String?,
  );
  final String id, name, type, status;
  final String? error;
}

class QuoteItemModel {
  const QuoteItemModel({
    required this.id,
    required this.rfqItemId,
    required this.productId,
    required this.description,
    required this.quantity,
    required this.unit,
    required this.unitPrice,
    required this.total,
    required this.alternative,
    required this.reason,
  });
  factory QuoteItemModel.fromJson(Map<String, dynamic> j) => QuoteItemModel(
    id: j['id'] as String,
    rfqItemId: j['rfqItemId'] as String,
    productId: j['productId'] as String?,
    description: j['description'] as String,
    quantity: (j['quantity'] as num).toDouble(),
    unit: j['unitName'] as String,
    unitPrice: (j['unitPrice'] as num).toDouble(),
    total: (j['lineTotal'] as num).toDouble(),
    alternative: j['isAlternative'] as bool,
    reason: j['alternativeReason'] as String?,
  );
  final String id, rfqItemId, description, unit;
  final String? productId, reason;
  final double quantity, unitPrice, total;
  final bool alternative;
}

class QuoteVersionModel {
  const QuoteVersionModel({
    required this.id,
    required this.version,
    required this.subtotal,
    required this.tax,
    required this.shipping,
    required this.total,
    required this.validUntil,
    required this.deliveryDays,
    required this.terms,
    required this.summary,
    required this.sentAt,
    required this.items,
    required this.expired,
  });
  factory QuoteVersionModel.fromJson(Map<String, dynamic> j) =>
      QuoteVersionModel(
        id: j['id'] as String,
        version: j['version'] as int,
        subtotal: (j['subtotal'] as num).toDouble(),
        tax: (j['tax'] as num).toDouble(),
        shipping: (j['shipping'] as num).toDouble(),
        total: (j['total'] as num).toDouble(),
        validUntil: DateTime.parse(j['validUntil'] as String),
        deliveryDays: j['deliveryDays'] as int,
        terms: j['terms'] as String?,
        summary: j['changeSummary'] as String?,
        sentAt: DateTime.parse(j['sentAt'] as String),
        items: (j['items'] as List)
            .map((x) => QuoteItemModel.fromJson(x as Map<String, dynamic>))
            .toList(),
        expired: j['isExpired'] as bool,
      );
  final String id;
  final int version, deliveryDays;
  final double subtotal, tax, shipping, total;
  final DateTime validUntil, sentAt;
  final String? terms, summary;
  final List<QuoteItemModel> items;
  final bool expired;
}

class NegotiationModel {
  const NegotiationModel({
    required this.id,
    required this.staff,
    required this.type,
    required this.message,
    required this.proposed,
    required this.createdAt,
  });
  factory NegotiationModel.fromJson(Map<String, dynamic> j) => NegotiationModel(
    id: j['id'] as String,
    staff: j['isStaff'] as bool,
    type: j['type'] as String,
    message: j['message'] as String,
    proposed: (j['proposedTotal'] as num?)?.toDouble(),
    createdAt: DateTime.parse(j['createdAt'] as String),
  );
  final String id, type, message;
  final bool staff;
  final double? proposed;
  final DateTime createdAt;
}

class RfqDetailModel {
  const RfqDetailModel({
    required this.id,
    required this.number,
    required this.title,
    required this.description,
    required this.status,
    required this.requiredDate,
    required this.deadline,
    required this.governorate,
    required this.items,
    required this.files,
    required this.quoteNumber,
    required this.quoteStatus,
    required this.versions,
    required this.negotiations,
    required this.orderId,
  });
  factory RfqDetailModel.fromJson(Map<String, dynamic> j) => RfqDetailModel(
    id: j['id'] as String,
    number: j['number'] as String,
    title: j['title'] as String,
    description: j['description'] as String?,
    status: j['status'] as String,
    requiredDate: DateTime.parse(j['requiredDate'] as String),
    deadline: DateTime.parse(j['quoteDeadline'] as String),
    governorate: j['deliveryGovernorate'] as String?,
    items: (j['items'] as List)
        .map((x) => RfqItemModel.fromJson(x as Map<String, dynamic>))
        .toList(),
    files: (j['attachments'] as List)
        .map((x) => RfqFileModel.fromJson(x as Map<String, dynamic>))
        .toList(),
    quoteNumber: j['quoteNumber'] as String?,
    quoteStatus: j['quoteStatus'] as String?,
    versions: (j['quoteVersions'] as List)
        .map((x) => QuoteVersionModel.fromJson(x as Map<String, dynamic>))
        .toList(),
    negotiations: (j['negotiations'] as List)
        .map((x) => NegotiationModel.fromJson(x as Map<String, dynamic>))
        .toList(),
    orderId: j['convertedOrderId'] as String?,
  );
  final String id, number, title, status;
  final String? description, governorate, quoteNumber, quoteStatus, orderId;
  final DateTime requiredDate, deadline;
  final List<RfqItemModel> items;
  final List<RfqFileModel> files;
  final List<QuoteVersionModel> versions;
  final List<NegotiationModel> negotiations;
}

class RfqConversionOptions {
  const RfqConversionOptions({
    required this.branches,
    required this.centers,
    required this.receivers,
  });
  factory RfqConversionOptions.fromJson(Map<String, dynamic> j) =>
      RfqConversionOptions(
        branches: (j['branches'] as List).cast<Map<String, dynamic>>(),
        centers: (j['costCenters'] as List).cast<Map<String, dynamic>>(),
        receivers: (j['receivers'] as List).cast<Map<String, dynamic>>(),
      );
  final List<Map<String, dynamic>> branches, centers, receivers;
}

class RfqRepository {
  RfqRepository(this._api);
  final ApiClient _api;
  Future<List<RfqListItem>> list([String? status]) async =>
      ((await _api.dio.get(
                '/api/rfqs',
                queryParameters: {if (status != null) 'status': status},
              )).data
              as List)
          .map((x) => RfqListItem.fromJson(x as Map<String, dynamic>))
          .toList();
  Future<RfqDetailModel> detail(String id) async => RfqDetailModel.fromJson(
    (await _api.dio.get('/api/rfqs/$id')).data as Map<String, dynamic>,
  );
  Future<RfqDetailModel> create({
    required String title,
    String? description,
    required DateTime requiredDate,
    required DateTime deadline,
    String? governorate,
  }) async => RfqDetailModel.fromJson(
    (await _api.dio.post(
          '/api/rfqs',
          data: {
            'title': title,
            'description': description,
            'requiredDate': requiredDate.toIso8601String(),
            'quoteDeadline': deadline.toIso8601String(),
            'deliveryGovernorate': governorate,
          },
        )).data
        as Map<String, dynamic>,
  );
  Map<String, dynamic> _item({
    String? productId,
    required String description,
    required double quantity,
    required String unit,
    String? specifications,
    String? brand,
    bool alternatives = true,
    String source = 'FreeText',
    bool reviewed = true,
  }) => {
    'productId': productId,
    'description': description,
    'quantity': quantity,
    'unitName': unit,
    'specifications': specifications,
    'preferredBrand': brand,
    'allowAlternatives': alternatives,
    'source': source,
    'isReviewed': reviewed,
  };
  Future<RfqDetailModel> addItem(
    String id, {
    String? productId,
    required String description,
    required double quantity,
    required String unit,
    String? specifications,
    String? brand,
    bool alternatives = true,
    String source = 'FreeText',
  }) async => RfqDetailModel.fromJson(
    (await _api.dio.post(
          '/api/rfqs/$id/items',
          data: _item(
            productId: productId,
            description: description,
            quantity: quantity,
            unit: unit,
            specifications: specifications,
            brand: brand,
            alternatives: alternatives,
            source: source,
          ),
        )).data
        as Map<String, dynamic>,
  );
  Future<RfqDetailModel> updateItem(
    String id,
    String itemId, {
    String? productId,
    required String description,
    required double quantity,
    required String unit,
    String? specifications,
    String? brand,
    bool alternatives = true,
    String source = 'FreeText',
  }) async => RfqDetailModel.fromJson(
    (await _api.dio.put(
          '/api/rfqs/$id/items/$itemId',
          data: _item(
            productId: productId,
            description: description,
            quantity: quantity,
            unit: unit,
            specifications: specifications,
            brand: brand,
            alternatives: alternatives,
            source: source,
          ),
        )).data
        as Map<String, dynamic>,
  );
  Future<RfqFileModel> upload(String id, PlatformFile file) async {
    final upload = file.bytes != null
        ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
        : await MultipartFile.fromFile(file.path!, filename: file.name);
    return RfqFileModel.fromJson(
      (await _api.dio.post(
            '/api/rfqs/$id/attachments',
            data: FormData.fromMap({'file': upload}),
          )).data
          as Map<String, dynamic>,
    );
  }

  Future<RfqDetailModel> submit(String id) async => RfqDetailModel.fromJson(
    (await _api.dio.post('/api/rfqs/$id/submit')).data as Map<String, dynamic>,
  );
  Future<RfqDetailModel> negotiate(
    String id,
    String message, {
    double? proposed,
    String type = 'Message',
  }) async => RfqDetailModel.fromJson(
    (await _api.dio.post(
          '/api/rfqs/$id/negotiate',
          data: {'message': message, 'proposedTotal': proposed, 'type': type},
        )).data
        as Map<String, dynamic>,
  );
  Future<RfqDetailModel> decision(
    String id,
    String action,
    String versionId,
    String? comment,
  ) async => RfqDetailModel.fromJson(
    (await _api.dio.post(
          '/api/rfqs/$id/quotes/$action',
          data: {'versionId': versionId, 'comment': comment},
        )).data
        as Map<String, dynamic>,
  );
  Future<Uint8List> quotePdf(String id, String versionId) async =>
      Uint8List.fromList(
        (await _api.dio.get<List<int>>(
          '/api/rfqs/$id/quotes/$versionId/pdf',
          options: Options(responseType: ResponseType.bytes),
        )).data!,
      );
  Future<RfqConversionOptions> conversionOptions() async =>
      RfqConversionOptions.fromJson(
        (await _api.dio.get('/api/rfqs/conversion-options')).data
            as Map<String, dynamic>,
      );
  Future<Map<String, dynamic>> convert(
    String id, {
    required String branchId,
    required String costCenterId,
    required String receiverName,
    required String receiverPhone,
  }) async =>
      (await _api.dio.post(
            '/api/rfqs/$id/convert',
            data: {
              'branchId': branchId,
              'costCenterId': costCenterId,
              'receiverName': receiverName,
              'receiverPhone': receiverPhone,
            },
          )).data
          as Map<String, dynamic>;
}

final rfqRepositoryProvider = Provider(
  (ref) => RfqRepository(ref.watch(apiClientProvider)),
);
final rfqListProvider = FutureProvider.family<List<RfqListItem>, String?>(
  (ref, status) => ref.watch(rfqRepositoryProvider).list(status),
);
final rfqDetailProvider = FutureProvider.family<RfqDetailModel, String>(
  (ref, id) => ref.watch(rfqRepositoryProvider).detail(id),
);
