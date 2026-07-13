import 'package:go_router/go_router.dart';

import '../../features/auth/documents_screen.dart';
import '../../features/auth/login_screen.dart';
import '../../features/auth/otp_screen.dart';
import '../../features/auth/registration_screen.dart';
import '../../features/auth/verification_screen.dart';
import '../../features/auth/two_factor_login_screen.dart';
import '../../features/catalog/categories_screen.dart';
import '../../features/catalog/catalog_search_screen.dart';
import '../../features/catalog/compare_screen.dart';
import '../../features/catalog/favorites_screen.dart';
import '../../features/catalog/product_detail_screen.dart';
import '../../features/catalog/products_screen.dart';
import '../../features/cart/cart_screen.dart';
import '../../features/cart/checkout_screen.dart';
import '../../features/customization/custom_product_wizard_screen.dart';
import '../../features/customization/custom_products_screen.dart';
import '../../features/customization/custom_requests_screen.dart';
import '../../features/approvals/approvals_screen.dart';
import '../../features/rfq/rfqs_screen.dart';
import '../../features/orders/orders_screen.dart';
import '../../features/returns/returns_screen.dart';
import '../../features/finance/finance_screen.dart';
import '../../features/budgets/budgets_screen.dart';
import '../../features/account/account_screen.dart';
import '../../features/engagement/engagement_screens.dart';
import '../api/auth_repository.dart';
import '../api/engagement_repository.dart';
import '../api/checkout_repository.dart';
import '../../features/home/client_shell.dart';
import '../../features/home/home_screen.dart';
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
      path: '/system/:type',
      builder: (_, state) => SystemRuntimeScreen(
        type: state.pathParameters['type']!,
        config: state.extra as MobileAppConfigModel?,
      ),
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
      path: '/two-factor-login',
      builder: (context, state) =>
          TwoFactorLoginScreen(challenge: state.extra! as AuthResult),
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
          builder: (context, state) => const OrdersScreen(),
        ),
        GoRoute(
          path: '/account',
          name: 'account',
          builder: (context, state) => const AccountScreen(),
          routes: [
            GoRoute(
              path: 'profile',
              builder: (_, _) => const ProfileEditorScreen(),
            ),
            GoRoute(
              path: 'company',
              builder: (_, _) => const CompanyAccountScreen(),
            ),
            GoRoute(
              path: 'documents',
              builder: (_, _) => const AccountDocumentsScreen(),
            ),
            GoRoute(
              path: 'branches',
              builder: (_, _) => const BranchesScreen(),
            ),
            GoRoute(
              path: 'users',
              builder: (_, _) => const CompanyUsersScreen(),
            ),
            GoRoute(
              path: 'roles',
              builder: (_, _) => const RolesPermissionsScreen(),
            ),
            GoRoute(
              path: 'approvals',
              builder: (_, _) => const ApprovalSettingsScreen(),
            ),
            GoRoute(
              path: 'cost-centers',
              builder: (_, _) => const CostCentersSettingsScreen(),
            ),
            GoRoute(
              path: 'audit',
              builder: (_, _) => const AccountAuditScreen(),
            ),
            GoRoute(
              path: 'brand',
              builder: (_, _) => const BrandSettingsScreen(),
            ),
            GoRoute(
              path: 'billing',
              builder: (_, _) => const BillingSettingsScreen(),
            ),
            GoRoute(
              path: 'contracts',
              builder: (_, _) => const CompanyContractsScreen(),
            ),
          ],
        ),
        GoRoute(
          path: '/notifications',
          builder: (_, _) => const NotificationsScreen(),
          routes: [
            GoRoute(
              path: 'preferences',
              builder: (_, _) => const NotificationPreferencesScreen(),
            ),
          ],
        ),
        GoRoute(
          path: '/support',
          builder: (_, _) => const SupportHubScreen(),
          routes: [
            GoRoute(
              path: 'tickets',
              builder: (_, _) => const SupportTicketsScreen(),
              routes: [
                GoRoute(
                  path: 'new',
                  builder: (_, state) => CreateSupportTicketScreen(
                    sales: state.uri.queryParameters['sales'] == 'true',
                  ),
                ),
                GoRoute(
                  path: ':id',
                  builder: (_, state) => SupportTicketDetailScreen(
                    id: state.pathParameters['id']!,
                  ),
                ),
              ],
            ),
            GoRoute(
              path: 'callback',
              builder: (_, _) => const CallbackScreen(),
            ),
            GoRoute(path: 'faq', builder: (_, _) => const FaqScreen()),
            GoRoute(
              path: 'content/:slug',
              builder: (_, state) =>
                  ContentScreen(slug: state.pathParameters['slug']!),
            ),
          ],
        ),
        GoRoute(
          path: '/settings',
          builder: (_, _) => const SettingsScreen(),
          routes: [
            GoRoute(
              path: 'password',
              builder: (_, _) => const ChangePasswordScreen(),
            ),
            GoRoute(
              path: '2fa',
              builder: (_, _) => const TwoFactorSettingsScreen(),
            ),
            GoRoute(
              path: 'sessions',
              builder: (_, _) => const SessionsScreen(),
            ),
            GoRoute(
              path: 'delete',
              builder: (_, _) => const DeleteAccountScreen(),
            ),
          ],
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
      path: '/orders/:id',
      name: 'order-detail',
      builder: (context, state) =>
          OrderDetailScreen(id: state.pathParameters['id']!),
    ),
    GoRoute(
      path: '/returns',
      name: 'returns',
      builder: (context, state) => const ReturnsScreen(),
    ),
    GoRoute(
      path: '/returns/new',
      name: 'return-new',
      builder: (context, state) => const CreateReturnScreen(),
    ),
    GoRoute(
      path: '/returns/:id',
      name: 'return-detail',
      builder: (context, state) =>
          ReturnDetailScreen(id: state.pathParameters['id']!),
    ),
    GoRoute(
      path: '/finance',
      name: 'finance',
      builder: (context, state) => const FinanceScreen(),
    ),
    GoRoute(
      path: '/finance/invoices/:id',
      name: 'invoice-detail',
      builder: (context, state) =>
          InvoiceDetailScreen(id: state.pathParameters['id']!),
    ),
    GoRoute(
      path: '/budgets',
      name: 'budgets',
      builder: (context, state) => const BudgetsScreen(),
    ),
    GoRoute(
      path: '/budgets/centers/:id',
      name: 'budget-center',
      builder: (context, state) =>
          BudgetCenterScreen(id: state.pathParameters['id']!),
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
    GoRoute(
      path: '/cart',
      name: 'cart',
      builder: (context, state) => const CartScreen(),
    ),
    GoRoute(
      path: '/checkout',
      name: 'checkout',
      builder: (context, state) => const CheckoutScreen(),
    ),
    GoRoute(
      path: '/checkout/success',
      name: 'checkout-success',
      builder: (context, state) =>
          OrderSuccessScreen(order: state.extra! as OrderCreated),
    ),
    GoRoute(
      path: '/custom-products',
      name: 'custom-products',
      builder: (context, state) => CustomProductsScreen(
        productId: state.uri.queryParameters['productId'],
      ),
    ),
    GoRoute(
      path: '/custom-products/:templateId',
      name: 'custom-product-wizard',
      builder: (context, state) => CustomProductWizardScreen(
        templateId: state.pathParameters['templateId']!,
      ),
    ),
    GoRoute(
      path: '/custom-requests',
      name: 'custom-requests',
      builder: (context, state) => const CustomRequestsScreen(),
    ),
    GoRoute(
      path: '/custom-requests/:requestId',
      name: 'custom-request-detail',
      builder: (context, state) => CustomRequestDetailScreen(
        requestId: state.pathParameters['requestId']!,
      ),
    ),
    GoRoute(
      path: '/approvals',
      name: 'approvals',
      builder: (context, state) => const ApprovalsScreen(),
    ),
    GoRoute(
      path: '/approvals/:id',
      name: 'approval-detail',
      builder: (context, state) =>
          ApprovalDetailScreen(id: state.pathParameters['id']!),
    ),
    GoRoute(
      path: '/rfqs',
      name: 'rfqs',
      builder: (context, state) => const RfqsScreen(),
    ),
    GoRoute(
      path: '/rfqs/new',
      name: 'rfq-new',
      builder: (context, state) => const CreateRfqScreen(),
    ),
    GoRoute(
      path: '/rfqs/:id',
      name: 'rfq-detail',
      builder: (context, state) =>
          RfqDetailScreen(id: state.pathParameters['id']!),
    ),
  ],
);
