import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://localhost:5199',
);

class TokenStore {
  static const _storage = FlutterSecureStorage();
  static const _kAccess = 'access_token';
  static const _kRefresh = 'refresh_token';

  Future<String?> get access => _storage.read(key: _kAccess);
  Future<String?> get refresh => _storage.read(key: _kRefresh);

  Future<void> save(String access, String refresh) async {
    await _storage.write(key: _kAccess, value: access);
    await _storage.write(key: _kRefresh, value: refresh);
  }

  Future<void> clear() async {
    await _storage.delete(key: _kAccess);
    await _storage.delete(key: _kRefresh);
  }
}

class ApiFailure implements Exception {
  ApiFailure(this.message, {this.statusCode});
  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class ApiClient {
  ApiClient(this._tokens) {
    dio = Dio(
      BaseOptions(
        baseUrl: apiBaseUrl,
        connectTimeout: const Duration(seconds: 15),
        receiveTimeout: const Duration(seconds: 30),
      ),
    );
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _tokens.access;
          if (token != null) options.headers['Authorization'] = 'Bearer $token';
          handler.next(options);
        },
        onError: (error, handler) async {
          final path = error.requestOptions.path;
          final isAuthOperation =
              path == '/api/auth/login' ||
              path == '/api/auth/refresh' ||
              path.contains('/api/auth/otp/');
          final alreadyRetried = error.requestOptions.extra['retried'] == true;
          if (error.response?.statusCode == 401 &&
              !isAuthOperation &&
              !alreadyRetried &&
              await _refreshAccessToken()) {
            final token = await _tokens.access;
            final retry = error.requestOptions;
            retry.extra['retried'] = true;
            retry.headers['Authorization'] = 'Bearer $token';
            try {
              handler.resolve(await dio.fetch(retry));
              return;
            } on DioException catch (retryError) {
              error = retryError;
            }
          }
          final data = error.response?.data;
          final message = switch (data) {
            {'title': final String title} => title,
            _ => 'تعذر الاتصال بالخادم، تحقق من الشبكة وحاول مجددًا',
          };
          handler.reject(
            DioException(
              requestOptions: error.requestOptions,
              response: error.response,
              error: ApiFailure(
                message,
                statusCode: error.response?.statusCode,
              ),
            ),
          );
        },
      ),
    );
  }

  final TokenStore _tokens;
  Completer<bool>? _refreshCompleter;
  late final Dio dio;

  Future<bool> _refreshAccessToken() async {
    if (_refreshCompleter case final pending?) return pending.future;
    final completer = Completer<bool>();
    _refreshCompleter = completer;
    try {
      final refreshToken = await _tokens.refresh;
      if (refreshToken == null) {
        completer.complete(false);
        return false;
      }
      final refreshClient = Dio(
        BaseOptions(
          baseUrl: apiBaseUrl,
          connectTimeout: const Duration(seconds: 15),
          receiveTimeout: const Duration(seconds: 30),
        ),
      );
      final response = await refreshClient.post(
        '/api/auth/refresh',
        data: {'refreshToken': refreshToken},
      );
      final data = response.data as Map<String, dynamic>;
      final access = data['accessToken'] as String?;
      final refresh = data['refreshToken'] as String?;
      if (access == null || refresh == null) {
        await _tokens.clear();
        completer.complete(false);
        return false;
      }
      await _tokens.save(access, refresh);
      completer.complete(true);
      return true;
    } catch (_) {
      await _tokens.clear();
      completer.complete(false);
      return false;
    } finally {
      _refreshCompleter = null;
    }
  }
}

final tokenStoreProvider = Provider((_) => TokenStore());
final apiClientProvider = Provider(
  (ref) => ApiClient(ref.watch(tokenStoreProvider)),
);
