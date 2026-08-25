import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:logicfit_mobile/main.dart';

void main() {
  testWidgets('renders the RTL foundation shell', (tester) async {
    await tester.pumpWidget(ProviderScope(
      overrides: [healthProvider.overrideWith((ref) async => const FoundationHealth(status: 'ok', environment: 'test', version: '0.1.0'))],
      child: const LogicFitApp(),
    ));
    expect(find.text('الأساس التقني المحلي'), findsOneWidget);
    expect(find.text('منصة LogicFit'), findsOneWidget);
  });
}
