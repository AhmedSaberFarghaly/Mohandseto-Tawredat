import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'catalog_widgets.dart';

class CategoriesScreen extends ConsumerWidget {
  const CategoriesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(categoriesProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('كل الأقسام'),
        actions: [
          IconButton(
            onPressed: () => context.push('/products'),
            icon: const Icon(Icons.search_rounded),
          ),
        ],
      ),
      body: categories.when(
        loading: () => const CatalogLoading(message: 'جاري تحميل الأقسام...'),
        error: (error, _) => CatalogError(
          error: error,
          retry: () => ref.invalidate(categoriesProvider),
        ),
        data: (items) => ListView.separated(
          padding: const EdgeInsets.all(16),
          itemCount: items.length,
          separatorBuilder: (_, __) => const SizedBox(height: 10),
          itemBuilder: (context, index) {
            final item = items[index];
            return Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(AppRadius.lg),
                border: Border.all(color: AppColors.gray200),
              ),
              child: ExpansionTile(
                shape: const Border(),
                leading: Container(
                  width: 46,
                  height: 46,
                  decoration: BoxDecoration(
                    color: index.isEven
                        ? AppColors.primaryTint
                        : AppColors.successTint,
                    borderRadius: BorderRadius.circular(AppRadius.md),
                  ),
                  child: Icon(
                    categoryIcon(item.iconName),
                    color: index.isEven ? AppColors.primary : AppColors.success,
                  ),
                ),
                title: Text(
                  item.nameAr,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                subtitle: Text(
                  '${item.children.length} أقسام فرعية',
                  style: const TextStyle(
                    fontSize: 10,
                    color: AppColors.gray500,
                  ),
                ),
                children: [
                  ...item.children.map(
                    (child) => ListTile(
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 24,
                      ),
                      title: Text(
                        child.nameAr,
                        style: const TextStyle(fontSize: 12),
                      ),
                      trailing: const Icon(
                        Icons.arrow_back_ios_new_rounded,
                        size: 13,
                      ),
                      onTap: () => context.push(
                        '/products?categoryId=${child.id}&title=${Uri.encodeComponent(child.nameAr)}',
                      ),
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                    child: OutlinedButton(
                      onPressed: () => context.push(
                        '/products?categoryId=${item.id}&title=${Uri.encodeComponent(item.nameAr)}',
                      ),
                      child: Text('عرض كل منتجات ${item.nameAr}'),
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}
