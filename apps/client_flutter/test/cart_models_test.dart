import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/cart_repository.dart';

void main() {
  test('cart parses coupon price and availability states', () {
    final cart = CartModel.fromJson({
      'id': 'cart-1',
      'items': [
        {
          'id': 'item-1',
          'productId': 'product-1',
          'slug': 'paper',
          'sku': 'P-1',
          'nameAr': 'ورق',
          'variantName': null,
          'quantity': 10,
          'minOrderQty': 5,
          'availableQty': 8,
          'unitPrice': 120,
          'lineTotal': 1200,
          'savings': 100,
          'unitName': 'عبوة',
          'stockStatus': 'LowStock',
          'imageUrl': null,
          'isSavedForLater': false,
          'customProductRequestId': null,
          'customerNote': 'تغليف منفصل',
          'previousUnitPrice': 110,
          'priceChanged': true,
          'hasAvailabilityIssue': true,
        },
      ],
      'savedItems': [],
      'itemCount': 1,
      'totalQuantity': 10,
      'subtotalBeforeSavings': 1400,
      'savings': 200,
      'subtotal': 1200,
      'taxIncluded': 168,
      'shipping': 150,
      'total': 1350,
      'eligibleForFreeShipping': false,
      'couponCode': 'SAVE10',
      'couponDiscount': 100,
      'orderNote': 'توصيل صباحي',
      'hasPriceChanges': true,
      'hasAvailabilityIssues': true,
    });

    expect(cart.couponCode, 'SAVE10');
    expect(cart.couponDiscount, 100);
    expect(cart.hasPriceChanges, isTrue);
    expect(cart.hasAvailabilityIssues, isTrue);
    expect(cart.items.single.customerNote, 'تغليف منفصل');
    expect(cart.items.single.previousUnitPrice, 110);
  });

  test('saved cart summary parses restore metadata', () {
    final saved = SavedCartModel.fromJson({
      'id': 'saved-1',
      'name': 'احتياجات يوليو',
      'savedAt': '2026-07-13T09:00:00Z',
      'itemCount': 4,
      'estimatedTotal': 3500,
    });
    expect(saved.name, 'احتياجات يوليو');
    expect(saved.itemCount, 4);
    expect(saved.estimatedTotal, 3500);
  });
}
