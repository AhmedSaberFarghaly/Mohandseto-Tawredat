import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

class CatalogCategory {
  const CatalogCategory({
    required this.id,
    required this.nameAr,
    required this.nameEn,
    required this.slug,
    required this.iconName,
    required this.productCount,
    required this.children,
  });

  factory CatalogCategory.fromJson(Map<String, dynamic> json) =>
      CatalogCategory(
        id: json['id'] as String,
        nameAr: json['nameAr'] as String,
        nameEn: json['nameEn'] as String,
        slug: json['slug'] as String,
        iconName: json['iconName'] as String?,
        productCount: json['productCount'] as int,
        children: (json['children'] as List)
            .map(
              (item) => CatalogCategory.fromJson(item as Map<String, dynamic>),
            )
            .toList(),
      );

  final String id;
  final String nameAr;
  final String nameEn;
  final String slug;
  final String? iconName;
  final int productCount;
  final List<CatalogCategory> children;
}

class CatalogBrand {
  const CatalogBrand({
    required this.id,
    required this.nameAr,
    required this.nameEn,
  });
  factory CatalogBrand.fromJson(Map<String, dynamic> json) => CatalogBrand(
    id: json['id'] as String,
    nameAr: json['nameAr'] as String,
    nameEn: json['nameEn'] as String,
  );
  final String id;
  final String nameAr;
  final String nameEn;
}

class CatalogProduct {
  const CatalogProduct({
    required this.id,
    required this.sku,
    required this.nameAr,
    required this.nameEn,
    required this.slug,
    required this.categoryName,
    required this.brandName,
    required this.price,
    required this.compareAtPrice,
    required this.hasContractPrice,
    required this.stockStatus,
    required this.stockQty,
    required this.imageUrl,
    required this.rating,
    required this.ratingCount,
    required this.minOrderQty,
    required this.unitName,
    required this.isPrintable,
    required this.isFeatured,
    required this.isFavorite,
  });

  factory CatalogProduct.fromJson(Map<String, dynamic> json) => CatalogProduct(
    id: json['id'] as String,
    sku: json['sku'] as String,
    nameAr: json['nameAr'] as String,
    nameEn: json['nameEn'] as String,
    slug: json['slug'] as String,
    categoryName: json['categoryName'] as String,
    brandName: json['brandName'] as String?,
    price: (json['price'] as num).toDouble(),
    compareAtPrice: (json['compareAtPrice'] as num?)?.toDouble(),
    hasContractPrice: json['hasContractPrice'] as bool,
    stockStatus: json['stockStatus'] as String,
    stockQty: json['stockQty'] as int,
    imageUrl: json['imageUrl'] as String?,
    rating: (json['rating'] as num).toDouble(),
    ratingCount: json['ratingCount'] as int,
    minOrderQty: json['minOrderQty'] as int,
    unitName: json['unitName'] as String,
    isPrintable: json['isPrintable'] as bool,
    isFeatured: json['isFeatured'] as bool,
    isFavorite: json['isFavorite'] as bool,
  );

  final String id;
  final String sku;
  final String nameAr;
  final String nameEn;
  final String slug;
  final String categoryName;
  final String? brandName;
  final double price;
  final double? compareAtPrice;
  final bool hasContractPrice;
  final String stockStatus;
  final int stockQty;
  final String? imageUrl;
  final double rating;
  final int ratingCount;
  final int minOrderQty;
  final String unitName;
  final bool isPrintable;
  final bool isFeatured;
  final bool isFavorite;
}

class CatalogPage {
  const CatalogPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.total,
    required this.totalPages,
  });
  factory CatalogPage.fromJson(Map<String, dynamic> json) => CatalogPage(
    items: (json['items'] as List)
        .map((item) => CatalogProduct.fromJson(item as Map<String, dynamic>))
        .toList(),
    page: json['page'] as int,
    pageSize: json['pageSize'] as int,
    total: json['total'] as int,
    totalPages: json['totalPages'] as int,
  );
  final List<CatalogProduct> items;
  final int page;
  final int pageSize;
  final int total;
  final int totalPages;
}

class PriceTier {
  const PriceTier(this.minQty, this.unitPrice);
  factory PriceTier.fromJson(Map<String, dynamic> json) =>
      PriceTier(json['minQty'] as int, (json['unitPrice'] as num).toDouble());
  final int minQty;
  final double unitPrice;
}

class ProductAttribute {
  const ProductAttribute(this.name, this.value);
  factory ProductAttribute.fromJson(Map<String, dynamic> json) =>
      ProductAttribute(json['nameAr'] as String, json['valueAr'] as String);
  final String name;
  final String value;
}

class ProductVariant {
  const ProductVariant({
    required this.id,
    required this.name,
    required this.price,
    required this.stockQty,
  });
  factory ProductVariant.fromJson(Map<String, dynamic> json) => ProductVariant(
    id: json['id'] as String,
    name: json['nameAr'] as String,
    price: (json['price'] as num).toDouble(),
    stockQty: json['stockQty'] as int,
  );
  final String id;
  final String name;
  final double price;
  final int stockQty;
}

class CatalogDocument {
  const CatalogDocument({
    required this.id,
    required this.name,
    required this.url,
    required this.contentType,
  });
  factory CatalogDocument.fromJson(Map<String, dynamic> json) =>
      CatalogDocument(
        id: json['id'] as String,
        name: json['nameAr'] as String,
        url: json['url'] as String,
        contentType: json['contentType'] as String,
      );
  final String id;
  final String name;
  final String url;
  final String contentType;
}

class CompareProduct {
  const CompareProduct({required this.summary, required this.attributes});
  factory CompareProduct.fromJson(Map<String, dynamic> json) => CompareProduct(
    summary: CatalogProduct.fromJson(json['summary'] as Map<String, dynamic>),
    attributes: (json['attributes'] as Map<String, dynamic>).map(
      (key, value) => MapEntry(key, value as String),
    ),
  );
  final CatalogProduct summary;
  final Map<String, String> attributes;
}

class ProductDetail {
  const ProductDetail({
    required this.summary,
    required this.description,
    required this.warranty,
    required this.deliveryDays,
    required this.priceTiers,
    required this.attributes,
    required this.variants,
    required this.documents,
    required this.related,
  });
  factory ProductDetail.fromJson(Map<String, dynamic> json) => ProductDetail(
    summary: CatalogProduct.fromJson(json['summary'] as Map<String, dynamic>),
    description: json['descriptionAr'] as String?,
    warranty: json['warrantyAr'] as String?,
    deliveryDays: json['deliveryEstimateDays'] as int,
    priceTiers: (json['priceTiers'] as List)
        .map((item) => PriceTier.fromJson(item as Map<String, dynamic>))
        .toList(),
    attributes: (json['attributes'] as List)
        .map((item) => ProductAttribute.fromJson(item as Map<String, dynamic>))
        .toList(),
    variants: (json['variants'] as List)
        .map((item) => ProductVariant.fromJson(item as Map<String, dynamic>))
        .toList(),
    documents: (json['documents'] as List)
        .map((item) => CatalogDocument.fromJson(item as Map<String, dynamic>))
        .toList(),
    related: (json['relatedProducts'] as List)
        .map((item) => CatalogProduct.fromJson(item as Map<String, dynamic>))
        .toList(),
  );
  final CatalogProduct summary;
  final String? description;
  final String? warranty;
  final int deliveryDays;
  final List<PriceTier> priceTiers;
  final List<ProductAttribute> attributes;
  final List<ProductVariant> variants;
  final List<CatalogDocument> documents;
  final List<CatalogProduct> related;
}

class CatalogQuery {
  const CatalogQuery({
    this.q,
    this.categoryId,
    this.brandId,
    this.minPrice,
    this.maxPrice,
    this.stock,
    this.featured,
    this.printable,
    this.sort = 'featured',
    this.page = 1,
    this.pageSize = 20,
  });
  final String? q;
  final String? categoryId;
  final String? brandId;
  final double? minPrice;
  final double? maxPrice;
  final String? stock;
  final bool? featured;
  final bool? printable;
  final String sort;
  final int page;
  final int pageSize;

  CatalogQuery copyWith({
    String? q,
    String? categoryId,
    String? brandId,
    double? minPrice,
    double? maxPrice,
    String? stock,
    bool? featured,
    bool? printable,
    String? sort,
    int? page,
    int? pageSize,
    bool clearCategory = false,
    bool clearBrand = false,
    bool clearPrice = false,
    bool clearStock = false,
  }) => CatalogQuery(
    q: q ?? this.q,
    categoryId: clearCategory ? null : categoryId ?? this.categoryId,
    brandId: clearBrand ? null : brandId ?? this.brandId,
    minPrice: clearPrice ? null : minPrice ?? this.minPrice,
    maxPrice: clearPrice ? null : maxPrice ?? this.maxPrice,
    stock: clearStock ? null : stock ?? this.stock,
    featured: featured ?? this.featured,
    printable: printable ?? this.printable,
    sort: sort ?? this.sort,
    page: page ?? this.page,
    pageSize: pageSize ?? this.pageSize,
  );

  Map<String, dynamic> toQuery() => {
    if (q?.trim().isNotEmpty == true) 'q': q!.trim(),
    if (categoryId != null) 'categoryId': categoryId,
    if (brandId != null) 'brandId': brandId,
    if (minPrice != null) 'minPrice': minPrice,
    if (maxPrice != null) 'maxPrice': maxPrice,
    if (stock != null) 'stock': stock,
    if (featured != null) 'featured': featured,
    if (printable != null) 'printable': printable,
    'sort': sort,
    'page': page,
    'pageSize': pageSize,
  };
}

class CatalogRepository {
  CatalogRepository(this._api);
  final ApiClient _api;

  Future<List<CatalogCategory>> categories() async {
    final response = await _api.dio.get('/api/catalog/categories');
    return (response.data as List)
        .map((item) => CatalogCategory.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<List<CatalogBrand>> brands() async {
    final response = await _api.dio.get('/api/catalog/brands');
    return (response.data as List)
        .map((item) => CatalogBrand.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<CatalogPage> products(CatalogQuery query) async {
    final response = await _api.dio.get(
      '/api/catalog/products',
      queryParameters: query.toQuery(),
    );
    return CatalogPage.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ProductDetail> product(String idOrSlug) async {
    final response = await _api.dio.get('/api/catalog/products/$idOrSlug');
    return ProductDetail.fromJson(response.data as Map<String, dynamic>);
  }

  Future<List<String>> suggestions(String q) async {
    final response = await _api.dio.get(
      '/api/catalog/search/suggestions',
      queryParameters: {'q': q},
    );
    return (response.data as List).cast<String>();
  }

  Future<bool> toggleFavorite(String productId) async {
    final response = await _api.dio.post(
      '/api/catalog/favorites/$productId/toggle',
    );
    return response.data['isFavorite'] as bool;
  }

  Future<bool> toggleCompare(String productId) async {
    final response = await _api.dio.post(
      '/api/catalog/compare/$productId/toggle',
    );
    return response.data['isCompared'] as bool;
  }

  Future<List<CompareProduct>> compare() async {
    final response = await _api.dio.get('/api/catalog/compare');
    return (response.data as List)
        .map((item) => CompareProduct.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<void> clearCompare() => _api.dio.delete('/api/catalog/compare');

  Future<List<String>> recentSearches() async {
    final response = await _api.dio.get('/api/catalog/search/recent');
    return (response.data as List).cast<String>();
  }

  Future<void> clearRecentSearches() =>
      _api.dio.delete('/api/catalog/search/recent');

  Future<List<CatalogProduct>> favorites() async {
    final response = await _api.dio.get('/api/catalog/favorites');
    return (response.data as List)
        .map((item) => CatalogProduct.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<List<CatalogProduct>> recentlyViewed() async {
    final response = await _api.dio.get('/api/catalog/recently-viewed');
    return (response.data as List)
        .map((item) => CatalogProduct.fromJson(item as Map<String, dynamic>))
        .toList();
  }
}

final catalogRepositoryProvider = Provider(
  (ref) => CatalogRepository(ref.watch(apiClientProvider)),
);

final categoriesProvider = FutureProvider(
  (ref) => ref.watch(catalogRepositoryProvider).categories(),
);
final brandsProvider = FutureProvider(
  (ref) => ref.watch(catalogRepositoryProvider).brands(),
);
final productFeedProvider = FutureProvider.family<CatalogPage, CatalogQuery>(
  (ref, query) => ref.watch(catalogRepositoryProvider).products(query),
);
final productDetailProvider = FutureProvider.family<ProductDetail, String>(
  (ref, id) => ref.watch(catalogRepositoryProvider).product(id),
);
final searchSuggestionsProvider = FutureProvider.family<List<String>, String>(
  (ref, q) => ref.watch(catalogRepositoryProvider).suggestions(q),
);
final favoritesProvider = FutureProvider(
  (ref) => ref.watch(catalogRepositoryProvider).favorites(),
);
final recentlyViewedProvider = FutureProvider(
  (ref) => ref.watch(catalogRepositoryProvider).recentlyViewed(),
);
final compareProvider = FutureProvider(
  (ref) => ref.watch(catalogRepositoryProvider).compare(),
);
final recentSearchesProvider = FutureProvider(
  (ref) => ref.watch(catalogRepositoryProvider).recentSearches(),
);
