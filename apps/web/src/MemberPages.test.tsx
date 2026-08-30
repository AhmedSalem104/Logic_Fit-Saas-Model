import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import App from './App';
import { queryClient } from './providers/query-client';

const session = {
  accessToken: 'member-session',
  sessionId: '11111111-1111-4111-8111-111111111111',
  requiresMfa: false,
  challenge: null,
  mfaVerified: true,
  expiresAtUtc: '2026-08-30T18:00:00Z',
  idleExpiresAtUtc: '2026-08-30T10:30:00Z',
  absoluteExpiresAtUtc: '2026-08-30T18:00:00Z',
  user: { userId: '22222222-2222-4222-8222-222222222222', email: 'member-admin@example.test', displayName: 'Member Admin', status: 'active', lastLoginAtUtc: null, version: 'AQ==' },
};

const me = {
  user: session.user,
  scopes: [{ gymId: '33333333-3333-4333-8333-333333333333', scopeType: 'gym', permissions: ['members.read'] }],
  permissions: ['members.read'],
};

const summary = {
  memberId: '44444444-4444-4444-8444-444444444444',
  memberCode: 'LF-1001',
  fullName: 'Phase 8 Member',
  phone: '+201000000000',
  email: 'member@example.test',
  registrationDate: '2026-08-30',
  status: 'ACTIVE',
  createdAtUtc: '2026-08-30T10:00:00Z',
  updatedAtUtc: '2026-08-30T10:00:00Z',
  version: 'AQ==',
};

const detail = { ...summary, gymId: '33333333-3333-4333-8333-333333333333', notes: 'Core profile only.' };

beforeEach(() => {
  queryClient.clear();
  localStorage.clear();
  sessionStorage.setItem('logicfit.session.accessToken', session.accessToken);
  const { accessToken: _accessToken, ...safeSession } = session;
  sessionStorage.setItem('logicfit.session.state', JSON.stringify(safeSession));
  window.history.pushState({}, '', '/app/members');
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('/auth/me')) return new Response(JSON.stringify({ data: me, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.includes('/members?')) return new Response(JSON.stringify({ data: [summary], meta: { requestId: 'test', version: 'v1', page: 1, pageSize: 25, total: 1, hasNext: false } }), { status: 200 });
    if (url.endsWith(`/members/${summary.memberId}`)) return new Response(JSON.stringify({ data: detail, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.includes(`/members/${summary.memberId}/timeline`)) return new Response(JSON.stringify({ data: [{ eventId: '55555555-5555-4555-8555-555555555555', memberId: summary.memberId, gymId: detail.gymId, eventType: 'MEMBER_CREATED', occurredAt: summary.createdAtUtc, actorId: session.user.userId, metadata: { status: 'ACTIVE' } }], meta: { requestId: 'test', version: 'v1', page: 1, pageSize: 25, total: 1, hasNext: false } }), { status: 200 });
    return new Response(JSON.stringify({ data: { status: 'ready' }, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
  }));
});

afterEach(() => {
  cleanup();
  queryClient.clear();
  vi.unstubAllGlobals();
});

test('Members list uses the scoped API and renders the approved summary fields', async () => {
  render(<App />);

  expect(await screen.findByText('LF-1001')).toBeInTheDocument();
  expect(screen.getByText('Phase 8 Member')).toBeInTheDocument();
  expect(vi.mocked(fetch)).toHaveBeenCalledWith(expect.stringContaining('/gyms/33333333-3333-4333-8333-333333333333/members?'), expect.anything());
});

test('Member detail renders the profile and bounded Member timeline', async () => {
  window.history.pushState({}, '', `/app/members/${summary.memberId}`);
  render(<App />);

  expect(await screen.findByText('Core profile only.')).toBeInTheDocument();
  expect(screen.getByText('MEMBER_CREATED')).toBeInTheDocument();
  await waitFor(() => expect(vi.mocked(fetch).mock.calls.some(([input]) => String(input).includes('/timeline'))).toBe(true));
});
