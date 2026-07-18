import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/widgets/skeleton.dart';
import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';

class CatalogSearchScreen extends ConsumerStatefulWidget {
  const CatalogSearchScreen({super.key});

  @override
  ConsumerState<CatalogSearchScreen> createState() =>
      _CatalogSearchScreenState();
}

class _CatalogSearchScreenState extends ConsumerState<CatalogSearchScreen> {
  final _controller = TextEditingController();
  Timer? _debounce;
  String _query = '';

  @override
  void dispose() {
    _controller.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _changed(String value) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 250), () {
      if (mounted) setState(() => _query = value.trim());
    });
  }

  void _submit(String value) {
    value = value.trim();
    if (value.length < 2) return;
    context.go('/products?q=${Uri.encodeComponent(value)}&title=نتائج البحث');
  }

  @override
  Widget build(BuildContext context) {
    final recent = ref.watch(recentSearchesProvider);
    final suggestions = _query.length < 2
        ? null
        : ref.watch(searchSuggestionsProvider(_query));
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        titleSpacing: 0,
        title: TextField(
          controller: _controller,
          autofocus: true,
          onChanged: _changed,
          onSubmitted: _submit,
          textInputAction: TextInputAction.search,
          decoration: InputDecoration(
            hintText: 'ابحث عن منتج أو كود...',
            prefixIcon: const Icon(Icons.search_rounded),
            suffixIcon: _controller.text.isEmpty
                ? null
                : IconButton(
                    onPressed: () {
                      _controller.clear();
                      setState(() => _query = '');
                    },
                    icon: const Icon(Icons.close_rounded),
                  ),
            filled: true,
            fillColor: AppColors.gray50,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(AppRadius.md),
              borderSide: BorderSide.none,
            ),
          ),
        ),
        actions: const [SizedBox(width: 12)],
      ),
      body: _query.length < 2
          ? _RecentSearches(
              recent: recent,
              onSelect: _submit,
              onClear: () async {
                await ref.read(catalogRepositoryProvider).clearRecentSearches();
                ref.invalidate(recentSearchesProvider);
              },
            )
          : suggestions!.when(
              loading: () => const ListSkeleton(),
              error: (error, _) =>
                  Center(child: Text('تعذر تحميل الاقتراحات: $error')),
              data: (items) => ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  Text(
                    'اقتراحات البحث (${items.length})',
                    style: const TextStyle(
                      color: AppColors.gray500,
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  ...items.map(
                    (item) => Container(
                      margin: const EdgeInsets.only(bottom: 8),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(15),
                        border: Border.all(color: AppColors.gray150),
                      ),
                      child: ListTile(
                        leading: const Icon(
                          Icons.search_rounded,
                          color: AppColors.primary,
                        ),
                        title: Text(item),
                        trailing: const Icon(
                          Icons.north_west_rounded,
                          color: AppColors.gray400,
                          size: 18,
                        ),
                        onTap: () => _submit(item),
                      ),
                    ),
                  ),
                  if (items.isEmpty)
                    const Padding(
                      padding: EdgeInsets.only(top: 80),
                      child: Column(
                        children: [
                          Icon(
                            Icons.search_off_rounded,
                            size: 72,
                            color: AppColors.gray300,
                          ),
                          SizedBox(height: 12),
                          Text('لا توجد اقتراحات مطابقة'),
                        ],
                      ),
                    ),
                ],
              ),
            ),
    );
  }
}

class _RecentSearches extends StatelessWidget {
  const _RecentSearches({
    required this.recent,
    required this.onSelect,
    required this.onClear,
  });
  final AsyncValue<List<String>> recent;
  final ValueChanged<String> onSelect;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      Row(
        children: [
          const Expanded(
            child: Text(
              'عمليات البحث الأخيرة',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
          ),
          if (recent.value?.isNotEmpty == true)
            TextButton(onPressed: onClear, child: const Text('مسح الكل')),
        ],
      ),
      recent.when(
        loading: () => const LinearProgressIndicator(),
        error: (_, __) => const Text(
          'سجّل الدخول لعرض سجل البحث',
          style: TextStyle(color: AppColors.gray500),
        ),
        data: (items) => Column(
          children: items
              .map(
                (item) => ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(
                    Icons.history_rounded,
                    color: AppColors.gray400,
                  ),
                  title: Text(item),
                  trailing: const Icon(
                    Icons.north_west_rounded,
                    color: AppColors.gray400,
                    size: 18,
                  ),
                  onTap: () => onSelect(item),
                ),
              )
              .toList(),
        ),
      ),
      const SizedBox(height: 24),
      const Text(
        'أكثر ما يبحث عنه عملاؤنا',
        style: TextStyle(fontWeight: FontWeight.w700),
      ),
      const SizedBox(height: 12),
      Wrap(
        spacing: 8,
        runSpacing: 8,
        children:
            ['ورق تصوير', 'أقلام', 'أحبار طابعات', 'منظفات', 'كراسي مكتبية']
                .map(
                  (item) => ActionChip(
                    avatar: const Icon(Icons.trending_up_rounded, size: 17),
                    label: Text(item),
                    onPressed: () => onSelect(item),
                  ),
                )
                .toList(),
      ),
    ],
  );
}
