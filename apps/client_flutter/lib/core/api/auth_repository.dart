import 'package:dio/dio.dart';
import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_sign_in/google_sign_in.dart';

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
      developmentCode = json['developmentCode'] as String?,
      prefillEmail = json['prefillEmail'] as String?,
      prefillName = json['prefillName'] as String?;

  final bool isNewUser;
  final AuthUser? user;
  final String? accessToken;
  final String? refreshToken;
  final bool requiresTwoFactor;
  final String? challengeToken, developmentCode, prefillEmail, prefillName;
}

class ExternalProviderModel {
  const ExternalProviderModel(this.code, this.name, this.enabled);
  factory ExternalProviderModel.fromJson(Map<String, dynamic> json) =>
      ExternalProviderModel(
        json['code'] as String,
        json['displayName'] as String,
        json['enabled'] as bool,
      );
  final String code, name;
  final bool enabled;
}

class ExternalAuthChallengeModel {
  const ExternalAuthChallengeModel({
    required this.provider,
    required this.clientId,
    required this.discoveryUrl,
    required this.redirectUrl,
    required this.scopes,
    required this.token,
    this.iosClientId,
  });
  factory ExternalAuthChallengeModel.fromJson(Map<String, dynamic> json) =>
      ExternalAuthChallengeModel(
        provider: json['provider'] as String,
        clientId: json['clientId'] as String,
        discoveryUrl: json['discoveryUrl'] as String,
        redirectUrl: json['redirectUrl'] as String,
        scopes: List<String>.from(json['scopes'] as List),
        token: json['challengeToken'] as String,
        iosClientId: json['iosClientId'] as String?,
      );
  final String provider, clientId, discoveryUrl, redirectUrl, token;
  final String? iosClientId;
  final List<String> scopes;
}

class LinkedExternalIdentityModel {
  const LinkedExternalIdentityModel(this.provider, this.email, this.linkedAt);
  factory LinkedExternalIdentityModel.fromJson(Map<String, dynamic> json) =>
      LinkedExternalIdentityModel(
        json['provider'] as String,
        json['email'] as String?,
        DateTime.parse(json['linkedAt'] as String),
      );
  final String provider;
  final String? email;
  final DateTime linkedAt;
}

class AuthRepository {
  AuthRepository(this._api, this._tokens);
  final ApiClient _api;
  final TokenStore _tokens;
  String? _initializedGoogleClient;

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

  Future<List<ExternalProviderModel>> externalProviders() => _call(() async {
    final response = await _api.dio.get('/api/auth/external/providers');
    return (response.data as List)
        .map(
          (item) =>
              ExternalProviderModel.fromJson(item as Map<String, dynamic>),
        )
        .toList();
  });

  Future<ExternalAuthChallengeModel> beginExternal(String provider) =>
      _call(() async {
        final response = await _api.dio.post(
          '/api/auth/external/challenge',
          data: {'provider': provider},
        );
        return ExternalAuthChallengeModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      });

  Future<AuthResult> loginExternal({
    required String provider,
    required String idToken,
    required String challengeToken,
  }) => _call(() async {
    final response = await _api.dio.post(
      '/api/auth/external/login',
      data: {
        'provider': provider,
        'idToken': idToken,
        'challengeToken': challengeToken,
      },
    );
    final result = AuthResult.fromJson(response.data as Map<String, dynamic>);
    await _persist(result);
    return result;
  });

  Future<AuthResult?> loginWithExternalProvider(String provider) async {
    final authorization = await _authorizeExternal(provider);
    if (authorization == null) return null;
    return loginExternal(
      provider: provider,
      idToken: authorization.idToken,
      challengeToken: authorization.challenge.token,
    );
  }

  Future<List<LinkedExternalIdentityModel>> linkedExternal() => _call(() async {
    final response = await _api.dio.get('/api/auth/external/linked');
    return (response.data as List)
        .map(
          (item) => LinkedExternalIdentityModel.fromJson(
            item as Map<String, dynamic>,
          ),
        )
        .toList();
  });

  Future<LinkedExternalIdentityModel?> linkExternalProvider(
    String provider,
  ) async {
    final authorization = await _authorizeExternal(provider);
    if (authorization == null) return null;
    return _call(() async {
      final response = await _api.dio.post(
        '/api/auth/external/link',
        data: {
          'provider': provider,
          'idToken': authorization.idToken,
          'challengeToken': authorization.challenge.token,
        },
      );
      return LinkedExternalIdentityModel.fromJson(
        response.data as Map<String, dynamic>,
      );
    });
  }

  Future<({ExternalAuthChallengeModel challenge, String idToken})?>
  _authorizeExternal(String provider) async {
    final challenge = await beginExternal(provider);
    if (provider == 'google') return _authorizeGoogle(challenge);
    try {
      final authorization = await const FlutterAppAuth()
          .authorizeAndExchangeCode(
            AuthorizationTokenRequest(
              challenge.clientId,
              challenge.redirectUrl,
              discoveryUrl: challenge.discoveryUrl,
              scopes: challenge.scopes,
              nonce: challenge.token,
              promptValues: const ['select_account'],
            ),
          );
      final idToken = authorization.idToken;
      if (idToken == null || idToken.isEmpty) {
        throw ApiFailure('لم يُرجع مزود الدخول رمز هوية صالحًا');
      }
      return (challenge: challenge, idToken: idToken);
    } on FlutterAppAuthUserCancelledException {
      return null;
    }
  }

  Future<({ExternalAuthChallengeModel challenge, String idToken})?>
  _authorizeGoogle(ExternalAuthChallengeModel challenge) async {
    try {
      if (_initializedGoogleClient == null) {
        await GoogleSignIn.instance.initialize(
          serverClientId: challenge.clientId,
          clientId: defaultTargetPlatform == TargetPlatform.iOS
              ? challenge.iosClientId
              : null,
        );
        _initializedGoogleClient = challenge.clientId;
      } else if (_initializedGoogleClient != challenge.clientId) {
        throw ApiFailure('تم تغيير إعداد Google أثناء تشغيل التطبيق؛ أعد فتحه');
      }
      final account = await GoogleSignIn.instance.authenticate();
      final idToken = account.authentication.idToken;
      if (idToken == null || idToken.isEmpty) {
        throw ApiFailure('لم يُرجع Google رمز هوية صالحًا');
      }
      return (challenge: challenge, idToken: idToken);
    } on GoogleSignInException catch (error) {
      if (error.code == GoogleSignInExceptionCode.canceled) return null;
      throw ApiFailure('تعذر تسجيل الدخول عبر Google (${error.code.name})');
    }
  }

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
