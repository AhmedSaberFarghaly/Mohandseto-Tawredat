import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

class AuthUser {
  AuthUser.fromJson(Map<String, dynamic> json)
    : id = json['id'] as String,
      fullName = json['fullName'] as String,
      phone = json['phone'] as String,
      email = json['email'] as String?,
      tenantId = json['tenantId'] as String?,
      tenantStatus = json['tenantStatus'] as String?,
      roles = (json['roles'] as List).cast<String>();

  final String id;
  final String fullName;
  final String phone;
  final String? email;
  final String? tenantId;
  final String? tenantStatus;
  final List<String> roles;
}

class AuthResult {
  AuthResult.fromJson(Map<String, dynamic> json)
    : isNewUser = json['isNewUser'] as bool,
      user = json['user'] == null
          ? null
          : AuthUser.fromJson(json['user'] as Map<String, dynamic>),
      accessToken = json['accessToken'] as String?,
      refreshToken = json['refreshToken'] as String?,
      requiresTwoFactor = json['requiresTwoFactor'] as bool? ?? false,
      challengeToken = json['challengeToken'] as String?,
      developmentCode = json['developmentCode'] as String?;

  final bool isNewUser;
  final AuthUser? user;
  final String? accessToken;
  final String? refreshToken;
  final bool requiresTwoFactor;
  final String? challengeToken, developmentCode;
}

class AuthRepository {
  AuthRepository(this._api, this._tokens);
  final ApiClient _api;
  final TokenStore _tokens;

  Future<T> _call<T>(Future<T> Function() body) async {
    try {
      return await body();
    } on DioException catch (error) {
      throw error.error is ApiFailure
          ? error.error as ApiFailure
          : ApiFailure('حدث خطأ غير متوقع');
    }
  }

  Future<String?> requestOtp(String phone, String purpose) => _call(() async {
    final response = await _api.dio.post(
      '/api/auth/otp/request',
      data: {'phone': phone, 'purpose': purpose},
    );
    return response.data['devCode'] as String?;
  });

  Future<AuthResult> verifyOtp(String phone, String code) => _call(() async {
    final response = await _api.dio.post(
      '/api/auth/otp/verify',
      data: {'phone': phone, 'code': code},
    );
    final result = AuthResult.fromJson(response.data as Map<String, dynamic>);
    await _persist(result);
    return result;
  });

  Future<AuthResult> loginWithEmail(String email, String password) => _call(
    () async {
      final response = await _api.dio.post(
        '/api/auth/login',
        data: {'email': email, 'password': password},
      );
      final result = AuthResult.fromJson(response.data as Map<String, dynamic>);
      await _persist(result);
      return result;
    },
  );

  Future<AuthResult> registerCompany(Map<String, dynamic> payload) => _call(
    () async {
      final response = await _api.dio.post(
        '/api/auth/register-company',
        data: payload,
      );
      final result = AuthResult.fromJson(response.data as Map<String, dynamic>);
      await _persist(result);
      return result;
    },
  );

  Future<AuthResult> verifyTwoFactor(String challenge, String code) => _call(
    () async {
      final response = await _api.dio.post(
        '/api/auth/2fa/verify',
        data: {'challengeToken': challenge, 'code': code},
      );
      final result = AuthResult.fromJson(response.data as Map<String, dynamic>);
      await _persist(result);
      return result;
    },
  );

  Future<Map<String, dynamic>> verificationStatus() => _call(() async {
    final response = await _api.dio.get('/api/company/verification-status');
    return response.data as Map<String, dynamic>;
  });

  Future<void> uploadDocument(
    String type,
    String filename,
    List<int> bytes,
    String contentType,
  ) => _call(() async {
    final form = FormData.fromMap({
      'type': type,
      'file': MultipartFile.fromBytes(
        bytes,
        filename: filename,
        contentType: DioMediaType.parse(contentType),
      ),
    });
    await _api.dio.post('/api/company/documents', data: form);
  });

  Future<void> logout() async {
    final refreshToken = await _tokens.refresh;
    try {
      if (refreshToken != null) {
        await _api.dio.post(
          '/api/auth/logout',
          data: {'refreshToken': refreshToken},
        );
      }
    } finally {
      await _tokens.clear();
    }
  }

  Future<void> _persist(AuthResult result) async {
    if (result.accessToken != null && result.refreshToken != null) {
      await _tokens.save(result.accessToken!, result.refreshToken!);
    }
  }
}

final authRepositoryProvider = Provider(
  (ref) => AuthRepository(
    ref.watch(apiClientProvider),
    ref.watch(tokenStoreProvider),
  ),
);

class CurrentUserNotifier extends Notifier<AuthUser?> {
  @override
  AuthUser? build() => null;

  void setUser(AuthUser? user) => state = user;
}

final currentUserProvider = NotifierProvider<CurrentUserNotifier, AuthUser?>(
  CurrentUserNotifier.new,
);
