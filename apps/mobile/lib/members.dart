import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

const _memberStatuses = <String>{'ACTIVE', 'INACTIVE', 'ARCHIVED'};

class MobileMemberSummary {
  const MobileMemberSummary({
    required this.memberId,
    required this.memberCode,
    required this.fullName,
    required this.phone,
    required this.email,
    required this.registrationDate,
    required this.status,
    required this.createdAtUtc,
    required this.updatedAtUtc,
    required this.version,
  });

  factory MobileMemberSummary.fromJson(Map<String, dynamic> json) =>
      MobileMemberSummary(
        memberId: json['memberId'] as String? ?? '',
        memberCode: json['memberCode'] as String? ?? '',
        fullName: json['fullName'] as String? ?? '',
        phone: json['phone'] as String? ?? '',
        email: json['email'] as String?,
        registrationDate: json['registrationDate'] as String? ?? '',
        status: json['status'] as String? ?? 'UNKNOWN',
        createdAtUtc: json['createdAtUtc'] as String? ?? '',
        updatedAtUtc: json['updatedAtUtc'] as String? ?? '',
        version: json['version'] as String? ?? '',
      );

  final String memberId;
  final String memberCode;
  final String fullName;
  final String phone;
  final String? email;
  final String registrationDate;
  final String status;
  final String createdAtUtc;
  final String updatedAtUtc;
  final String version;
}

class MobileMemberDetail extends MobileMemberSummary {
  const MobileMemberDetail({
    required super.memberId,
    required super.memberCode,
    required super.fullName,
    required super.phone,
    required super.email,
    required super.registrationDate,
    required super.status,
    required super.createdAtUtc,
    required super.updatedAtUtc,
    required super.version,
    required this.gymId,
    required this.notes,
  });

  factory MobileMemberDetail.fromJson(Map<String, dynamic> json) =>
      MobileMemberDetail(
        memberId: json['memberId'] as String? ?? '',
        memberCode: json['memberCode'] as String? ?? '',
        fullName: json['fullName'] as String? ?? '',
        phone: json['phone'] as String? ?? '',
        email: json['email'] as String?,
        registrationDate: json['registrationDate'] as String? ?? '',
        status: json['status'] as String? ?? 'UNKNOWN',
        createdAtUtc: json['createdAtUtc'] as String? ?? '',
        updatedAtUtc: json['updatedAtUtc'] as String? ?? '',
        version: json['version'] as String? ?? '',
        gymId: json['gymId'] as String? ?? '',
        notes: json['notes'] as String?,
      );

  final String gymId;
  final String? notes;
}

class MobileMemberTimelineItem {
  const MobileMemberTimelineItem({
    required this.eventId,
    required this.memberId,
    required this.gymId,
    required this.eventType,
    required this.occurredAt,
    required this.actorId,
    required this.metadata,
  });

  factory MobileMemberTimelineItem.fromJson(Map<String, dynamic> json) =>
      MobileMemberTimelineItem(
        eventId: json['eventId'] as String? ?? '',
        memberId: json['memberId'] as String? ?? '',
        gymId: json['gymId'] as String? ?? '',
        eventType: json['eventType'] as String? ?? '',
        occurredAt: json['occurredAt'] as String? ?? '',
        actorId: json['actorId'] as String?,
        metadata: (json['metadata'] as Map<String, dynamic>?) ?? const {},
      );

  final String eventId;
  final String memberId;
  final String gymId;
  final String eventType;
  final String occurredAt;
  final String? actorId;
  final Map<String, dynamic> metadata;
}

class MobileMemberPage {
  const MobileMemberPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.total,
    required this.hasNext,
  });

  factory MobileMemberPage.fromJson(Map<String, dynamic> json) {
    final data = json['data'];
    final meta = json['meta'] as Map<String, dynamic>? ?? const {};
    return MobileMemberPage(
      items: data is List
          ? data
                .whereType<Map<String, dynamic>>()
                .map(MobileMemberSummary.fromJson)
                .toList(growable: false)
          : const [],
      page: (meta['page'] as num?)?.toInt() ?? 1,
      pageSize: (meta['pageSize'] as num?)?.toInt() ?? 25,
      total: (meta['total'] as num?)?.toInt() ?? 0,
      hasNext: meta['hasNext'] as bool? ?? false,
    );
  }

  final List<MobileMemberSummary> items;
  final int page;
  final int pageSize;
  final int total;
  final bool hasNext;
}

class MobileMemberTimelinePage {
  const MobileMemberTimelinePage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.total,
    required this.hasNext,
  });

  factory MobileMemberTimelinePage.fromJson(Map<String, dynamic> json) {
    final data = json['data'];
    final meta = json['meta'] as Map<String, dynamic>? ?? const {};
    return MobileMemberTimelinePage(
      items: data is List
          ? data
                .whereType<Map<String, dynamic>>()
                .map(MobileMemberTimelineItem.fromJson)
                .toList(growable: false)
          : const [],
      page: (meta['page'] as num?)?.toInt() ?? 1,
      pageSize: (meta['pageSize'] as num?)?.toInt() ?? 25,
      total: (meta['total'] as num?)?.toInt() ?? 0,
      hasNext: meta['hasNext'] as bool? ?? false,
    );
  }

  final List<MobileMemberTimelineItem> items;
  final int page;
  final int pageSize;
  final int total;
  final bool hasNext;
}

class MembersApi {
  MembersApi(this._dio, this._accessToken);

  final Dio _dio;
  final String _accessToken;

  Options get _authorized =>
      Options(headers: {'Authorization': 'Bearer $_accessToken'});

  Future<String> resolveGymId() async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/auth/me',
      options: _authorized,
    );
    final data = response.data?['data'] as Map<String, dynamic>? ?? const {};
    final scopes = data['scopes'];
    if (scopes is List) {
      for (final item in scopes.whereType<Map<String, dynamic>>()) {
        final gymId = item['gymId'] as String?;
        final permissions = item['permissions'];
        if (gymId != null &&
            permissions is List &&
            permissions.whereType<String>().contains('members.read')) {
          return gymId;
        }
      }
    }
    throw MembersApiException('لا يوجد نطاق Gym مصرح له بقراءة الأعضاء.');
  }

  Future<MobileMemberPage> list(
    String gymId, {
    int page = 1,
    String? search,
    String? status,
  }) async {
    final query = <String, dynamic>{'page': page, 'pageSize': 25};
    if (search != null && search.trim().isNotEmpty) {
      query['search'] = search.trim();
    }
    if (status != null && _memberStatuses.contains(status)) {
      query['status'] = status;
    }
    final response = await _dio.get<Map<String, dynamic>>(
      '/gyms/$gymId/members',
      queryParameters: query,
      options: _authorized,
    );
    return MobileMemberPage.fromJson(response.data ?? const {});
  }

  Future<MobileMemberDetail> get(String gymId, String memberId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/gyms/$gymId/members/$memberId',
      options: _authorized,
    );
    return MobileMemberDetail.fromJson(
      (response.data?['data'] as Map<String, dynamic>?) ?? const {},
    );
  }

  Future<MobileMemberTimelinePage> timeline(
    String gymId,
    String memberId,
  ) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/gyms/$gymId/members/$memberId/timeline',
      queryParameters: const {'page': 1, 'pageSize': 25},
      options: _authorized,
    );
    return MobileMemberTimelinePage.fromJson(response.data ?? const {});
  }
}

class MembersApiException implements Exception {
  const MembersApiException(this.message);
  final String message;

  @override
  String toString() => message;
}

class MembersScreen extends StatefulWidget {
  const MembersScreen({
    required this.dio,
    required this.accessToken,
    super.key,
  });

  final Dio dio;
  final String accessToken;

  @override
  State<MembersScreen> createState() => _MembersScreenState();
}

class _MembersScreenState extends State<MembersScreen> {
  final _searchController = TextEditingController();
  String? _status;
  late Future<_MembersListState> _membersFuture;

  @override
  void initState() {
    super.initState();
    _membersFuture = _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<_MembersListState> _load() async {
    final api = MembersApi(widget.dio, widget.accessToken);
    final gymId = await api.resolveGymId();
    final page = await api.list(
      gymId,
      search: _searchController.text,
      status: _status,
    );
    return _MembersListState(gymId, page);
  }

  void _reload() => setState(() => _membersFuture = _load());

  @override
  Widget build(BuildContext context) => FutureBuilder<_MembersListState>(
    future: _membersFuture,
    builder: (context, snapshot) {
      if (snapshot.connectionState == ConnectionState.waiting) {
        return const _MobileLoading();
      }
      if (snapshot.hasError) {
        return _MobileError(
          message: _messageFor(snapshot.error),
          onRetry: _reload,
        );
      }
      final state = snapshot.data!;
      return Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _MembersFilters(
            searchController: _searchController,
            status: _status,
            onStatusChanged: (value) {
              setState(() {
                _status = value;
                _membersFuture = _load();
              });
            },
            onSearch: _reload,
          ),
          const SizedBox(height: 12),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => _reload(),
              child: state.page.items.isEmpty
                  ? ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      children: const [
                        SizedBox(height: 80),
                        _MobileEmpty(
                          title: 'لا يوجد أعضاء',
                          message: 'ستظهر الأعضاء المصرح بهم هنا.',
                        ),
                      ],
                    )
                  : ListView.separated(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.only(bottom: 20),
                      itemCount: state.page.items.length,
                      separatorBuilder: (_, _) => const SizedBox(height: 8),
                      itemBuilder: (context, index) {
                        final member = state.page.items[index];
                        return Card(
                          child: ListTile(
                            title: Text(member.fullName),
                            subtitle: Text(
                              '${member.memberCode}\n${member.phone}${member.email == null ? '' : ' · ${member.email}'}',
                            ),
                            isThreeLine: true,
                            trailing: _StatusChip(status: member.status),
                            onTap: () =>
                                context.go('/app/members/${member.memberId}'),
                          ),
                        );
                      },
                    ),
            ),
          ),
          if (state.page.hasNext)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: Text(
                'إجمالي النتائج: ${state.page.total} · اعرض المزيد من البحث أو الفلترة.',
                textAlign: TextAlign.center,
              ),
            ),
        ],
      );
    },
  );
}

class MemberDetailScreen extends StatefulWidget {
  const MemberDetailScreen({
    required this.dio,
    required this.accessToken,
    required this.memberId,
    super.key,
  });

  final Dio dio;
  final String accessToken;
  final String memberId;

  @override
  State<MemberDetailScreen> createState() => _MemberDetailScreenState();
}

class _MemberDetailScreenState extends State<MemberDetailScreen> {
  late Future<_MemberDetailState> _detailFuture;

  @override
  void initState() {
    super.initState();
    _detailFuture = _load();
  }

  Future<_MemberDetailState> _load() async {
    final api = MembersApi(widget.dio, widget.accessToken);
    final gymId = await api.resolveGymId();
    final member = await api.get(gymId, widget.memberId);
    final timeline = await api.timeline(gymId, widget.memberId);
    return _MemberDetailState(member, timeline);
  }

  void _reload() => setState(() => _detailFuture = _load());

  @override
  Widget build(BuildContext context) => FutureBuilder<_MemberDetailState>(
    future: _detailFuture,
    builder: (context, snapshot) {
      if (snapshot.connectionState == ConnectionState.waiting) {
        return const _MobileLoading();
      }
      if (snapshot.hasError) {
        return _MobileError(
          message: _messageFor(snapshot.error),
          onRetry: _reload,
        );
      }
      final state = snapshot.data!;
      final member = state.member;
      return ListView(
        padding: const EdgeInsets.only(bottom: 20),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          member.fullName,
                          style: Theme.of(context).textTheme.headlineSmall,
                        ),
                      ),
                      _StatusChip(status: member.status),
                    ],
                  ),
                  const Divider(height: 24),
                  _DetailRow(label: 'رمز العضو', value: member.memberCode),
                  _DetailRow(label: 'الهاتف', value: member.phone),
                  _DetailRow(
                    label: 'البريد الإلكتروني',
                    value: member.email ?? '—',
                  ),
                  _DetailRow(
                    label: 'تاريخ التسجيل',
                    value: member.registrationDate,
                  ),
                  _DetailRow(label: 'ملاحظات', value: member.notes ?? '—'),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'السجل الزمني',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 12),
                  if (state.timeline.items.isEmpty)
                    const _MobileEmpty(
                      title: 'لا توجد أحداث',
                      message: 'لم تسجل أحداث هذا العضو بعد.',
                    )
                  else
                    ...state.timeline.items.map(
                      (event) => ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: const Icon(Icons.history),
                        title: Text(event.eventType),
                        subtitle: Text(event.occurredAt),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ],
      );
    },
  );
}

class _MembersListState {
  const _MembersListState(this.gymId, this.page);
  final String gymId;
  final MobileMemberPage page;
}

class _MemberDetailState {
  const _MemberDetailState(this.member, this.timeline);
  final MobileMemberDetail member;
  final MobileMemberTimelinePage timeline;
}

class _MembersFilters extends StatelessWidget {
  const _MembersFilters({
    required this.searchController,
    required this.status,
    required this.onStatusChanged,
    required this.onSearch,
  });

  final TextEditingController searchController;
  final String? status;
  final ValueChanged<String?> onStatusChanged;
  final VoidCallback onSearch;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      TextField(
        controller: searchController,
        textInputAction: TextInputAction.search,
        decoration: InputDecoration(
          labelText: 'بحث في الأعضاء',
          prefixIcon: const Icon(Icons.search),
          suffixIcon: IconButton(
            onPressed: onSearch,
            icon: const Icon(Icons.arrow_forward),
            tooltip: 'بحث',
          ),
        ),
        onSubmitted: (_) => onSearch(),
      ),
      const SizedBox(height: 8),
      DropdownButtonFormField<String?>(
        initialValue: status,
        decoration: const InputDecoration(labelText: 'الحالة'),
        items: const [
          DropdownMenuItem<String?>(
            value: null,
            child: Text('النشط وغير النشط'),
          ),
          DropdownMenuItem<String?>(value: 'ACTIVE', child: Text('نشط')),
          DropdownMenuItem<String?>(value: 'INACTIVE', child: Text('غير نشط')),
          DropdownMenuItem<String?>(value: 'ARCHIVED', child: Text('مؤرشف')),
        ],
        onChanged: onStatusChanged,
      ),
    ],
  );
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status});
  final String status;

  @override
  Widget build(BuildContext context) => Chip(label: Text(_statusLabel(status)));
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 130,
          child: Text(
            label,
            style: const TextStyle(fontWeight: FontWeight.bold),
          ),
        ),
        Expanded(child: Text(value)),
      ],
    ),
  );
}

class _MobileLoading extends StatelessWidget {
  const _MobileLoading();

  @override
  Widget build(BuildContext context) =>
      const Center(child: CircularProgressIndicator());
}

class _MobileError extends StatelessWidget {
  const _MobileError({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(20),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          FilledButton.tonal(
            onPressed: onRetry,
            child: const Text('إعادة المحاولة'),
          ),
        ],
      ),
    ),
  );
}

class _MobileEmpty extends StatelessWidget {
  const _MobileEmpty({required this.title, required this.message});
  final String title;
  final String message;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
      const SizedBox(height: 6),
      Text(message, textAlign: TextAlign.center),
    ],
  );
}

String _statusLabel(String status) => switch (status) {
  'ACTIVE' => 'نشط',
  'INACTIVE' => 'غير نشط',
  'ARCHIVED' => 'مؤرشف',
  _ => status,
};

String _messageFor(Object? error) {
  if (error is MembersApiException) return error.message;
  if (error is DioException) {
    final body = error.response?.data;
    if (body is Map<String, dynamic>) {
      final apiError = body['error'];
      if (apiError is Map<String, dynamic> && apiError['message'] is String) {
        return apiError['message'] as String;
      }
    }
  }
  return 'تعذر تحميل بيانات الأعضاء. تحقق من الاتصال والصلاحيات.';
}
