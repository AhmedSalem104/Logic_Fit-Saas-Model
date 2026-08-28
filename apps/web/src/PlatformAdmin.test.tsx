import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import App from './App';
import { queryClient } from './providers/query-client';

const user = {
  userId: '11111111-1111-4111-8111-111111111111',
  email: 'platform@example.test',
  displayName: 'Platform Admin',
  status: 'active',
  lastLoginAtUtc: null,
  version: 'AQ==',
};

const session = {
  accessToken: 'platform-session',
  sessionId: '22222222-2222-4222-8222-222222222222',
  requiresMfa: false,
  challenge: null,
  mfaVerified: true,
  expiresAtUtc: '2026-08-29T18:00:00Z',
  idleExpiresAtUtc: '2026-08-29T10:30:00Z',
  absoluteExpiresAtUtc: '2026-08-29T18:00:00Z',
  user,
};

const organization = {
  organizationId: '33333333-3333-4333-8333-333333333333',
  name: 'LogicFit Organization',
  slug: 'logicfit',
  status: 'active',
  createdAtUtc: '2026-08-20T10:00:00Z',
  updatedAtUtc: '2026-08-20T10:00:00Z',
};

const gym = {
  gymId: '44444444-4444-4444-8444-444444444444',
  organizationId: organization.organizationId,
  name: 'LogicFit Gym',
  slug: 'logicfit-gym',
  status: 'active',
  timezoneName: 'Africa/Cairo',
  createdAtUtc: '2026-08-20T10:00:00Z',
  updatedAtUtc: '2026-08-20T10:00:00Z',
};

const database = {
  gymDatabaseId: '55555555-5555-4555-8555-555555555555',
  gymId: gym.gymId,
  databaseName: 'LogicFit_Gym_001_Local',
  environment: 'local',
  schemaVersion: '20260825',
  seedVersion: 'v1',
  status: 'active',
  lastHealthAtUtc: '2026-08-29T09:00:00Z',
};

function response(data: unknown, collection = false) {
  return new Response(JSON.stringify(collection
    ? { data, meta: { requestId: 'web-test', version: 'v1', page: 1, pageSize: 25, total: Array.isArray(data) ? data.length : 0, hasNext: false } }
    : { data, meta: { requestId: 'web-test', version: 'v1' } }), { status: 200 });
}

function prepareSession() {
  sessionStorage.setItem('logicfit.session.accessToken', session.accessToken);
  const { accessToken: _accessToken, ...safeSession } = session;
  sessionStorage.setItem('logicfit.session.state', JSON.stringify(safeSession));
}

function mockPlatformApi() {
  vi.mocked(fetch).mockImplementation(async (input) => {
    const url = String(input);
    if (url.endsWith('/auth/me')) return response({ user, scopes: [{ gymId: null, scopeType: 'platform', permissions: ['platform.view'] }], permissions: ['platform.view'] });
    if (url.endsWith('/platform/overview')) return response({ observedAtUtc: '2026-08-29T09:00:00Z', platformHealth: { status: 'healthy', service: 'api', version: '0.1.0', environment: 'Development' }, organizationCount: 1, gymCounts: { total: 1, byStatus: [{ status: 'active', count: 1 }] }, databaseCounts: { total: 1, byStatus: [{ status: 'active', count: 1 }] } });
    if (url.endsWith('/platform/monitoring')) return response({ observedAtUtc: '2026-08-29T09:00:00Z', platformHealth: { status: 'healthy', service: 'api', version: '0.1.0', environment: 'Development' }, registeredDatabases: [database] });
    if (url.includes('/platform/organizations?')) return response([organization], true);
    if (url.includes('/platform/organizations/')) return response(organization);
    if (url.includes('/gyms?')) return response([gym], true);
    if (url.includes('/gyms/')) return response({ ...gym, databases: [database] });
    if (url.includes('/platform/databases?')) return response([database], true);
    if (url.includes('/platform/databases/')) return response(database);
    return response({ status: 'healthy', service: 'api', database: 'test' });
  });
}

beforeEach(() => {
  queryClient.clear();
  prepareSession();
  vi.stubGlobal('fetch', vi.fn());
  mockPlatformApi();
});

afterEach(async () => {
  await queryClient.cancelQueries();
  cleanup();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
  sessionStorage.clear();
  queryClient.clear();
});

test('Platform Admin overview renders the approved platform-only snapshot', async () => {
  window.history.pushState({}, '', '/platform-admin');
  render(<App />);

  expect(await screen.findByRole('heading', { name: 'نظرة عامة على المنصة' })).toBeInTheDocument();
  expect(screen.getByText('لا تعرض هذه الشاشة أعضاء أو مدفوعات أو حضورًا أو خطط تدريب أو تغذية أو معاملات متجر أو سجلات CRM.')).toBeInTheDocument();
});

test('Platform Admin registry routes use the corresponding read APIs', async () => {
  window.history.pushState({}, '', '/platform-admin/organizations');
  render(<App />);
  expect(await screen.findByRole('heading', { name: 'المؤسسات والأندية' })).toBeInTheDocument();
  expect(await screen.findByText('LogicFit Organization')).toBeInTheDocument();

  window.history.pushState({}, '', `/platform-admin/gyms/${gym.gymId}`);
  window.dispatchEvent(new PopStateEvent('popstate'));
  expect(await screen.findByRole('heading', { name: 'LogicFit Gym' })).toBeInTheDocument();

  window.history.pushState({}, '', '/platform-admin/databases');
  window.dispatchEvent(new PopStateEvent('popstate'));
  expect(await screen.findByRole('heading', { name: 'سجل قواعد البيانات' })).toBeInTheDocument();

  window.history.pushState({}, '', '/platform-admin/operations');
  window.dispatchEvent(new PopStateEvent('popstate'));
  expect(await screen.findByRole('heading', { name: 'لقطة العمليات' })).toBeInTheDocument();
});
