import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:logicfit_mobile/members.dart';

void main() {
  test('parses the approved Member collection envelope', () {
    final page = MobileMemberPage.fromJson({
      'data': [
        {
          'memberId': 'member-1',
          'memberCode': 'LF-0001',
          'fullName': 'عضو تجريبي',
          'phone': '+201000000000',
          'email': null,
          'registrationDate': '2026-08-30',
          'status': 'ACTIVE',
          'createdAtUtc': '2026-08-30T10:00:00Z',
          'updatedAtUtc': '2026-08-30T10:00:00Z',
          'version': 'AQ==',
        },
      ],
      'meta': {'page': 1, 'pageSize': 25, 'total': 1, 'hasNext': false},
    });

    expect(page.items.single.memberCode, 'LF-0001');
    expect(page.items.single.status, 'ACTIVE');
    expect(page.total, 1);
  });

  testWidgets('renders the Members list from the API', (tester) async {
    final dio = Dio()..httpClientAdapter = _MembersFakeAdapter();
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: SizedBox(
            height: 700,
            width: 420,
            child: MembersScreen(dio: dio, accessToken: 'test-token'),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('عضو تجريبي'), findsOneWidget);
    expect(find.textContaining('LF-0001'), findsOneWidget);
  });
}

class _MembersFakeAdapter implements HttpClientAdapter {
  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<List<int>>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    final body = switch (options.path) {
      '/auth/me' => {
        'data': {
          'scopes': [
            {
              'gymId': 'gym-1',
              'permissions': ['members.read'],
            },
          ],
        },
      },
      '/gyms/gym-1/members' => {
        'data': [
          {
            'memberId': 'member-1',
            'memberCode': 'LF-0001',
            'fullName': 'عضو تجريبي',
            'phone': '+201000000000',
            'email': null,
            'registrationDate': '2026-08-30',
            'status': 'ACTIVE',
            'createdAtUtc': '2026-08-30T10:00:00Z',
            'updatedAtUtc': '2026-08-30T10:00:00Z',
            'version': 'AQ==',
          },
        ],
        'meta': {'page': 1, 'pageSize': 25, 'total': 1, 'hasNext': false},
      },
      _ => <String, dynamic>{'data': []},
    };
    return ResponseBody.fromString(
      jsonEncode(body),
      200,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}
