import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

const _defaultApiBaseUrl = String.fromEnvironment(
  'LOGICFIT_API_BASE_URL',
  defaultValue: 'http://127.0.0.1:4100/api/v1',
);

final apiClientProvider = Provider<ApiClient>((ref) {
  final dio = Dio(BaseOptions(
    baseUrl: _defaultApiBaseUrl,
    connectTimeout: const Duration(seconds: 3),
    receiveTimeout: const Duration(seconds: 5),
    headers: {'Accept': 'application/json'},
  ));
  dio.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) {
      options.headers['X-Request-Id'] = 'mobile-${DateTime.now().microsecondsSinceEpoch}';
      handler.next(options);
    },
  ));
  ref.onDispose(dio.close);
  return ApiClient(dio);
});

final healthProvider = FutureProvider<FoundationHealth>((ref) async {
  return ref.read(apiClientProvider).health();
});

final themeModeProvider = StateProvider<ThemeMode>((ref) => ThemeMode.light);

class ApiClient {
  ApiClient(this._dio);
  final Dio _dio;

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
  const FoundationHealth({required this.status, required this.environment, required this.version});
  final String status;
  final String environment;
  final String version;
}

final _routerProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/',
    routes: [
      GoRoute(path: '/', builder: (context, state) => const FoundationHome()),
      GoRoute(path: '/diagnostics', builder: (context, state) => const DiagnosticsScreen()),
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
  final scheme = ColorScheme.fromSeed(seedColor: const Color(0xff2563eb), brightness: brightness);
  return ThemeData(
    useMaterial3: true,
    brightness: brightness,
    colorScheme: scheme,
    scaffoldBackgroundColor: brightness == Brightness.dark ? const Color(0xff111827) : const Color(0xfff4f7fb),
    cardTheme: const CardThemeData(margin: EdgeInsets.zero),
    inputDecorationTheme: const InputDecorationTheme(border: OutlineInputBorder()),
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
          Text('الأساس التقني المحلي', style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w800)),
          const SizedBox(height: 8),
          Text('Foundation فقط — لا توجد Business Modules في هذه المرحلة.', style: Theme.of(context).textTheme.bodyLarge),
          const SizedBox(height: 20),
          LayoutBuilder(builder: (context, constraints) {
            final columns = constraints.maxWidth > 720 ? 3 : 1;
            return GridView.count(
              crossAxisCount: columns,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
              childAspectRatio: 1.7,
              children: [
                FoundationCard(title: 'API Health', child: health.when(
                  data: (value) => Metric(value: value.status, caption: '${value.environment} · ${value.version}'),
                  loading: () => const LoadingState(),
                  error: (error, _) => const ErrorState(message: 'API المحلي غير متاح.'),
                )),
                const FoundationCard(title: 'Routing', child: Metric(value: 'GoRouter', caption: 'ready')),
                const FoundationCard(title: 'State', child: Metric(value: 'Riverpod', caption: 'ready')),
              ],
            );
          }),
          const SizedBox(height: 20),
          const FoundationCard(title: 'Scope', child: EmptyState(title: 'لا توجد وحدات أعمال بعد', message: 'يبدأ التنفيذ العمودي بعد إغلاق Foundation.')),
        ],
      ),
    );
  }
}

class DiagnosticsScreen extends StatelessWidget {
  const DiagnosticsScreen({super.key});

  @override
  Widget build(BuildContext context) => const AppShell(title: 'التشخيص', child: EmptyState(title: 'Diagnostics foundation', message: 'لا يتم عرض أسرار أو بيانات حساسة هنا.'));
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
              onPressed: () => ref.read(themeModeProvider.notifier).state = themeMode == ThemeMode.dark ? ThemeMode.light : ThemeMode.dark,
              icon: const Icon(Icons.brightness_6_outlined),
            ),
          ],
        ),
        drawer: Drawer(child: SafeArea(child: ListView(children: [const DrawerHeader(child: Text('LogicFit', style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold))), ListTile(title: const Text('الأساس التقني'), leading: const Icon(Icons.foundation_outlined), onTap: () => context.go('/')), ListTile(title: const Text('Diagnostics'), leading: const Icon(Icons.health_and_safety_outlined), onTap: () => context.go('/diagnostics'))]))),
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
  Widget build(BuildContext context) => Card(child: Padding(padding: const EdgeInsets.all(18), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(title, style: Theme.of(context).textTheme.titleSmall), const SizedBox(height: 12), child])));
}

class Metric extends StatelessWidget {
  const Metric({required this.value, required this.caption, super.key});
  final String value;
  final String caption;
  @override
  Widget build(BuildContext context) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(value, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w800)), Text(caption)]);
}

class LoadingState extends StatelessWidget {
  const LoadingState({super.key});
  @override
  Widget build(BuildContext context) => const Row(children: [SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2)), SizedBox(width: 8), Text('جارٍ التحميل…')]);
}

class ErrorState extends StatelessWidget {
  const ErrorState({required this.message, super.key});
  final String message;
  @override
  Widget build(BuildContext context) => Text(message, style: TextStyle(color: Theme.of(context).colorScheme.error));
}

class EmptyState extends StatelessWidget {
  const EmptyState({required this.title, required this.message, super.key});
  final String title;
  final String message;
  @override
  Widget build(BuildContext context) => Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(title, style: const TextStyle(fontWeight: FontWeight.bold)), const SizedBox(height: 6), Text(message)]);
}
