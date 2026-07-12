import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/catalog_repository.dart';
import '../../core/theme/app_tokens.dart';
import 'catalog_widgets.dart';

class FavoritesScreen extends ConsumerWidget {
  const FavoritesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final favorites = ref.watch(favoritesProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('المفضلة')),
      body: favorites.when(
        loading: () => const CatalogLoading(message: 'جاري تحميل المفضلة...'),
        error: (error, _) => CatalogError(
          error: error,
          retry: () => ref.invalidate(favoritesProvider),
        ),
        data: (items) {
          if (items.isEmpty) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(32),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.favorite_border_rounded,
                      size: 78,
                      color: AppColors.gray300,
                    ),
                    SizedBox(height: 16),
                    Text(
                      'قائمة المفضلة فارغة',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                    SizedBox(height: 6),
                    Text(
                      'اضغط على علامة القلب لحفظ المنتجات والرجوع إليها بسهولة',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: AppColors.gray500, height: 1.6),
                    ),
                  ],
                ),
              ),
            );
          }
          return GridView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: items.length,
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
              childAspectRatio: .63,
            ),
            itemBuilder: (context, index) => CatalogProductCard(
              product: items[index],
              onFavorite: () async {
                await ref
                    .read(catalogRepositoryProvider)
                    .toggleFavorite(items[index].id);
                ref.invalidate(favoritesProvider);
                ref.invalidate(productFeedProvider);
              },
            ),
          );
        },
      ),
    );
  }
}
