import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import App from './App';
import { queryClient } from './providers/query-client';

const session = {
  sessionId: '11111111-1111-4111-8111-111111111111',
  requiresMfa: false,
  challenge: null,
  mfaVerified: true,
  expiresAtUtc: '2026-08-30T18:00:00Z',
  idleExpiresAtUtc: '2026-08-30T10:30:00Z',
  absoluteExpiresAtUtc: '2026-08-30T18:00:00Z',
  user: { userId: '22222222-2222-4222-8222-222222222222', email: 'platform@example.test', displayName: 'Platform Admin', status: 'active', lastLoginAtUtc: null, version: 'AQ==' },
};

const me = {
  user: session.user,
  scopes: [{ gymId: null, scopeType: 'platform', permissions: ['platform.view', 'platform.provision'] }],
  permissions: ['platform.view', 'platform.provision'],
};

const accepted = {
  operationId: '33333333-3333-4333-8333-333333333333',
  organizationId: '44444444-4444-4444-8444-444444444444',
  gymId: '55555555-5555-4555-8555-555555555555',
  status: 'Requested',
  currentStep: null,
  requestedAtUtc: '2026-08-29T10:00:00Z',
  statusUrl: '/api/v1/platform/provisioning/33333333-3333-4333-8333-333333333333',
};

const activeStatus = {
  ...accepted,
  status: 'Active',
  attemptNo: 1,
  startedAtUtc: '2026-08-29T10:00:01Z',
  completedAtUtc: '2026-08-29T10:00:10Z',
  server: { serverId: '5e5f5f7e-31d2-4f0c-9ec4-0fcf3fdbac73', environment: 'local', status: 'active' },
  database: { databaseId: '66666666-6666-4666-8666-666666666666', databaseName: 'LogicFit_Gym_55555555555545558555555555555555_local', status: 'Active', schemaVersion: 'InitialGymFoundation', seedVersion: 'v1' },
  ownerInitialized: true,
  retryable: false,
  failure: null,
  steps: ['RequestValidation', 'OrganizationCreation', 'GymRegistryCreation', 'ServerPlacement', 'DatabaseCreation', 'EfCoreMigrations', 'CanonicalSeeding', 'Verification', 'OwnerInitialization', 'Activation'].map((stepKey) => ({ stepKey, status: 'Success', attemptNo: 1, startedAtUtc: '2026-08-29T10:00:01Z', completedAtUtc: '2026-08-29T10:00:02Z', retryable: false, failureCategory: null })),
};

function seedSession() {
  sessionStorage.setItem('logicfit.session.accessToken', 'opaque-platform-session');
  sessionStorage.setItem('logicfit.session.state', JSON.stringify(session));
}

afterEach(() => {
  cleanup();
  queryClient.clear();
  vi.unstubAllGlobals();
  localStorage.clear();
  sessionStorage.clear();
});

beforeEach(() => {
  seedSession();
  window.history.pushState({}, '', '/platform-admin/provisioning');
});

test('submits the approved provisioning request and renders the active operation', async () => {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.endsWith('/auth/me')) return new Response(JSON.stringify({ data: me, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.endsWith('/platform/provisioning') && init?.method === 'POST') {
      const headers = new Headers(init.headers);
      expect(headers.get('Idempotency-Key')).toBeTruthy();
      expect(JSON.parse(String(init.body))).not.toHaveProperty('databaseName');
      return new Response(JSON.stringify({ data: accepted, meta: { requestId: 'test', version: 'v1' } }), { status: 202 });
    }
    if (url.endsWith(`/platform/provisioning/${accepted.operationId}`)) return new Response(JSON.stringify({ data: activeStatus, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    return new Response(JSON.stringify({ data: { status: 'ready' }, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
  });
  vi.stubGlobal('fetch', fetchMock);

  render(<App />);
  expect(await screen.findByRole('heading', { name: 'تهيئة نادٍ جديد' })).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('اسم المؤسسة'), { target: { value: 'Example Fitness Group' } });
  fireEvent.change(screen.getByLabelText('معرّف المؤسسة (slug)'), { target: { value: 'example-fitness-group' } });
  fireEvent.change(screen.getByLabelText('اسم النادي'), { target: { value: 'Downtown Gym' } });
  fireEvent.change(screen.getByLabelText('معرّف النادي (slug)'), { target: { value: 'downtown' } });
  fireEvent.change(screen.getByLabelText('البريد الإلكتروني'), { target: { value: 'owner@example.test' } });
  fireEvent.change(screen.getByLabelText('اسم العرض'), { target: { value: 'Gym Owner' } });
  fireEvent.change(screen.getByLabelText('كلمة المرور الأولية'), { target: { value: 'Local Test Password 123!' } });
  fireEvent.click(screen.getByRole('button', { name: 'بدء التهيئة' }));

  expect(await screen.findByRole('heading', { name: 'حالة تهيئة النادي' })).toBeInTheDocument();
  expect(await screen.findByText('تم تفعيل النادي بنجاح. تم تهيئة قاعدة البيانات والبيانات المرجعية والمالك الأول.')).toBeInTheDocument();
  expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/platform/provisioning'), expect.objectContaining({ method: 'POST' }));
});

test('renders a retryable failure and sends the retry operation through the same API namespace', async () => {
  window.history.pushState({}, '', `/platform-admin/provisioning/${accepted.operationId}`);
  const failed = { ...activeStatus, status: 'MigrationFailed', currentStep: 'EfCoreMigrations', completedAtUtc: null, retryable: true, failure: { failureCategory: 'transient', errorCode: 'MIGRATION_FAILED', failedStep: 'EfCoreMigrations', occurredAtUtc: '2026-08-29T10:00:10Z', retryable: true }, steps: activeStatus.steps.map((step) => step.stepKey === 'EfCoreMigrations' ? { ...step, status: 'Failed', retryable: true, failureCategory: 'transient' } : step) };
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url.endsWith('/auth/me')) return new Response(JSON.stringify({ data: me, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.endsWith(`/platform/provisioning/${accepted.operationId}/retry`)) {
      expect(init?.method).toBe('POST');
      return new Response(JSON.stringify({ data: { operationId: accepted.operationId, status: 'MigrationFailed', retryAccepted: true, failedStep: 'EfCoreMigrations', nextStep: 'EfCoreMigrations', nextAttemptNo: 2, retryable: true }, meta: { requestId: 'test', version: 'v1' } }), { status: 202 });
    }
    return new Response(JSON.stringify({ data: failed, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
  });
  vi.stubGlobal('fetch', fetchMock);

  render(<App />);
  expect(await screen.findByText('فشلت العملية: MIGRATION_FAILED · transient يمكن إعادة المحاولة.')).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('سبب آمن لإعادة المحاولة'), { target: { value: 'إعادة المحاولة بعد استعادة اتصال SQL Server' } });
  fireEvent.click(screen.getByRole('button', { name: 'إعادة المحاولة' }));
  await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/retry'), expect.objectContaining({ method: 'POST' })));
});
