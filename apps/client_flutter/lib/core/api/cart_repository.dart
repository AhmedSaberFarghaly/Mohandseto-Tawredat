import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

class CartItemModel {
  const CartItemModel({
    required this.id,
    required this.productId,
    required this.slug,
    required this.sku,
    required this.name,
    required this.variantName,
    required this.quantity,
    required this.minOrderQty,
    required this.availableQty,
    required this.unitPrice,
    required this.lineTotal,
    required this.savings,
    required this.unitName,
    required this.stockStatus,
    required this.imageUrl,
    required this.saved,
  });
  factory CartItemModel.fromJson(Map<String, dynamic> json) => CartItemModel(
    id: json['id'] as String,
    productId: json['productId'] as String,
    slug: json['slug'] as String,
    sku: json['sku'] as String,
    name: json['nameAr'] as String,
    variantName: json['variantName'] as String?,
    quantity: json['quantity'] as int,
    minOrderQty: json['minOrderQty'] as int,
    availableQty: json['availableQty'] as int,
    unitPrice: (json['unitPrice'] as num).toDouble(),
    lineTotal: (json['lineTotal'] as num).toDouble(),
    savings: (json['savings'] as num).toDouble(),
    unitName: json['unitName'] as String,
    stockStatus: json['stockStatus'] as String,
    imageUrl: json['imageUrl'] as String?,
    saved: json['isSavedForLater'] as bool,
  );
  final String id, productId, slug, sku, name, unitName, stockStatus;
  final String? variantName, imageUrl;
  final int quantity, minOrderQty, availableQty;
  final double unitPrice, lineTotal, savings;
  final bool saved;
}

class CartModel {
  const CartModel({
    required this.id,
    required this.items,
    required this.savedItems,
    required this.itemCount,
    required this.totalQuantity,
    required this.subtotalBeforeSavings,
    required this.savings,
    required this.subtotal,
    required this.taxIncluded,
    required this.shipping,
    required this.total,
    required this.eligibleForFreeShipping,
  });
  factory CartModel.fromJson(Map<String, dynamic> json) => CartModel(
    id: json['id'] as String?,
    items: (json['items'] as List)
        .map((item) => CartItemModel.fromJson(item as Map<String, dynamic>))
        .toList(),
    savedItems: (json['savedItems'] as List)
        .map((item) => CartItemModel.fromJson(item as Map<String, dynamic>))
        .toList(),
    itemCount: json['itemCount'] as int,
    totalQuantity: json['totalQuantity'] as int,
    subtotalBeforeSavings: (json['subtotalBeforeSavings'] as num).toDouble(),
    savings: (json['savings'] as num).toDouble(),
    subtotal: (json['subtotal'] as num).toDouble(),
    taxIncluded: (json['taxIncluded'] as num).toDouble(),
    shipping: (json['shipping'] as num).toDouble(),
    total: (json['total'] as num).toDouble(),
    eligibleForFreeShipping: json['eligibleForFreeShipping'] as bool,
  );
  final String? id;
  final List<CartItemModel> items, savedItems;
  final int itemCount, totalQuantity;
  final double subtotalBeforeSavings,
      savings,
      subtotal,
      taxIncluded,
      shipping,
      total;
  final bool eligibleForFreeShipping;
}

class CartRepository {
  CartRepository(this._api);
  final ApiClient _api;
  Future<CartModel> get() async => CartModel.fromJson(
    (await _api.dio.get('/api/cart')).data as Map<String, dynamic>,
  );
  Future<CartModel> add(
    String productId,
    int quantity, {
    String? variantId,
  }) async => CartModel.fromJson(
    (await _api.dio.post(
          '/api/cart/items',
          data: {
            'productId': productId,
            'variantId': variantId,
            'quantity': quantity,
          },
        )).data
        as Map<String, dynamic>,
  );
  Future<CartModel> update(String itemId, int quantity) async =>
      CartModel.fromJson(
        (await _api.dio.put(
              '/api/cart/items/$itemId',
              data: {'quantity': quantity},
            )).data
            as Map<String, dynamic>,
      );
  Future<CartModel> remove(String itemId) async => CartModel.fromJson(
    (await _api.dio.delete('/api/cart/items/$itemId')).data
        as Map<String, dynamic>,
  );
  Future<CartModel> save(String itemId, bool saved) async => CartModel.fromJson(
    (await _api.dio.post(
          '/api/cart/items/$itemId/${saved ? 'save-for-later' : 'restore'}',
        )).data
        as Map<String, dynamic>,
  );
  Future<void> clear() => _api.dio.delete('/api/cart');
}

final cartRepositoryProvider = Provider(
  (ref) => CartRepository(ref.watch(apiClientProvider)),
);
final cartProvider = FutureProvider(
  (ref) => ref.watch(cartRepositoryProvider).get(),
);
