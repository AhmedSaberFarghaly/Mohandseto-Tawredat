import 'package:go_router/go_router.dart';

import '../../features/auth/documents_screen.dart';
import '../../features/auth/login_screen.dart';
import '../../features/auth/otp_screen.dart';
import '../../features/auth/registration_screen.dart';
import '../../features/auth/verification_screen.dart';
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
    GoRoute(
      path: '/home',
      name: 'home',
      builder: (context, state) => const HomeScreen(),
    ),
  ],
);
