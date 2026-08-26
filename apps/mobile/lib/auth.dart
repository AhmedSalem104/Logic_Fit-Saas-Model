import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class AuthSession {
  const AuthSession({
    required this.accessToken,
    required this.sessionId,
    required this.requiresMfa,
    required this.mfaVerified,
    required this.expiresAtUtc,
    required this.idleExpiresAtUtc,
    required this.absoluteExpiresAtUtc,
    required this.user,
    this.challenge,
  });

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
    accessToken: json['accessToken'] as String? ?? '',
    sessionId: json['sessionId'] as String? ?? '',
    requiresMfa: json['requiresMfa'] as bool? ?? false,
    mfaVerified: json['mfaVerified'] as bool? ?? false,
    expiresAtUtc:
        DateTime.tryParse(json['expiresAtUtc'] as String? ?? '') ??
        DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
    idleExpiresAtUtc:
        DateTime.tryParse(json['idleExpiresAtUtc'] as String? ?? '') ??
        DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
    absoluteExpiresAtUtc:
        DateTime.tryParse(json['absoluteExpiresAtUtc'] as String? ?? '') ??
        DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
    user: AuthUser.fromJson(
      (json['user'] as Map<String, dynamic>?) ?? const {},
    ),
    challenge: json['challenge'] as String?,
  );

  final String accessToken;
  final String sessionId;
  final bool requiresMfa;
  final bool mfaVerified;
  final DateTime expiresAtUtc;
  final DateTime idleExpiresAtUtc;
  final DateTime absoluteExpiresAtUtc;
  final AuthUser user;
  final String? challenge;
}

class AuthUser {
  const AuthUser({
    required this.userId,
    required this.email,
    required this.displayName,
    required this.status,
  });

  factory AuthUser.fromJson(Map<String, dynamic> json) => AuthUser(
    userId: json['userId'] as String? ?? '',
    email: json['email'] as String? ?? '',
    displayName: json['displayName'] as String? ?? '',
    status: json['status'] as String? ?? 'unknown',
  );

  final String userId;
  final String email;
  final String displayName;
  final String status;
}

class MobileSessionItem {
  const MobileSessionItem({
    required this.sessionId,
    required this.createdAtUtc,
    required this.lastSeenAtUtc,
    required this.expiresAtUtc,
    required this.mfaVerified,
    required this.isCurrent,
  });

  factory MobileSessionItem.fromJson(Map<String, dynamic> json) =>
      MobileSessionItem(
        sessionId: json['sessionId'] as String? ?? '',
        createdAtUtc:
            DateTime.tryParse(json['createdAtUtc'] as String? ?? '') ??
            DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
        lastSeenAtUtc:
            DateTime.tryParse(json['lastSeenAtUtc'] as String? ?? '') ??
            DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
        expiresAtUtc:
            DateTime.tryParse(json['expiresAtUtc'] as String? ?? '') ??
            DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
        mfaVerified: json['mfaVerified'] as bool? ?? false,
        isCurrent: json['isCurrent'] as bool? ?? false,
      );

  final String sessionId;
  final DateTime createdAtUtc;
  final DateTime lastSeenAtUtc;
  final DateTime expiresAtUtc;
  final bool mfaVerified;
  final bool isCurrent;
}

class MfaEnrollment {
  const MfaEnrollment({
    required this.factorId,
    required this.status,
    required this.secret,
    required this.provisioningUri,
  });

  factory MfaEnrollment.fromJson(Map<String, dynamic> json) => MfaEnrollment(
    factorId: json['factorId'] as String? ?? '',
    status: json['status'] as String? ?? 'pending',
    secret: json['secret'] as String? ?? '',
    provisioningUri: json['provisioningUri'] as String? ?? '',
  );

  final String factorId;
  final String status;
  final String secret;
  final String provisioningUri;
}

class AuthState {
  const AuthState({this.session, this.isLoading = false, this.errorMessage});

  final AuthSession? session;
  final bool isLoading;
  final String? errorMessage;

  AuthState copyWith({
    AuthSession? session,
    bool? isLoading,
    String? errorMessage,
    bool clearSession = false,
    bool clearError = false,
  }) => AuthState(
    session: clearSession ? null : session ?? this.session,
    isLoading: isLoading ?? this.isLoading,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}

class AuthController extends StateNotifier<AuthState> {
  AuthController(this._dio) : super(const AuthState());

  final Dio _dio;

  Future<bool> login(String email, String password) async {
    state = state.copyWith(isLoading: true, clearError: true);
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/auth/login',
        data: {'email': email, 'password': password},
      );
      final data = response.data?['data'] as Map<String, dynamic>? ?? const {};
      final session = AuthSession.fromJson(data);
      state = AuthState(session: session);
      return true;
    } on DioException catch (error) {
      state = AuthState(errorMessage: _safeError(error));
      return false;
    }
  }

  Future<void> logout() async {
    final current = state.session;
    if (current == null) return;
    try {
      await _dio.post<void>(
        '/auth/logout',
        data: {'sessionId': current.sessionId},
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
    } on DioException {
      // Local session state is still cleared when the server is unavailable.
    } finally {
      state = const AuthState();
    }
  }

  Future<bool> verifyMfa(String challenge, String method, String code) async {
    final current = state.session;
    if (current == null) return false;
    state = state.copyWith(isLoading: true, clearError: true);
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/auth/mfa/verify',
        data: {'challenge': challenge, 'method': method, 'code': code},
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
      final data = response.data?['data'] as Map<String, dynamic>? ?? const {};
      final verifiedSession = data['session'];
      if (verifiedSession is Map<String, dynamic>) {
        state = AuthState(session: AuthSession.fromJson(verifiedSession));
      } else {
        state = AuthState(
          session: AuthSession(
            accessToken: current.accessToken,
            sessionId: current.sessionId,
            requiresMfa: false,
            mfaVerified: true,
            expiresAtUtc: current.expiresAtUtc,
            idleExpiresAtUtc: current.idleExpiresAtUtc,
            absoluteExpiresAtUtc: current.absoluteExpiresAtUtc,
            user: current.user,
          ),
        );
      }
      return true;
    } on DioException catch (error) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: _safeError(error),
        clearError: false,
      );
      return false;
    }
  }

  Future<List<MobileSessionItem>?> listSessions() async {
    final current = state.session;
    if (current == null) return null;
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/auth/sessions',
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
      final data = response.data?['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map<String, dynamic>>()
          .map(MobileSessionItem.fromJson)
          .toList(growable: false);
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return null;
    }
  }

  Future<bool> revokeSession(String sessionId) async {
    final current = state.session;
    if (current == null) return false;
    try {
      await _dio.post<void>(
        '/auth/sessions/$sessionId/revoke',
        data: {'reason': 'self-service-mobile-security'},
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
      if (sessionId == current.sessionId) state = const AuthState();
      return true;
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return false;
    }
  }

  Future<bool> changePassword(
    String currentPassword,
    String newPassword,
  ) async {
    final current = state.session;
    if (current == null) return false;
    try {
      await _dio.post<void>(
        '/auth/password/change',
        data: {'currentPassword': currentPassword, 'newPassword': newPassword},
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
      state = const AuthState();
      return true;
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return false;
    }
  }

  Future<MfaEnrollment?> enrollMfa() async {
    final current = state.session;
    if (current == null) return null;
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/auth/mfa/enroll',
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
      return MfaEnrollment.fromJson(
        (response.data?['data'] as Map<String, dynamic>?) ?? const {},
      );
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return null;
    }
  }

  Future<bool> disableMfa(String currentPassword) async {
    final current = state.session;
    if (current == null) return false;
    try {
      await _dio.post<void>(
        '/auth/mfa/disable',
        data: {'currentPassword': currentPassword, 'code': null},
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
      state = const AuthState();
      return true;
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return false;
    }
  }

  Future<List<String>?> regenerateRecoveryCodes(String currentPassword) async {
    final current = state.session;
    if (current == null) return null;
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/auth/mfa/recovery-codes/regenerate',
        data: {'currentPassword': currentPassword, 'code': null},
        options: Options(
          headers: {'Authorization': 'Bearer ${current.accessToken}'},
        ),
      );
      final data = response.data?['data'] as Map<String, dynamic>? ?? const {};
      return (data['codes'] as List? ?? const []).whereType<String>().toList(
        growable: false,
      );
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return null;
    }
  }

  Future<bool> requestPasswordReset(String email) async {
    try {
      await _dio.post<void>(
        '/auth/password-reset/request',
        data: {'email': email},
      );
      return true;
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return false;
    }
  }

  Future<bool> completePasswordReset(String token, String newPassword) async {
    try {
      await _dio.post<void>(
        '/auth/password-reset/complete',
        data: {'token': token, 'newPassword': newPassword},
      );
      return true;
    } on DioException catch (error) {
      state = state.copyWith(errorMessage: _safeError(error));
      return false;
    }
  }

  static String _safeError(DioException error) {
    final body = error.response?.data;
    if (body is Map<String, dynamic>) {
      final apiError = body['error'];
      if (apiError is Map<String, dynamic> && apiError['message'] is String) {
        return apiError['message'] as String;
      }
    }
    return 'تعذر إكمال العملية. حاول مرة أخرى.';
  }
}
