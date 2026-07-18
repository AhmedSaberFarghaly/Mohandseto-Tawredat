import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import '../../core/widgets/skeleton.dart';
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
  late final ScrollController _scroll;
  bool _grid = true;
  Timer? _debounce;

  final List<CatalogProduct> _items = [];
  int _total = 0;
  bool _loadingMore = false;
  bool _initialLoading = true;
  bool _hasMore = true;
  Object? _error;
  int _requestId = 0;

  @override
  void initState() {
    super.initState();
    _search = TextEditingController(text: widget.initialQuery);
    _scroll = ScrollController()..addListener(_onScroll);
    _query = CatalogQuery(
      categoryId: widget.categoryId,
      q: widget.initialQuery,
      sort: widget.initialSort ?? 'featured',
      featured: widget.featured,
    );
    _load(reset: true);
  }

  @override
  void dispose() {
    _debounce?.cancel();
    _search.dispose();
    _scroll.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_hasMore || _loadingMore || _initialLoading) return;
    if (_scroll.position.pixels >= _scroll.position.maxScrollExtent - 400) {
      _load();
    }
  }

  Future<void> _load({bool reset = false}) async {
    final requestId = ++_requestId;
    if (reset) {
      setState(() {
        _initialLoading = _items.isEmpty;
        _loadingMore = _items.isNotEmpty;
        _error = null;
        _query = _query.copyWith(page: 1);
      });
    } else {
      setState(() {
        _loadingMore = true;
        _query = _query.copyWith(page: _query.page + 1);
      });
    }
    try {
      final page = await ref.read(catalogRepositoryProvider).products(_query);
      if (!mounted || requestId != _requestId) return;
      setState(() {
        if (reset) _items.clear();
        _items.addAll(page.items);
        _total = page.total;
        _hasMore = page.page < page.totalPages;
        _initialLoading = false;
        _loadingMore = false;
      });
    } catch (error) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _error = error;
        _initialLoading = false;
        _loadingMore = false;
      });
    }
  }

  void _applyQuery(CatalogQuery query) {
    _query = query;
    _load(reset: true);
  }

  void _searchChanged(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 450), () {
      if (mounted) _applyQuery(_query.copyWith(q: value, page: 1));
    });
  }

  int get _activeFilterCount => [
    _query.stock,
    _query.brandId,
    _query.minPrice,
    _query.maxPrice,
  ].where((value) => value != null).length;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(widget.title ?? 'المنتجات'),
        actions: [
          IconButton(
            tooltip: 'بحث',
            onPressed: () => context.push('/search'),
            icon: const Icon(Icons.search_rounded),
          ),
          IconButton(
            tooltip: 'مقارنة',
            onPressed: () => context.push('/compare'),
            icon: const Icon(Icons.compare_arrows_rounded),
          ),
        ],
      ),
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
                          _applyQuery(_query.copyWith(q: '', page: 1));
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
                    label: Text(
                      _activeFilterCount == 0
                          ? 'تصفية'
                          : 'تصفية ($_activeFilterCount)',
                    ),
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
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_initialLoading) return const CatalogLoading();
    if (_error != null && _items.isEmpty) {
      return CatalogError(error: _error!, retry: () => _load(reset: true));
    }
    if (_items.isEmpty) return const _EmptyProducts();
    final extra = _loadingMore ? (_grid ? 2 : 1) : 0;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 5,
                ),
                decoration: BoxDecoration(
                  color: AppColors.primaryTint,
                  borderRadius: BorderRadius.circular(AppRadius.pill),
                ),
                child: Text(
                  '$_total منتج متاح',
                  style: const TextStyle(
                    color: AppColors.primary,
                    fontSize: 10,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              const Spacer(),
              if (_query.stock != null ||
                  _query.brandId != null ||
                  _query.minPrice != null)
                TextButton(
                  onPressed: () => _applyQuery(
                    CatalogQuery(q: _query.q, categoryId: _query.categoryId),
                  ),
                  child: const Text('مسح الفلاتر'),
                ),
            ],
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => _load(reset: true),
            child: _grid
                ? GridView.builder(
                    controller: _scroll,
                    cacheExtent: 600,
                    padding: const EdgeInsets.fromLTRB(16, 6, 16, 20),
                    itemCount: _items.length + extra,
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                          crossAxisCount: 2,
                          mainAxisSpacing: 12,
                          crossAxisSpacing: 12,
                          childAspectRatio: .56,
                        ),
                    itemBuilder: (context, index) => index >= _items.length
                        ? const ProductCardSkeleton()
                        : CatalogProductCard(
                            product: _items[index],
                            onFavorite: () => _favorite(index),
                          ),
                  )
                : ListView.separated(
                    controller: _scroll,
                    cacheExtent: 600,
                    padding: const EdgeInsets.fromLTRB(16, 6, 16, 20),
                    itemCount: _items.length + extra,
                    separatorBuilder: (_, __) => const SizedBox(height: 10),
                    itemBuilder: (context, index) => index >= _items.length
                        ? const ProductCardSkeleton()
                        : CatalogProductCard(
                            product: _items[index],
                            list: true,
                            onFavorite: () => _favorite(index),
                          ),
                  ),
          ),
        ),
      ],
    );
  }

  Future<void> _favorite(int index) async {
    final product = _items[index];
    // optimistic flip for instant feedback
    setState(() => _items[index] = product.withFavorite(!product.isFavorite));
    try {
      await ref.read(catalogRepositoryProvider).toggleFavorite(product.id);
    } catch (error) {
      if (!mounted) return;
      setState(() => _items[index] = product);
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(error.toString())));
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
                style: TextStyle(fontWeight: FontWeight.w700),
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
                  _applyQuery(_query.copyWith(sort: value, page: 1));
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
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
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
                          _applyQuery(
                            _query.copyWith(
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
                          _applyQuery(
                            _query.copyWith(
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
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
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
