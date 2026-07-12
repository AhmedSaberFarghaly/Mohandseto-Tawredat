import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/checkout_repository.dart';

void main() {
  test('checkout options parse branches payments and cart totals', () {
    final options = CheckoutOptions.fromJson({
      'sessionId': 'session-1',
      'cart': {
        'id': 'cart-1',
        'items': <Object>[],
        'savedItems': <Object>[],
        'itemCount': 0,
        'totalQuantity': 0,
        'subtotalBeforeSavings': 0,
        'savings': 0,
        'subtotal': 0,
        'taxIncluded': 0,
        'shipping': 0,
        'total': 0,
        'eligibleForFreeShipping': false,
      },
      'branches': [
        {
          'id': 'branch-1',
          'name': 'الرئيسي',
          'address': 'القاهرة',
          'phone': '0100',
          'isMain': true,
        },
      ],
      'paymentOptions': [
        {
          'code': 'BankTransfer',
          'nameAr': 'تحويل بنكي',
          'enabled': true,
          'reason': null,
        },
      ],
      'branchId': null,
      'receiverName': null,
      'receiverPhone': null,
      'requiredDate': null,
      'timeSlot': null,
      'shippingMethod': 'Standard',
      'paymentMethod': null,
      'purchaseOrderNumber': null,
      'internalReference': null,
    });
    expect(options.branches.single.isMain, isTrue);
    expect(options.payments.single.enabled, isTrue);
    expect(options.cart.total, 0);
  });

  test('created order parses approval and delivery state', () {
    final order = OrderCreated.fromJson({
      'id': 'order-1',
      'number': 'ORD-260712-1234',
      'status': 'PendingApproval',
      'requiresApproval': true,
      'total': 42192.5,
      'requiredDate': '2026-07-15T00:00:00Z',
    });
    expect(order.requiresApproval, isTrue);
    expect(order.status, 'PendingApproval');
    expect(order.total, 42192.5);
  });
}
