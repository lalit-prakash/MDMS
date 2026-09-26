import 'package:dio/dio.dart';

/// Base URL for the MDMS backend. `localhost` works for both the Android emulator (which the
/// Flutter tool maps to the host loopback for a physical USB device once `adb reverse tcp:5004
/// tcp:5004` is set up) and any environment where the backend is reachable at this address.
/// Point this at your real backend host for a non-USB deployment.
const String kApiBaseUrl = 'http://localhost:5004';

/// Thin Dio wrapper: base URL, JSON, and (once signed in) the Bearer token on every request.
/// No refresh-token flow exists yet for the consumer app (see ConsumerAuthController) — a 401
/// just routes back to Login.
class ApiClient {
  final Dio dio;
  String? _accessToken;

  ApiClient()
      : dio = Dio(BaseOptions(
          baseUrl: kApiBaseUrl,
          connectTimeout: const Duration(seconds: 15),
          receiveTimeout: const Duration(seconds: 20),
          contentType: 'application/json',
        ));

  void setToken(String? token) {
    _accessToken = token;
    if (token != null) {
      dio.options.headers['Authorization'] = 'Bearer $token';
    } else {
      dio.options.headers.remove('Authorization');
    }
  }

  bool get isAuthenticated => _accessToken != null;
}

/// Raised for any non-2xx API response, carrying a message safe to show a consumer (never a raw
/// backend exception) — per the spec's error-handling requirement.
class ApiException implements Exception {
  final int? statusCode;
  final String message;
  ApiException(this.statusCode, this.message);

  factory ApiException.from(DioException e) {
    final code = e.response?.statusCode;
    final body = e.response?.data;
    String friendly;
    switch (code) {
      case 401:
        friendly = 'Your session has expired. Please login again.';
        break;
      case 404:
        friendly = 'Requested information was not found.';
        break;
      case 409:
        friendly = 'This action conflicts with an existing record.';
        break;
      case 422:
        friendly = body is String ? body : 'This request could not be processed.';
        break;
      case null:
        friendly = 'No internet connection. Please check your network and try again.';
        break;
      default:
        friendly = 'Unable to complete this request. Please try again.';
    }
    return ApiException(code, friendly);
  }

  @override
  String toString() => message;
}
