// lib/core/api/api_client.dart

import 'package:dio/dio.dart';

class ApiClient {
  ApiClient({required String baseUrl}) {
    _dio = Dio(BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: {'Content-Type': 'application/json'},
    ));

    _dio.interceptors.add(LogInterceptor(responseBody: true));
    _dio.interceptors.add(_AuthInterceptor());
  }

  late final Dio _dio;
  Dio get dio => _dio;
}

class _AuthInterceptor extends Interceptor {
  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    // TODO: JWT-Token aus SecureStorage lesen
    // final token = await secureStorage.read('jwt');
    // if (token != null) options.headers['Authorization'] = 'Bearer $token';
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err.response?.statusCode == 401) {
      // TODO: Token-Refresh-Flow
    }
    handler.next(err);
  }
}
