import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dio/dio.dart';
import 'package:logicfit_mobile/main.dart';
import 'package:logicfit_mobile/auth.dart';

void main() {
  testWidgets('renders the RTL foundation shell', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          healthProvider.overrideWith(
            (ref) async => const FoundationHealth(
              status: 'ok',
              environment: 'test',
              version: '0.1.0',
            ),
          ),
        ],
        child: const LogicFitApp(),
      ),
    );
    expect(find.text('الأساس التقني المحلي'), findsOneWidget);
    expect(find.text('منصة LogicFit'), findsOneWidget);
  });

  testWidgets('login screen validates credentials before calling the API', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          authControllerProvider.overrideWith((ref) => AuthController(Dio())),
        ],
        child: const MaterialApp(home: LoginScreen()),
      ),
    );

    await tester.tap(find.text('دخول'));
    await tester.pump();

    expect(
      find.text('أدخل بريدًا إلكترونيًا صحيحًا وكلمة مرور.'),
      findsOneWidget,
    );
  });
}
