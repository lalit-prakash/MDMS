import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Signed-in consumer session: access token (JWT from /consumer-auth/login) plus the identity
/// fields the app needs on every screen. There is no OTP/refresh-token flow yet (see
/// ConsumerAuthController's own doc comment) — the token is simply long-lived and re-issued on
/// the next login. `mobileNumber` is kept so "Add Account" can silently re-verify a new account
/// against the same phone without asking the user to retype it.
class ConsumerSession {
  final String accessToken;
  final String consumerId;
  final String name;
  final String accountNumber;
  final String mobileNumber;
  final String organisationName;

  const ConsumerSession({
    required this.accessToken,
    required this.consumerId,
    required this.name,
    required this.accountNumber,
    required this.mobileNumber,
    this.organisationName = 'Default Organisation',
  });

  Map<String, dynamic> toJson() => {
        'accessToken': accessToken,
        'consumerId': consumerId,
        'name': name,
        'accountNumber': accountNumber,
        'mobileNumber': mobileNumber,
        'organisationName': organisationName,
      };

  factory ConsumerSession.fromJson(Map<String, dynamic> j) => ConsumerSession(
        accessToken: j['accessToken'] as String,
        consumerId: j['consumerId'] as String,
        name: j['name'] as String,
        accountNumber: j['accountNumber'] as String,
        mobileNumber: j['mobileNumber'] as String? ?? '',
        organisationName: j['organisationName'] as String? ?? 'Default Organisation',
      );
}

/// Every account the user has linked on this device (their own, plus any added via "Add
/// Account"/"Add Organisation"), and which one is active. Multiple linked accounts is what makes
/// switching organisations possible too: each account's own token already carries its own tenant
/// claim (see ConsumerAuthController.ListAccountsByMobile), so activating a different linked
/// account is the same action as switching organisation.
class SessionController extends StateNotifier<List<ConsumerSession>> {
  static const _storage = FlutterSecureStorage();
  int _activeIndex = 0;

  SessionController() : super(const []) {
    _restore();
  }

  ConsumerSession? get active => state.isEmpty ? null : state[_activeIndex.clamp(0, state.length - 1)];
  int get activeIndex => _activeIndex;

  Future<void> _restore() async {
    final raw = await _storage.read(key: 'linkedSessions');
    final activeRaw = await _storage.read(key: 'activeIndex');
    if (raw != null) {
      final list = (jsonDecode(raw) as List).map((e) => ConsumerSession.fromJson(e as Map<String, dynamic>)).toList();
      _activeIndex = int.tryParse(activeRaw ?? '0') ?? 0;
      state = list;
    }
  }

  Future<void> _persist() async {
    await _storage.write(key: 'linkedSessions', value: jsonEncode(state.map((s) => s.toJson()).toList()));
    await _storage.write(key: 'activeIndex', value: '$_activeIndex');
  }

  /// Signs in fresh — replaces every linked account (used by the Login screen).
  Future<void> signIn(ConsumerSession session) async {
    state = [session];
    _activeIndex = 0;
    await _persist();
  }

  /// Adds another account (same phone number, possibly a different organisation) alongside the
  /// existing ones, and makes it active.
  Future<void> addAccount(ConsumerSession session) async {
    if (state.any((s) => s.consumerId == session.consumerId)) {
      _activeIndex = state.indexWhere((s) => s.consumerId == session.consumerId);
    } else {
      state = [...state, session];
      _activeIndex = state.length - 1;
    }
    await _persist();
  }

  Future<void> switchTo(int index) async {
    if (index < 0 || index >= state.length) return;
    _activeIndex = index;
    state = [...state];
    await _persist();
  }

  Future<void> removeAccount(int index) async {
    if (index < 0 || index >= state.length) return;
    final next = [...state]..removeAt(index);
    state = next;
    if (_activeIndex >= state.length) _activeIndex = state.isEmpty ? 0 : state.length - 1;
    if (state.isEmpty) {
      await _storage.deleteAll();
    } else {
      await _persist();
    }
  }

  Future<void> signOut() async {
    await _storage.deleteAll();
    state = const [];
    _activeIndex = 0;
  }
}

final sessionListProvider = StateNotifierProvider<SessionController, List<ConsumerSession>>((ref) => SessionController());

/// The currently active account's session, or null when signed out — the same shape every screen
/// used before multi-account support existed.
final sessionProvider = Provider<ConsumerSession?>((ref) {
  ref.watch(sessionListProvider);
  return ref.read(sessionListProvider.notifier).active;
});
