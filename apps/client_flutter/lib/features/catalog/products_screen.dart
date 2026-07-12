import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'catalog_widgets.dart';

class ProductsScreen extends ConsumerStatefulWidget {
  const ProductsScreen({
    super.key,
    this.categoryId,
    this.title,
    this.initialQuery,
    this.initialSort,
    this.featured,
  });
  final String? categoryId;
  final String? title;
  final String? initialQuery;
  final String? initialSort;
  final bool? featured;

  @override
  ConsumerState<ProductsScreen> createState() => _ProductsScreenState();
}

class _ProductsScreenState extends ConsumerState<ProductsScreen> {
  late CatalogQuery _query;
  late final TextEditingController _search;
  bool _grid = true;
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    _search = TextEditingController(text: widget.initialQuery);
    _query = CatalogQuery(
      categoryId: widget.categoryId,
      q: widget.initialQuery,
      sort: widget.initialSort ?? 'featured',
      featured: widget.featured,
    );
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    super.dispose();
  }

  void _searchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 450), () {
      if (mounted) setState(() => _query = _query.copyWith(q: value, page: 1));
    });
  }

  @override
  Widget build(BuildContext context) {
    final products = ref.watch(productFeedProvider(_query));
    return Scaffold(
      appBar: AppBar(title: Text(widget.title ?? 'المنتجات')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 8),
            child: TextField(
              controller: _search,
              onChanged: _searchChanged,
              textInputAction: TextInputAction.search,
              decoration: InputDecoration(
                hintText: 'ابحث بالاسم أو كود المنتج',
                prefixIcon: const Icon(Icons.search_rounded),
                suffixIcon: _search.text.isEmpty
                    ? null
                    : IconButton(
                        onPressed: () {
                          _search.clear();
                          setState(
                            () => _query = _query.copyWith(q: '', page: 1),
                          );
                        },
                        icon: const Icon(Icons.close_rounded),
                      ),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _showFilters,
                    icon: const Icon(Icons.tune_rounded, size: 18),
                    label: const Text('تصفية'),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: _showSort,
                    icon: const Icon(Icons.swap_vert_rounded, size: 18),
                    label: const Text('ترتيب'),
                  ),
                ),
                const SizedBox(width: 8),
                SegmentedButton<bool>(
                  segments: const [
                    ButtonSegment(
                      value: true,
                      icon: Icon(Icons.grid_view_rounded),
                    ),
                    ButtonSegment(
                      value: false,
                      icon: Icon(Icons.view_list_rounded),
                    ),
                  ],
                  selected: {_grid},
                  showSelectedIcon: false,
                  onSelectionChanged: (value) =>
                      setState(() => _grid = value.first),
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: products.when(
              loading: () => const CatalogLoading(),
              error: (error, _) => CatalogError(
                error: error,
                retry: () => ref.invalidate(productFeedProvider(_query)),
              ),
              data: (page) {
                if (page.items.isEmpty) return const _EmptyProducts();
                return Column(
                  children: [
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: Row(
                        children: [
                          Text(
                            '${page.total} منتج',
                            style: const TextStyle(
                              color: AppColors.gray500,
                              fontSize: 11,
                            ),
                          ),
                          const Spacer(),
                          if (_query.stock != null ||
                              _query.brandId != null ||
                              _query.minPrice != null)
                            TextButton(
                              onPressed: () => setState(
                                () => _query = CatalogQuery(
                                  q: _query.q,
                                  categoryId: _query.categoryId,
                                ),
                              ),
                              child: const Text('مسح الفلاتر'),
                            ),
                        ],
                      ),
                    ),
                    Expanded(
                      child: _grid
                          ? GridView.builder(
                              padding: const EdgeInsets.fromLTRB(16, 6, 16, 20),
                              itemCount: page.items.length,
                              gridDelegate:
                                  const SliverGridDelegateWithFixedCrossAxisCount(
                                    crossAxisCount: 2,
                                    mainAxisSpacing: 12,
                                    crossAxisSpacing: 12,
                                    childAspectRatio: .63,
                                  ),
                              itemBuilder: (context, index) =>
                                  CatalogProductCard(
                                    product: page.items[index],
                                    onFavorite: () =>
                                        _favorite(page.items[index]),
                                  ),
                            )
                          : ListView.separated(
                              padding: const EdgeInsets.fromLTRB(16, 6, 16, 20),
                              itemCount: page.items.length,
                              separatorBuilder: (_, __) =>
                                  const SizedBox(height: 10),
                              itemBuilder: (context, index) =>
                                  CatalogProductCard(
                                    product: page.items[index],
                                    list: true,
                                    onFavorite: () =>
                                        _favorite(page.items[index]),
                                  ),
                            ),
                    ),
                    if (page.totalPages > 1)
                      Padding(
                        padding: const EdgeInsets.fromLTRB(16, 6, 16, 12),
                        child: Row(
                          children: [
                            OutlinedButton(
                              onPressed: page.page > 1
                                  ? () => setState(
                                      () => _query = _query.copyWith(
                                        page: page.page - 1,
                                      ),
                                    )
                                  : null,
                              child: const Text('السابق'),
                            ),
                            Expanded(
                              child: Text(
                                'صفحة ${page.page} من ${page.totalPages}',
                                textAlign: TextAlign.center,
                                style: const TextStyle(fontSize: 11),
                              ),
                            ),
                            OutlinedButton(
                              onPressed: page.page < page.totalPages
                                  ? () => setState(
                                      () => _query = _query.copyWith(
                                        page: page.page + 1,
                                      ),
                                    )
                                  : null,
                              child: const Text('التالي'),
                            ),
                          ],
                        ),
                      ),
                  ],
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _favorite(CatalogProduct product) async {
    try {
      await ref.read(catalogRepositoryProvider).toggleFavorite(product.id);
      ref.invalidate(productFeedProvider(_query));
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(error.toString())));
      }
    }
  }

  void _showSort() {
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const ListTile(
              title: Text(
                'ترتيب المنتجات',
                style: TextStyle(fontWeight: FontWeight.w800),
              ),
            ),
            ...const {
              'featured': 'الأكثر ملاءمة',
              'newest': 'الأحدث',
              'price_asc': 'السعر: من الأقل للأعلى',
              'price_desc': 'السعر: من الأعلى للأقل',
              'rating': 'الأعلى تقييمًا',
              'name': 'الاسم أبجديًا',
            }.entries.map(
              (entry) => RadioListTile<String>(
                value: entry.key,
                groupValue: _query.sort,
                title: Text(entry.value),
                onChanged: (value) {
                  setState(
                    () => _query = _query.copyWith(sort: value, page: 1),
                  );
                  Navigator.pop(context);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showFilters() {
    var stock = _query.stock;
    RangeValues price = RangeValues(
      _query.minPrice ?? 0,
      (_query.maxPrice ?? 5000).clamp(100, 5000),
    );
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheetContext) => StatefulBuilder(
        builder: (context, setSheetState) => SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Align(
                  alignment: Alignment.centerRight,
                  child: Text(
                    'تصفية المنتجات',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
                  ),
                ),
                const SizedBox(height: 18),
                const Align(
                  alignment: Alignment.centerRight,
                  child: Text(
                    'حالة المخزون',
                    style: TextStyle(fontWeight: FontWeight.w700),
                  ),
                ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  children: [
                    ChoiceChip(
                      label: const Text('الكل'),
                      selected: stock == null,
                      onSelected: (_) => setSheetState(() => stock = null),
                    ),
                    ChoiceChip(
                      label: const Text('متوفر'),
                      selected: stock == 'in',
                      onSelected: (_) => setSheetState(() => stock = 'in'),
                    ),
                    ChoiceChip(
                      label: const Text('كمية محدودة'),
                      selected: stock == 'low',
                      onSelected: (_) => setSheetState(() => stock = 'low'),
                    ),
                    ChoiceChip(
                      label: const Text('غير متوفر'),
                      selected: stock == 'out',
                      onSelected: (_) => setSheetState(() => stock = 'out'),
                    ),
                  ],
                ),
                const SizedBox(height: 20),
                Row(
                  children: [
                    const Text(
                      'نطاق السعر',
                      style: TextStyle(fontWeight: FontWeight.w700),
                    ),
                    const Spacer(),
                    Text(
                      '${price.start.round()} - ${price.end.round()} ج.م',
                      style: const TextStyle(color: AppColors.primary),
                    ),
                  ],
                ),
                RangeSlider(
                  values: price,
                  min: 0,
                  max: 5000,
                  divisions: 50,
                  onChanged: (value) => setSheetState(() => price = value),
                ),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () {
                          setState(
                            () => _query = _query.copyWith(
                              clearPrice: true,
                              clearStock: true,
                              clearBrand: true,
                              page: 1,
                            ),
                          );
                          Navigator.pop(sheetContext);
                        },
                        child: const Text('إعادة ضبط'),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: FilledButton(
                        onPressed: () {
                          setState(
                            () => _query = _query.copyWith(
                              stock: stock,
                              minPrice: price.start,
                              maxPrice: price.end,
                              page: 1,
                              clearStock: stock == null,
                            ),
                          );
                          Navigator.pop(sheetContext);
                        },
                        child: const Text('تطبيق'),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _EmptyProducts extends StatelessWidget {
  const _EmptyProducts();
  @override
  Widget build(BuildContext context) => const Center(
    child: Padding(
      padding: EdgeInsets.all(32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.search_off_rounded, size: 76, color: AppColors.gray300),
          SizedBox(height: 14),
          Text(
            'لا توجد منتجات مطابقة',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
          ),
          SizedBox(height: 6),
          Text(
            'جرّب تغيير كلمات البحث أو إزالة بعض الفلاتر',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.gray500),
          ),
        ],
      ),
    ),
  );
}
