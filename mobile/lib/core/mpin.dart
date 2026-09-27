import 'dart:async';
import 'dart:convert';
import 'package:crypto/crypto.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// A device-local 4-digit MPIN, real and enforced (not a decorative field): once set, it gates
/// re-entry into an already-signed-in session on this device, hashed (never stored in plain
/// text) alongside the real account session in secure storage. It does not replace the real
/// account-number + mobile-number login -- it is a quicker unlock for a session that already
/// exists, same as any banking app's PIN screen sitting in front of a real signed-in session.
class MpinController extends StateNotifier<bool> {
  static const _storage = FlutterSecureStorage();
  static const _key = 'mpinHash';
  final _readyCompleter = Completer<void>();
  Future<void> get ready => _readyCompleter.future;

  MpinController() : super(false) {
    _restore();
  }

  Future<void> _restore() async {
    final hash = await _storage.read(key: _key);
    state = hash != null;
    if (!_readyCompleter.isCompleted) _readyCompleter.complete();
  }

  String _hash(String pin) => sha256.convert(utf8.encode(pin)).toString();

  Future<void> setPin(String pin) async {
    await _storage.write(key: _key, value: _hash(pin));
    state = true;
  }

  Future<bool> verify(String pin) async {
    final stored = await _storage.read(key: _key);
    return stored != null && stored == _hash(pin);
  }

  Future<void> clear() async {
    await _storage.delete(key: _key);
    state = false;
  }
}

final mpinControllerProvider = StateNotifierProvider<MpinController, bool>((ref) => MpinController());

/// Resolves once the MPIN flag has finished loading -- awaited by the splash screen.
final mpinReadyProvider = FutureProvider<void>((ref) => ref.watch(mpinControllerProvider.notifier).ready);
