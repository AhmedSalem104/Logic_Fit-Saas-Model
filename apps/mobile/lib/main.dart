import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'auth.dart';

const _defaultApiBaseUrl = String.fromEnvironment(
  'LOGICFIT_API_BASE_URL',
  defaultValue: 'http://127.0.0.1:5199/api/v1',
);

final apiClientProvider = Provider<ApiClient>((ref) {
  final dio = Dio(
    BaseOptions(
      baseUrl: _defaultApiBaseUrl,
      connectTimeout: const Duration(seconds: 3),
      receiveTimeout: const Duration(seconds: 5),
      headers: {'Accept': 'application/json'},
    ),
  );
  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) {
        options.headers['X-Request-Id'] =
            'mobile-${DateTime.now().microsecondsSinceEpoch}';
        handler.next(options);
      },
    ),
  );
  ref.onDispose(dio.close);
  return ApiClient(dio);
});

final healthProvider = FutureProvider<FoundationHealth>((ref) async {
  return ref.read(apiClientProvider).health();
});

final themeModeProvider = StateProvider<ThemeMode>((ref) => ThemeMode.light);

final authControllerProvider = StateNotifierProvider<AuthController, AuthState>(
  (ref) => AuthController(ref.read(apiClientProvider).dio),
);

class ApiClient {
  ApiClient(this._dio);
  final Dio _dio;

  Dio get dio => _dio;

  Future<FoundationHealth> health() async {
    final response = await _dio.get<Map<String, dynamic>>('/health');
    final data = response.data?['data'] as Map<String, dynamic>? ?? const {};
    return FoundationHealth(
      status: data['status'] as String? ?? 'unknown',
      environment: data['environment'] as String? ?? 'unknown',
      version: data['version'] as String? ?? 'unknown',
    );
  }
}

class FoundationHealth {
  const FoundationHealth({
    required this.status,
    required this.environment,
    required this.version,
  });
  final String status;
  final String environment;
  final String version;
}

final _routerProvider = Provider<GoRouter>((ref) {
  final auth = ref.watch(authControllerProvider).session;
  return GoRouter(
    initialLocation: '/',
    redirect: (_, state) {
      final protected =
          state.matchedLocation == '/app' ||
          state.matchedLocation == '/app/security';
      if (protected && (auth == null || auth.requiresMfa)) return '/login';
      if (state.matchedLocation == '/login' &&
          auth != null &&
          !auth.requiresMfa) {
        return '/app';
      }
      return null;
    },
    routes: [
      GoRoute(path: '/', builder: (context, state) => const FoundationHome()),
      GoRoute(
        path: '/diagnostics',
        builder: (context, state) => const DiagnosticsScreen(),
      ),
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      GoRoute(
        path: '/password-reset',
        builder: (context, state) => const PasswordResetScreen(),
      ),
      GoRoute(
        path: '/app',
        builder: (context, state) => const AuthenticatedScreen(),
      ),
      GoRoute(
        path: '/app/security',
        builder: (context, state) => const SecurityMobileScreen(),
      ),
    ],
  );
});

void main() {
  runApp(const ProviderScope(child: LogicFitApp()));
}

class LogicFitApp extends ConsumerWidget {
  const LogicFitApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp.router(
      title: 'LogicFit',
      debugShowCheckedModeBanner: false,
      themeMode: ref.watch(themeModeProvider),
      theme: _buildTheme(Brightness.light),
      darkTheme: _buildTheme(Brightness.dark),
      routerConfig: ref.watch(_routerProvider),
      locale: const Locale('ar'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
    );
  }
}

ThemeData _buildTheme(Brightness brightness) {
  final scheme = ColorScheme.fromSeed(
    seedColor: const Color(0xff2563eb),
    brightness: brightness,
  );
  return ThemeData(
    useMaterial3: true,
    brightness: brightness,
    colorScheme: scheme,
    scaffoldBackgroundColor: brightness == Brightness.dark
        ? const Color(0xff111827)
        : const Color(0xfff4f7fb),
    cardTheme: const CardThemeData(margin: EdgeInsets.zero),
    inputDecorationTheme: const InputDecorationTheme(
      border: OutlineInputBorder(),
    ),
  );
}

class FoundationHome extends ConsumerWidget {
  const FoundationHome({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final health = ref.watch(healthProvider);
    return AppShell(
      title: 'منصة LogicFit',
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'الأساس التقني المحلي',
            style: Theme.of(
              context,
            ).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          Text(
            'Foundation فقط — لا توجد Business Modules في هذه المرحلة.',
            style: Theme.of(context).textTheme.bodyLarge,
          ),
          const SizedBox(height: 20),
          LayoutBuilder(
            builder: (context, constraints) {
              final columns = constraints.maxWidth > 720 ? 3 : 1;
              return GridView.count(
                crossAxisCount: columns,
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                mainAxisSpacing: 12,
                crossAxisSpacing: 12,
                childAspectRatio: 1.7,
                children: [
                  FoundationCard(
                    title: 'API Health',
                    child: health.when(
                      data: (value) => Metric(
                        value: value.status,
                        caption: '${value.environment} · ${value.version}',
                      ),
                      loading: () => const LoadingState(),
                      error: (error, _) =>
                          const ErrorState(message: 'API المحلي غير متاح.'),
                    ),
                  ),
                  const FoundationCard(
                    title: 'Routing',
                    child: Metric(value: 'GoRouter', caption: 'جاهز'),
                  ),
                  const FoundationCard(
                    title: 'State',
                    child: Metric(value: 'Riverpod', caption: 'جاهز'),
                  ),
                ],
              );
            },
          ),
          const SizedBox(height: 20),
          const FoundationCard(
            title: 'النطاق',
            child: EmptyState(
              title: 'لا توجد وحدات أعمال بعد',
              message: 'يبدأ التنفيذ العمودي بعد إغلاق Foundation.',
            ),
          ),
        ],
      ),
    );
  }
}

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _mfaCodeController = TextEditingController();
  String? _validationError;
  String _mfaMethod = 'totp';

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _mfaCodeController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final email = _emailController.text.trim();
    final password = _passwordController.text;
    if (!email.contains('@') || password.isEmpty) {
      setState(
        () => _validationError = 'أدخل بريدًا إلكترونيًا صحيحًا وكلمة مرور.',
      );
      return;
    }

    setState(() => _validationError = null);
    final success = await ref
        .read(authControllerProvider.notifier)
        .login(email, password);
    if (!mounted || !success) return;
    final session = ref.read(authControllerProvider).session;
    if (session?.requiresMfa == true) {
      setState(() => _validationError = null);
      return;
    }
    if (session != null) context.go('/app');
  }

  Future<void> _verifyMfa() async {
    final session = ref.read(authControllerProvider).session;
    final code = _mfaCodeController.text.trim();
    if (session?.challenge == null || code.isEmpty) {
      setState(() => _validationError = 'أدخل رمز التحقق.');
      return;
    }
    setState(() => _validationError = null);
    final success = await ref
        .read(authControllerProvider.notifier)
        .verifyMfa(session!.challenge!, _mfaMethod, code);
    if (mounted && success) context.go('/app');
  }

  @override
  Widget build(BuildContext context) {
    final auth = ref.watch(authControllerProvider);
    final pending = auth.session?.requiresMfa == true;
    return AppShell(
      title: 'تسجيل الدخول',
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 460),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: AutofillGroup(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      pending ? 'تحقق MFA' : 'تسجيل الدخول الآمن',
                      style: Theme.of(context).textTheme.headlineSmall
                          ?.copyWith(fontWeight: FontWeight.w800),
                    ),
                    const SizedBox(height: 20),
                    if (pending) ...[
                      const Text(
                        'أدخل الرمز من تطبيق Authenticator أو استخدم رمز استرداد لمرة واحدة.',
                      ),
                      const SizedBox(height: 12),
                      DropdownButtonFormField<String>(
                        initialValue: _mfaMethod,
                        decoration: const InputDecoration(
                          labelText: 'طريقة التحقق',
                        ),
                        items: const [
                          DropdownMenuItem(
                            value: 'totp',
                            child: Text('رمز Authenticator'),
                          ),
                          DropdownMenuItem(
                            value: 'recovery_code',
                            child: Text('رمز استرداد'),
                          ),
                        ],
                        onChanged: (value) =>
                            setState(() => _mfaMethod = value ?? 'totp'),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: _mfaCodeController,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(labelText: 'الرمز'),
                      ),
                      const SizedBox(height: 20),
                      FilledButton(
                        onPressed: auth.isLoading ? null : _verifyMfa,
                        child: auth.isLoading
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Text('تحقق'),
                      ),
                    ] else ...[
                      TextField(
                        controller: _emailController,
                        keyboardType: TextInputType.emailAddress,
                        autofillHints: const [AutofillHints.username],
                        decoration: const InputDecoration(
                          labelText: 'البريد الإلكتروني',
                        ),
                      ),
                      const SizedBox(height: 12),
                      TextField(
                        controller: _passwordController,
                        obscureText: true,
                        autofillHints: const [AutofillHints.password],
                        decoration: const InputDecoration(
                          labelText: 'كلمة المرور',
                        ),
                        onSubmitted: (_) => _submit(),
                      ),
                      const SizedBox(height: 20),
                      FilledButton(
                        onPressed: auth.isLoading ? null : _submit,
                        child: auth.isLoading
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                ),
                              )
                            : const Text('دخول'),
                      ),
                      TextButton(
                        onPressed: () => context.go('/password-reset'),
                        child: const Text('نسيت كلمة المرور؟'),
                      ),
                    ],
                    if (_validationError != null) ...[
                      const SizedBox(height: 12),
                      ErrorState(message: _validationError!),
                    ],
                    if (auth.errorMessage != null) ...[
                      const SizedBox(height: 12),
                      ErrorState(message: auth.errorMessage!),
                    ],
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class PasswordResetScreen extends ConsumerStatefulWidget {
  const PasswordResetScreen({super.key});

  @override
  ConsumerState<PasswordResetScreen> createState() =>
      _PasswordResetScreenState();
}

class _PasswordResetScreenState extends ConsumerState<PasswordResetScreen> {
  final _email = TextEditingController();
  final _token = TextEditingController();
  final _password = TextEditingController();
  final _confirmation = TextEditingController();
  String? _error;
  String? _success;

  @override
  void dispose() {
    _email.dispose();
    _token.dispose();
    _password.dispose();
    _confirmation.dispose();
    super.dispose();
  }

  Future<void> _request() async {
    final email = _email.text.trim();
    if (!email.contains('@')) {
      setState(() => _error = 'أدخل بريدًا إلكترونيًا صحيحًا.');
      return;
    }
    setState(() {
      _error = null;
      _success = null;
    });
    final ok = await ref
        .read(authControllerProvider.notifier)
        .requestPasswordReset(email);
    if (mounted && ok) {
      setState(
        () => _success =
            'إذا كان الحساب موجودًا، ستصل تعليمات الاسترداد عبر القناة المحلية المعتمدة.',
      );
    }
  }

  Future<void> _complete() async {
    if (_token.text.trim().isEmpty ||
        _password.text.length < 12 ||
        _password.text != _confirmation.text) {
      setState(() => _error = 'تحقق من الرمز وكلمة المرور وتأكيدها.');
      return;
    }
    setState(() {
      _error = null;
      _success = null;
    });
    final ok = await ref
        .read(authControllerProvider.notifier)
        .completePasswordReset(_token.text.trim(), _password.text);
    if (!mounted) return;
    if (ok) {
      setState(() => _success = 'تم تغيير كلمة المرور.');
      context.go('/login');
    }
  }

  @override
  Widget build(BuildContext context) => AppShell(
    title: 'استرداد الحساب',
    child: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 560),
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'استرداد الحساب',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: _email,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(
                      labelText: 'البريد الإلكتروني',
                    ),
                  ),
                  const SizedBox(height: 12),
                  FilledButton(
                    onPressed: _request,
                    child: const Text('طلب تعليمات الاسترداد'),
                  ),
                  const Divider(height: 32),
                  TextField(
                    controller: _token,
                    decoration: const InputDecoration(
                      labelText: 'رمز الاسترداد',
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _password,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: 'كلمة المرور الجديدة',
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _confirmation,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: 'تأكيد كلمة المرور',
                    ),
                  ),
                  const SizedBox(height: 12),
                  FilledButton.tonal(
                    onPressed: _complete,
                    child: const Text('إكمال تغيير كلمة المرور'),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    ErrorState(message: _error!),
                  ],
                  if (_success != null) ...[
                    const SizedBox(height: 12),
                    Text(
                      _success!,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.primary,
                      ),
                    ),
                  ],
                  TextButton(
                    onPressed: () => context.go('/login'),
                    child: const Text('العودة لتسجيل الدخول'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ],
    ),
  );
}

class AuthenticatedScreen extends ConsumerWidget {
  const AuthenticatedScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).session;
    if (session == null) return const LoginScreen();
    return AppShell(
      title: 'مساحة الحساب',
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Text(
            'مرحبًا ${session.user.displayName}',
            style: Theme.of(
              context,
            ).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 8),
          Text(session.user.email),
          const SizedBox(height: 20),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Text(
                'تمت المصادقة عبر ASP.NET Core API. حالة MFA: ${session.mfaVerified ? 'متحقق' : 'مطلوب'}',
              ),
            ),
          ),
          const SizedBox(height: 12),
          FilledButton.tonal(
            onPressed: () => context.go('/app/security'),
            child: const Text('أمان الحساب والجلسات'),
          ),
          const SizedBox(height: 12),
          FilledButton.tonal(
            onPressed: () async {
              await ref.read(authControllerProvider.notifier).logout();
              if (context.mounted) context.go('/login');
            },
            child: const Text('تسجيل الخروج'),
          ),
        ],
      ),
    );
  }
}

class SecurityMobileScreen extends ConsumerStatefulWidget {
  const SecurityMobileScreen({super.key});

  @override
  ConsumerState<SecurityMobileScreen> createState() =>
      _SecurityMobileScreenState();
}

class _SecurityMobileScreenState extends ConsumerState<SecurityMobileScreen> {
  late Future<List<MobileSessionItem>?> _sessionsFuture;
  final _currentPassword = TextEditingController();
  final _newPassword = TextEditingController();
  final _confirmation = TextEditingController();
  final _mfaCode = TextEditingController();
  final _recoveryPassword = TextEditingController();
  final _disablePassword = TextEditingController();
  MfaEnrollment? _enrollment;
  List<String>? _recoveryCodes;
  String? _error;
  String? _message;

  @override
  void initState() {
    super.initState();
    _sessionsFuture = ref.read(authControllerProvider.notifier).listSessions();
  }

  @override
  void dispose() {
    _currentPassword.dispose();
    _newPassword.dispose();
    _confirmation.dispose();
    _mfaCode.dispose();
    _recoveryPassword.dispose();
    _disablePassword.dispose();
    super.dispose();
  }

  void _reload() => setState(() {
    _sessionsFuture = ref.read(authControllerProvider.notifier).listSessions();
    _error = null;
  });

  Future<void> _changePassword() async {
    if (_newPassword.text.length < 12 ||
        _newPassword.text != _confirmation.text) {
      setState(
        () => _error =
            'كلمة المرور الجديدة يجب أن تكون 12 حرفًا على الأقل ومتطابقة.',
      );
      return;
    }
    final ok = await ref
        .read(authControllerProvider.notifier)
        .changePassword(_currentPassword.text, _newPassword.text);
    if (!mounted) return;
    if (ok) {
      context.go('/login');
    } else {
      setState(() => _error = ref.read(authControllerProvider).errorMessage);
    }
  }

  Future<void> _startMfa() async {
    final enrollment = await ref
        .read(authControllerProvider.notifier)
        .enrollMfa();
    if (mounted) {
      setState(() {
        _enrollment = enrollment;
        _error = enrollment == null
            ? ref.read(authControllerProvider).errorMessage
            : null;
      });
    }
  }

  Future<void> _verifyEnrollment() async {
    final enrollment = _enrollment;
    final session = ref.read(authControllerProvider).session;
    if (enrollment == null || session == null) return;
    final ok = await ref
        .read(authControllerProvider.notifier)
        .verifyMfa(enrollment.factorId, 'totp', _mfaCode.text.trim());
    if (mounted) {
      setState(() {
        _enrollment = ok ? null : enrollment;
        _message = ok ? 'تم تفعيل MFA.' : null;
        _error = ok ? null : ref.read(authControllerProvider).errorMessage;
      });
    }
  }

  Future<void> _regenerateCodes() async {
    final codes = await ref
        .read(authControllerProvider.notifier)
        .regenerateRecoveryCodes(_recoveryPassword.text);
    if (mounted) {
      setState(() {
        _recoveryCodes = codes;
        _error = codes == null
            ? ref.read(authControllerProvider).errorMessage
            : null;
      });
    }
  }

  Future<void> _disableMfa() async {
    final ok = await ref
        .read(authControllerProvider.notifier)
        .disableMfa(_disablePassword.text);
    if (!mounted) return;
    if (ok) {
      context.go('/login');
    } else {
      setState(() => _error = ref.read(authControllerProvider).errorMessage);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(authControllerProvider).session;
    if (session == null) return const LoginScreen();
    return AppShell(
      title: 'أمان الحساب',
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'أمان الحساب',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              IconButton(
                onPressed: () => context.go('/app'),
                icon: const Icon(Icons.arrow_back),
              ),
            ],
          ),
          if (_error != null) ErrorState(message: _error!),
          if (_message != null)
            Text(
              _message!,
              style: TextStyle(color: Theme.of(context).colorScheme.primary),
            ),
          const SizedBox(height: 12),
          FoundationCard(
            title: 'الجلسات النشطة',
            child: FutureBuilder<List<MobileSessionItem>?>(
              future: _sessionsFuture,
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const LoadingState();
                }
                if (snapshot.hasError || snapshot.data == null) {
                  return const ErrorState(message: 'تعذر تحميل الجلسات.');
                }
                final items = snapshot.data!;
                if (items.isEmpty) {
                  return const EmptyState(
                    title: 'لا توجد جلسات',
                    message: 'ستظهر الجلسات النشطة هنا.',
                  );
                }
                return Column(
                  children: items
                      .map(
                        (item) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(
                            '${item.isCurrent ? 'هذه الجلسة' : 'جلسة'} · ${item.sessionId.substring(0, 8)}…',
                          ),
                          subtitle: Text(
                            'تنتهي ${item.expiresAtUtc.toLocal()}',
                          ),
                          trailing: TextButton(
                            onPressed: () async {
                              await ref
                                  .read(authControllerProvider.notifier)
                                  .revokeSession(item.sessionId);
                              if (context.mounted && item.isCurrent) {
                                context.go('/login');
                              } else if (context.mounted) {
                                _reload();
                              }
                            },
                            child: const Text('إلغاء'),
                          ),
                        ),
                      )
                      .toList(),
                );
              },
            ),
          ),
          const SizedBox(height: 12),
          FoundationCard(
            title: 'تغيير كلمة المرور',
            child: Column(
              children: [
                TextField(
                  controller: _currentPassword,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'كلمة المرور الحالية',
                  ),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: _newPassword,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'كلمة المرور الجديدة',
                  ),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: _confirmation,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'تأكيد كلمة المرور',
                  ),
                ),
                const SizedBox(height: 10),
                FilledButton(
                  onPressed: _changePassword,
                  child: const Text('تغيير كلمة المرور'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          FoundationCard(
            title: 'تطبيق المصادقة MFA',
            child: _enrollment == null
                ? Column(
                    children: [
                      const Text('استخدم Authenticator لحماية تسجيل الدخول.'),
                      const SizedBox(height: 10),
                      FilledButton(
                        onPressed: _startMfa,
                        child: const Text('بدء التسجيل'),
                      ),
                      const SizedBox(height: 10),
                      TextField(
                        controller: _disablePassword,
                        obscureText: true,
                        decoration: const InputDecoration(
                          labelText: 'كلمة المرور لتعطيل MFA',
                        ),
                      ),
                      const SizedBox(height: 10),
                      FilledButton.tonal(
                        onPressed: _disableMfa,
                        child: const Text('تعطيل MFA'),
                      ),
                    ],
                  )
                : Column(
                    children: [
                      const Text(
                        'أضف URI إلى تطبيق Authenticator ثم أدخل الرمز. يظهر سر الإعداد أثناء التسجيل فقط.',
                      ),
                      const SizedBox(height: 8),
                      SelectableText(
                        _enrollment!.provisioningUri,
                        textDirection: TextDirection.ltr,
                      ),
                      const SizedBox(height: 10),
                      TextField(
                        controller: _mfaCode,
                        keyboardType: TextInputType.number,
                        decoration: const InputDecoration(
                          labelText: 'رمز التطبيق',
                        ),
                      ),
                      const SizedBox(height: 10),
                      FilledButton(
                        onPressed: _verifyEnrollment,
                        child: const Text('تأكيد التفعيل'),
                      ),
                    ],
                  ),
          ),
          const SizedBox(height: 12),
          FoundationCard(
            title: 'رموز الاسترداد',
            child: Column(
              children: [
                TextField(
                  controller: _recoveryPassword,
                  obscureText: true,
                  decoration: const InputDecoration(
                    labelText: 'كلمة المرور الحالية',
                  ),
                ),
                const SizedBox(height: 10),
                FilledButton.tonal(
                  onPressed: _regenerateCodes,
                  child: const Text('إنشاء رموز جديدة'),
                ),
                if (_recoveryCodes != null) ...[
                  const SizedBox(height: 10),
                  SelectableText(
                    _recoveryCodes!.join('\n'),
                    textDirection: TextDirection.ltr,
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class DiagnosticsScreen extends StatelessWidget {
  const DiagnosticsScreen({super.key});

  @override
  Widget build(BuildContext context) => const AppShell(
    title: 'التشخيص',
    child: EmptyState(
      title: 'Diagnostics foundation',
      message: 'لا يتم عرض أسرار أو بيانات حساسة هنا.',
    ),
  );
}

class AppShell extends ConsumerWidget {
  const AppShell({required this.title, required this.child, super.key});
  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final themeMode = ref.watch(themeModeProvider);
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        appBar: AppBar(
          title: Text(title),
          actions: [
            IconButton(
              tooltip: 'تبديل المظهر',
              onPressed: () => ref.read(themeModeProvider.notifier).state =
                  themeMode == ThemeMode.dark
                  ? ThemeMode.light
                  : ThemeMode.dark,
              icon: const Icon(Icons.brightness_6_outlined),
            ),
          ],
        ),
        drawer: Drawer(
          child: SafeArea(
            child: ListView(
              children: [
                const DrawerHeader(
                  child: Text(
                    'LogicFit',
                    style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                  ),
                ),
                ListTile(
                  title: const Text('الأساس التقني'),
                  leading: const Icon(Icons.foundation_outlined),
                  onTap: () => context.go('/'),
                ),
                ListTile(
                  title: const Text('تسجيل الدخول'),
                  leading: const Icon(Icons.login_outlined),
                  onTap: () => context.go('/login'),
                ),
                ListTile(
                  title: const Text('التشخيص'),
                  leading: const Icon(Icons.health_and_safety_outlined),
                  onTap: () => context.go('/diagnostics'),
                ),
              ],
            ),
          ),
        ),
        body: child,
      ),
    );
  }
}

class FoundationCard extends StatelessWidget {
  const FoundationCard({required this.title, required this.child, super.key});
  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title, style: Theme.of(context).textTheme.titleSmall),
          const SizedBox(height: 12),
          child,
        ],
      ),
    ),
  );
}

class Metric extends StatelessWidget {
  const Metric({required this.value, required this.caption, super.key});
  final String value;
  final String caption;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        value,
        style: Theme.of(
          context,
        ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800),
      ),
      Text(caption),
    ],
  );
}

class LoadingState extends StatelessWidget {
  const LoadingState({super.key});

  @override
  Widget build(BuildContext context) => const Row(
    children: [
      SizedBox(
        width: 18,
        height: 18,
        child: CircularProgressIndicator(strokeWidth: 2),
      ),
      SizedBox(width: 8),
      Text('جارٍ التحميل…'),
    ],
  );
}

class ErrorState extends StatelessWidget {
  const ErrorState({required this.message, super.key});
  final String message;

  @override
  Widget build(BuildContext context) => Text(
    message,
    style: TextStyle(color: Theme.of(context).colorScheme.error),
  );
}

class EmptyState extends StatelessWidget {
  const EmptyState({required this.title, required this.message, super.key});
  final String title;
  final String message;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
      const SizedBox(height: 6),
      Text(message),
    ],
  );
}
