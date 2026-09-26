import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';
import 'session.dart';

/// Single ApiClient instance for the app; its Authorization header is kept in sync with the
/// current session by [SessionSyncProvider] (wired in main.dart).
final apiClientProvider = Provider<ApiClient>((ref) {
  final client = ApiClient();
  ref.listen<ConsumerSession?>(sessionProvider, (previous, next) {
    client.setToken(next?.accessToken);
  }, fireImmediately: true);
  return client;
});

/// Wraps a Dio call so every screen gets the same ApiException-on-failure behavior.
Future<T> apiCall<T>(Future<T> Function() call) async {
  try {
    return await call();
  } on DioException catch (e) {
    throw ApiException.from(e);
  }
}
