import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';
import 'cart_repository.dart';

class CheckoutBranch {
  const CheckoutBranch({
    required this.id,
    required this.name,
    required this.address,
    required this.phone,
    required this.isMain,
  });
  factory CheckoutBranch.fromJson(Map<String, dynamic> json) => CheckoutBranch(
    id: json['id'] as String,
    name: json['name'] as String,
    address: json['address'] as String,
    phone: json['phone'] as String?,
    isMain: json['isMain'] as bool,
  );
  final String id, name, address;
  final String? phone;
  final bool isMain;
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
          },
        )).data
        as Map<String, dynamic>,
  );
  Future<CheckoutOptions> payment({
    required String method,
    String? poNumber,
    String? internalReference,
  }) async => CheckoutOptions.fromJson(
    (await _api.dio.put(
          '/api/checkout/payment',
          data: {
            'paymentMethod': method,
            'purchaseOrderNumber': poNumber,
            'internalReference': internalReference,
          },
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
