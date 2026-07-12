import 'package:flutter_test/flutter_test.dart';
import 'package:mohandseto_client/core/api/catalog_repository.dart';

void main() {
  test('catalog page parses API pricing and stock metadata', () {
    final page = CatalogPage.fromJson({
      'items': [
        {
          'id': 'p1',
          'sku': 'MT-00001',
          'nameAr': 'ورق تصوير ممتاز',
          'nameEn': 'Premium Copy Paper',
          'slug': 'premium-copy-paper',
          'categoryName': 'ورق تصوير',
          'brandName': 'دبل إيه',
          'price': 245.5,
          'compareAtPrice': 270,
          'hasContractPrice': true,
          'stockStatus': 'InStock',
          'stockQty': 80,
          'imageUrl': null,
          'rating': 4.6,
          'ratingCount': 72,
          'minOrderQty': 1,
          'unitName': 'رزمة',
          'isPrintable': false,
          'isFeatured': true,
          'isFavorite': false,
        },
      ],
      'page': 1,
      'pageSize': 20,
      'total': 1,
      'totalPages': 1,
    });

    expect(page.items, hasLength(1));
    expect(page.items.single.price, 245.5);
    expect(page.items.single.hasContractPrice, isTrue);
    expect(page.items.single.stockQty, 80);
  });

  test('catalog query emits only active filter values', () {
    final query = const CatalogQuery(
      q: 'ورق',
      categoryId: 'category-1',
      minPrice: 100,
      maxPrice: 500,
      stock: 'in',
      sort: 'price_asc',
      page: 2,
    ).toQuery();

    expect(query['q'], 'ورق');
    expect(query['stock'], 'in');
    expect(query['page'], 2);
    expect(query.containsKey('brandId'), isFalse);
  });
}
