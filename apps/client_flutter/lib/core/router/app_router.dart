import 'package:go_router/go_router.dart';

import '../../features/auth/documents_screen.dart';
import '../../features/auth/login_screen.dart';
import '../../features/auth/otp_screen.dart';
import '../../features/auth/registration_screen.dart';
import '../../features/auth/verification_screen.dart';
import '../../features/catalog/categories_screen.dart';
import '../../features/catalog/catalog_search_screen.dart';
import '../../features/catalog/compare_screen.dart';
import '../../features/catalog/favorites_screen.dart';
import '../../features/catalog/product_detail_screen.dart';
import '../../features/catalog/products_screen.dart';
import '../../features/home/client_shell.dart';
import '../../features/home/home_screen.dart';
import '../../features/home/placeholder_screen.dart';
import 'package:flutter/material.dart';
import '../../features/startup/splash_screen.dart';

final appRouter = GoRouter(
  initialLocation: '/splash',
  routes: [
    GoRoute(
      path: '/splash',
      name: 'splash',
      builder: (context, state) => const SplashScreen(),
    ),
    GoRoute(
      path: '/login',
      name: 'login',
      builder: (context, state) => const LoginScreen(),
    ),
    GoRoute(
      path: '/otp',
      name: 'otp',
      builder: (context, state) => OtpScreen(
        phone: state.uri.queryParameters['phone'] ?? '',
        devCode: state.uri.queryParameters['devCode'],
      ),
    ),
    GoRoute(
      path: '/register',
      name: 'register',
      builder: (context, state) => RegistrationScreen(
        phone: state.uri.queryParameters['phone'] ?? '',
        registrationCode: state.uri.queryParameters['code'],
      ),
    ),
    GoRoute(
      path: '/documents',
      name: 'documents',
      builder: (context, state) => const DocumentsScreen(),
    ),
    GoRoute(
      path: '/verification',
      name: 'verification',
      builder: (context, state) => const VerificationScreen(),
    ),
    ShellRoute(
      builder: (context, state, child) => ClientShell(child: child),
      routes: [
        GoRoute(
          path: '/home',
          name: 'home',
          builder: (context, state) => const HomeScreen(),
        ),
        GoRoute(
          path: '/categories',
          name: 'categories',
          builder: (context, state) => const CategoriesScreen(),
        ),
        GoRoute(
          path: '/favorites',
          name: 'favorites',
          builder: (context, state) => const FavoritesScreen(),
        ),
        GoRoute(
          path: '/orders',
          name: 'orders',
          builder: (context, state) => const PlaceholderScreen(
            title: 'الطلبات',
            icon: Icons.receipt_long_outlined,
            message: 'سيتم تنفيذ رحلة الطلبات والتتبع في المرحلة السادسة.',
          ),
        ),
        GoRoute(
          path: '/account',
          name: 'account',
          builder: (context, state) => const PlaceholderScreen(
            title: 'حسابي',
            icon: Icons.person_outline_rounded,
            message: 'سيتم استكمال بيانات الحساب والمستخدمين والإعدادات.',
          ),
        ),
        GoRoute(
          path: '/products',
          name: 'products',
          builder: (context, state) => ProductsScreen(
            categoryId: state.uri.queryParameters['categoryId'],
            title: state.uri.queryParameters['title'],
            initialQuery: state.uri.queryParameters['q'],
            initialSort: state.uri.queryParameters['sort'],
            featured: state.uri.queryParameters['featured'] == 'true'
                ? true
                : null,
          ),
        ),
      ],
    ),
    GoRoute(
      path: '/products/:idOrSlug',
      name: 'product-detail',
      builder: (context, state) =>
          ProductDetailScreen(idOrSlug: state.pathParameters['idOrSlug']!),
    ),
    GoRoute(
      path: '/search',
      name: 'catalog-search',
      builder: (context, state) => const CatalogSearchScreen(),
    ),
    GoRoute(
      path: '/compare',
      name: 'compare',
      builder: (context, state) => const CompareScreen(),
    ),
  ],
);
