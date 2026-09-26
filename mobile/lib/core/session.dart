import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Signed-in consumer session: access token (JWT from /consumer-auth/login) plus the identity
/// fields the app needs on every screen, persisted in Android Keystore-backed secure storage so a
/// restart doesn't force re-login. There is no OTP/refresh-token flow yet (see
/// ConsumerAuthController's own doc comment) — the token is simply long-lived and re-issued on
/// the next login.
class ConsumerSession {
  final String accessToken;
  final String consumerId;
  final String name;
  final String accountNumber;

  const ConsumerSession({
    required this.accessToken,
    required this.consumerId,
    required this.name,
    required this.accountNumber,
  });
}

class SessionController extends StateNotifier<ConsumerSession?> {
  static const _storage = FlutterSecureStorage();

  SessionController() : super(null) {
    _restore();
  }

  Future<void> _restore() async {
    final token = await _storage.read(key: 'accessToken');
    final consumerId = await _storage.read(key: 'consumerId');
    final name = await _storage.read(key: 'name');
    final accountNumber = await _storage.read(key: 'accountNumber');
    if (token != null && consumerId != null && name != null && accountNumber != null) {
      state = ConsumerSession(accessToken: token, consumerId: consumerId, name: name, accountNumber: accountNumber);
    }
  }

  Future<void> signIn(ConsumerSession session) async {
    await _storage.write(key: 'accessToken', value: session.accessToken);
    await _storage.write(key: 'consumerId', value: session.consumerId);
    await _storage.write(key: 'name', value: session.name);
    await _storage.write(key: 'accountNumber', value: session.accountNumber);
    state = session;
  }

  Future<void> signOut() async {
    await _storage.deleteAll();
    state = null;
  }
}

final sessionProvider = StateNotifierProvider<SessionController, ConsumerSession?>((ref) => SessionController());
