import 'package:go_router/go_router.dart';

import '../../features/startup/splash_screen.dart';

/// Central route table. Routes are added per milestone as screens are built.
final appRouter = GoRouter(
  initialLocation: '/splash',
  routes: [
    GoRoute(
      path: '/splash',
      name: 'splash',
      builder: (context, state) => const SplashScreen(),
    ),
  ],
);
