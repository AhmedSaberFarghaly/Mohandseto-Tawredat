import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';

import 'api_client.dart';
import 'cart_repository.dart';

class CheckoutBranch {
  const CheckoutBranch({
    required this.id,
    required this.name,
    required this.address,
    required this.phone,
    required this.isMain,
    required this.latitude,
    required this.longitude,
  });
  factory CheckoutBranch.fromJson(Map<String, dynamic> json) => CheckoutBranch(
    id: json['id'] as String,
    name: json['name'] as String,
    address: json['address'] as String,
    phone: json['phone'] as String?,
    isMain: json['isMain'] as bool,
    latitude: (json['latitude'] as num?)?.toDouble(),
    longitude: (json['longitude'] as num?)?.toDouble(),
  );
  final String id, name, address;
  final String? phone;
  final bool isMain;
  final double? latitude, longitude;
}

class CheckoutCostCenter {
  const CheckoutCostCenter({
    required this.id,
    required this.code,
    required this.name,
    required this.available,
    required this.threshold,
  });
  factory CheckoutCostCenter.fromJson(Map<String, dynamic> json) =>
      CheckoutCostCenter(
        id: json['id'] as String,
        code: json['code'] as String,
        name: json['nameAr'] as String,
        available: (json['availableAmount'] as num).toDouble(),
        threshold: (json['approvalThreshold'] as num?)?.toDouble(),
      );
  final String id, code, name;
  final double available;
  final double? threshold;
}

class CheckoutProject {
  const CheckoutProject({
    required this.id,
    required this.code,
    required this.name,
  });
  factory CheckoutProject.fromJson(Map<String, dynamic> json) =>
      CheckoutProject(
        id: json['id'] as String,
        code: json['code'] as String,
        name: json['nameAr'] as String,
      );
  final String id, code, name;
}

class CheckoutAttachment {
  const CheckoutAttachment({
    required this.id,
    required this.name,
    required this.sizeBytes,
    required this.downloadUrl,
  });
  factory CheckoutAttachment.fromJson(Map<String, dynamic> json) =>
      CheckoutAttachment(
        id: json['id'] as String,
        name: json['name'] as String,
        sizeBytes: json['sizeBytes'] as int,
        downloadUrl: json['downloadUrl'] as String,
      );
  final String id, name, downloadUrl;
  final int sizeBytes;
}

class CheckoutPaymentOption {
  const CheckoutPaymentOption({
    required this.code,
    required this.name,
    required this.enabled,
    required this.reason,
  });
  factory CheckoutPaymentOption.fromJson(Map<String, dynamic> json) =>
      CheckoutPaymentOption(
        code: json['code'] as String,
        name: json['nameAr'] as String,
        enabled: json['enabled'] as bool,
        reason: json['reason'] as String?,
      );
  final String code, name;
  final bool enabled;
  final String? reason;
}

class CheckoutOptions {
  const CheckoutOptions({
    required this.sessionId,
    required this.cart,
    required this.branches,
    required this.payments,
    required this.branchId,
    required this.receiverName,
    required this.receiverPhone,
    required this.requiredDate,
    required this.timeSlot,
    required this.shippingMethod,
    required this.paymentMethod,
    required this.purchaseOrderNumber,
    required this.internalReference,
    this.costCenters = const [],
    this.projects = const [],
    this.costCenterId,
    this.projectId,
    this.requestingDepartment,
    this.orderNote,
    this.allowSplitDelivery = false,
    this.paymentAttemptId,
    this.creditPortion,
    this.cardPortion,
    this.purchaseOrderAttachment,
  });
  factory CheckoutOptions.fromJson(Map<String, dynamic> json) =>
      CheckoutOptions(
        sessionId: json['sessionId'] as String,
        cart: CartModel.fromJson(json['cart'] as Map<String, dynamic>),
        branches: (json['branches'] as List)
            .map(
              (item) => CheckoutBranch.fromJson(item as Map<String, dynamic>),
            )
            .toList(),
        payments: (json['paymentOptions'] as List)
            .map(
              (item) =>
                  CheckoutPaymentOption.fromJson(item as Map<String, dynamic>),
            )
            .toList(),
        branchId: json['branchId'] as String?,
        receiverName: json['receiverName'] as String?,
        receiverPhone: json['receiverPhone'] as String?,
        requiredDate: json['requiredDate'] == null
            ? null
            : DateTime.parse(json['requiredDate'] as String),
        timeSlot: json['timeSlot'] as String?,
        shippingMethod: json['shippingMethod'] as String,
        paymentMethod: json['paymentMethod'] as String?,
        purchaseOrderNumber: json['purchaseOrderNumber'] as String?,
        internalReference: json['internalReference'] as String?,
        costCenters: ((json['costCenters'] as List?) ?? const [])
            .map((x) => CheckoutCostCenter.fromJson(x as Map<String, dynamic>))
            .toList(),
        projects: ((json['projects'] as List?) ?? const [])
            .map((x) => CheckoutProject.fromJson(x as Map<String, dynamic>))
            .toList(),
        costCenterId: json['costCenterId'] as String?,
        projectId: json['projectId'] as String?,
        requestingDepartment: json['requestingDepartment'] as String?,
        orderNote: json['orderNote'] as String?,
        allowSplitDelivery: json['allowSplitDelivery'] as bool? ?? false,
        paymentAttemptId: json['paymentAttemptId'] as String?,
        creditPortion: (json['creditPortion'] as num?)?.toDouble(),
        cardPortion: (json['cardPortion'] as num?)?.toDouble(),
        purchaseOrderAttachment: json['purchaseOrderAttachment'] == null
            ? null
            : CheckoutAttachment.fromJson(
                json['purchaseOrderAttachment'] as Map<String, dynamic>,
              ),
      );
  final String sessionId;
  final CartModel cart;
  final List<CheckoutBranch> branches;
  final List<CheckoutPaymentOption> payments;
  final String? branchId,
      receiverName,
      receiverPhone,
      timeSlot,
      paymentMethod,
      purchaseOrderNumber,
      internalReference;
  final DateTime? requiredDate;
  final String shippingMethod;
  final List<CheckoutCostCenter> costCenters;
  final List<CheckoutProject> projects;
  final String? costCenterId,
      projectId,
      requestingDepartment,
      orderNote,
      paymentAttemptId;
  final bool allowSplitDelivery;
  final double? creditPortion, cardPortion;
  final CheckoutAttachment? purchaseOrderAttachment;
}

class CheckoutReview {
  const CheckoutReview({
    required this.items,
    required this.branchName,
    required this.address,
    required this.receiverName,
    required this.requiredDate,
    required this.timeSlot,
    required this.shippingMethod,
    required this.paymentMethod,
    required this.poNumber,
    required this.subtotal,
    required this.savings,
    required this.tax,
    required this.shipping,
    required this.total,
    required this.requiresApproval,
    this.costCenterName,
    this.projectName,
    this.requestingDepartment,
    this.orderNote,
    this.allowSplitDelivery = false,
    this.purchaseOrderAttachment,
    this.budgetAvailable,
    this.budgetExceeded = false,
  });
  factory CheckoutReview.fromJson(Map<String, dynamic> json) => CheckoutReview(
    items: (json['items'] as List)
        .map((item) => CartItemModel.fromJson(item as Map<String, dynamic>))
        .toList(),
    branchName: json['branchName'] as String,
    address: json['deliveryAddress'] as String,
    receiverName: json['receiverName'] as String,
    requiredDate: DateTime.parse(json['requiredDate'] as String),
    timeSlot: json['timeSlot'] as String,
    shippingMethod: json['shippingMethod'] as String,
    paymentMethod: json['paymentMethod'] as String,
    poNumber: json['purchaseOrderNumber'] as String?,
    subtotal: (json['subtotal'] as num).toDouble(),
    savings: (json['savings'] as num).toDouble(),
    tax: (json['taxIncluded'] as num).toDouble(),
    shipping: (json['shipping'] as num).toDouble(),
    total: (json['total'] as num).toDouble(),
    requiresApproval: json['requiresApproval'] as bool,
    costCenterName: json['costCenterName'] as String?,
    projectName: json['projectName'] as String?,
    requestingDepartment: json['requestingDepartment'] as String?,
    orderNote: json['orderNote'] as String?,
    allowSplitDelivery: json['allowSplitDelivery'] as bool? ?? false,
    purchaseOrderAttachment: json['purchaseOrderAttachment'] == null
        ? null
        : CheckoutAttachment.fromJson(
            json['purchaseOrderAttachment'] as Map<String, dynamic>,
          ),
    budgetAvailable: (json['budgetAvailable'] as num?)?.toDouble(),
    budgetExceeded: json['budgetExceeded'] as bool? ?? false,
  );
  final List<CartItemModel> items;
  final String branchName,
      address,
      receiverName,
      timeSlot,
      shippingMethod,
      paymentMethod;
  final String? poNumber;
  final DateTime requiredDate;
  final double subtotal, savings, tax, shipping, total;
  final bool requiresApproval;
  final String? costCenterName, projectName, requestingDepartment, orderNote;
  final bool allowSplitDelivery, budgetExceeded;
  final CheckoutAttachment? purchaseOrderAttachment;
  final double? budgetAvailable;
}

class CheckoutPaymentAttempt {
  const CheckoutPaymentAttempt({
    required this.id,
    required this.reference,
    required this.status,
    required this.amount,
    required this.failureMessage,
  });
  factory CheckoutPaymentAttempt.fromJson(Map<String, dynamic> json) =>
      CheckoutPaymentAttempt(
        id: json['id'] as String,
        reference: json['providerReference'] as String,
        status: json['status'] as String,
        amount: (json['amount'] as num).toDouble(),
        failureMessage: json['failureMessage'] as String?,
      );
  final String id, reference, status;
  final double amount;
  final String? failureMessage;
}

class OrderCreated {
  const OrderCreated({
    required this.id,
    required this.number,
    required this.status,
    required this.requiresApproval,
    required this.total,
    required this.requiredDate,
  });
  factory OrderCreated.fromJson(Map<String, dynamic> json) => OrderCreated(
    id: json['id'] as String,
    number: json['number'] as String,
    status: json['status'] as String,
    requiresApproval: json['requiresApproval'] as bool,
    total: (json['total'] as num).toDouble(),
    requiredDate: DateTime.parse(json['requiredDate'] as String),
  );
  final String id, number, status;
  final bool requiresApproval;
  final double total;
  final DateTime requiredDate;
}

class CheckoutRepository {
  CheckoutRepository(this._api);
  final ApiClient _api;
  Future<CheckoutOptions> options() async => CheckoutOptions.fromJson(
    (await _api.dio.get('/api/checkout/options')).data as Map<String, dynamic>,
  );
  Future<CheckoutOptions> delivery({
    required String branchId,
    required String receiverName,
    required String receiverPhone,
    required DateTime requiredDate,
    required String timeSlot,
    required String shippingMethod,
    bool allowSplitDelivery = false,
  }) async => CheckoutOptions.fromJson(
    (await _api.dio.put(
          '/api/checkout/delivery',
          data: {
            'branchId': branchId,
            'receiverName': receiverName,
            'receiverPhone': receiverPhone,
            'requiredDate': requiredDate.toIso8601String(),
            'timeSlot': timeSlot,
            'shippingMethod': shippingMethod,
            'allowSplitDelivery': allowSplitDelivery,
          },
        )).data
        as Map<String, dynamic>,
  );
  Future<CheckoutOptions> context({
    required String costCenterId,
    String? projectId,
    required String department,
    String? orderNote,
    String? internalReference,
  }) async => CheckoutOptions.fromJson(
    (await _api.dio.put(
          '/api/checkout/context',
          data: {
            'costCenterId': costCenterId,
            'projectId': projectId,
            'requestingDepartment': department,
            'orderNote': orderNote,
            'internalReference': internalReference,
          },
        )).data
        as Map<String, dynamic>,
  );
  Future<CheckoutOptions> addAddress({
    required String name,
    required String governorate,
    required String city,
    required String addressLine,
    String? phone,
    double? latitude,
    double? longitude,
  }) async => CheckoutOptions.fromJson(
    (await _api.dio.post(
          '/api/checkout/addresses',
          data: {
            'name': name,
            'governorate': governorate,
            'city': city,
            'addressLine': addressLine,
            'phone': phone,
            'latitude': latitude,
            'longitude': longitude,
          },
        )).data
        as Map<String, dynamic>,
  );
  Future<CheckoutOptions> payment({
    required String method,
    String? poNumber,
    String? internalReference,
    String? paymentAttemptId,
    double? creditPortion,
    double? cardPortion,
  }) async => CheckoutOptions.fromJson(
    (await _api.dio.put(
          '/api/checkout/payment',
          data: {
            'paymentMethod': method,
            'purchaseOrderNumber': poNumber,
            'internalReference': internalReference,
            'paymentAttemptId': paymentAttemptId,
            'creditPortion': creditPortion,
            'cardPortion': cardPortion,
          },
        )).data
        as Map<String, dynamic>,
  );
  Future<CheckoutAttachment> uploadPurchaseOrder(PlatformFile file) async {
    final upload = file.bytes != null
        ? MultipartFile.fromBytes(file.bytes!, filename: file.name)
        : await MultipartFile.fromFile(file.path!, filename: file.name);
    return CheckoutAttachment.fromJson(
      (await _api.dio.post(
            '/api/checkout/attachments/purchase-order',
            data: FormData.fromMap({'file': upload}),
          )).data
          as Map<String, dynamic>,
    );
  }

  Future<CheckoutPaymentAttempt> createPayment(double amount) async =>
      CheckoutPaymentAttempt.fromJson(
        (await _api.dio.post(
              '/api/checkout/payment-attempts',
              data: {
                'idempotencyKey':
                    'mobile-${DateTime.now().microsecondsSinceEpoch}',
                'amount': amount,
              },
            )).data
            as Map<String, dynamic>,
      );
  Future<CheckoutPaymentAttempt> confirmPayment(
    String id, {
    String token = 'tok_test_success',
  }) async => CheckoutPaymentAttempt.fromJson(
    (await _api.dio.post(
          '/api/checkout/payment-attempts/$id/confirm',
          data: {'paymentToken': token},
        )).data
        as Map<String, dynamic>,
  );
  Future<CheckoutReview> review() async => CheckoutReview.fromJson(
    (await _api.dio.get('/api/checkout/review')).data as Map<String, dynamic>,
  );
  Future<OrderCreated> submit() async => OrderCreated.fromJson(
    (await _api.dio.post(
          '/api/checkout/submit',
          data: {'acceptTerms': true},
        )).data
        as Map<String, dynamic>,
  );
}

final checkoutRepositoryProvider = Provider(
  (ref) => CheckoutRepository(ref.watch(apiClientProvider)),
);
