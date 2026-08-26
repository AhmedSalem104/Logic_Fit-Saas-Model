import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import App from './App';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);
    if (url.endsWith('/health')) {
      return new Response(JSON.stringify({ data: { status: 'healthy', service: 'api', environment: 'test', version: '0.1.0' }, meta: { requestId: 'test', version: 'v1' } }), { status: 200, headers: { 'Content-Type': 'application/json' } });
    }
    if (url.endsWith('/auth/login')) {
      return new Response(JSON.stringify({ error: { code: 'AUTHENTICATION_FAILED', message: 'The supplied credentials could not be authenticated.' }, meta: { requestId: 'test', version: 'v1' } }), { status: 401, headers: { 'Content-Type': 'application/json' } });
    }
    return new Response(JSON.stringify({ data: { status: 'ready', service: 'api', database: 'test' }, meta: { requestId: 'test', version: 'v1' } }), { status: 200, headers: { 'Content-Type': 'application/json' } });
  }));
  localStorage.clear();
  sessionStorage.clear();
  window.history.pushState({}, '', '/');
});

test('renders the RTL foundation shell and health state', async () => {
  render(<App />);
  expect(await screen.findByText('الأساس التقني المحلي')).toBeInTheDocument();
  expect(document.documentElement.dir).toBe('rtl');
  expect(screen.getByText('API يعمل')).toBeInTheDocument();
});

test('login form sends credentials through the API and shows safe errors', async () => {
  window.history.pushState({}, '', '/login');
  render(<App />);
  fireEvent.change(screen.getByLabelText('البريد الإلكتروني'), { target: { value: 'member@example.test' } });
  fireEvent.change(screen.getByLabelText('كلمة المرور'), { target: { value: 'wrong-password' } });
  fireEvent.click(screen.getByRole('button', { name: 'دخول آمن' }));
  await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('The supplied credentials could not be authenticated.'));
  expect(vi.mocked(fetch)).toHaveBeenCalledWith(expect.stringContaining('/auth/login'), expect.objectContaining({ method: 'POST' }));
});

test('successful login resolves the session through the API and opens the protected shell', async () => {
  window.history.pushState({}, '', '/login');
  const session = {
    accessToken: 'opaque-test-session',
    sessionId: '11111111-1111-4111-8111-111111111111',
    requiresMfa: false,
    challenge: null,
    mfaVerified: true,
    expiresAtUtc: '2026-08-26T18:00:00Z',
    idleExpiresAtUtc: '2026-08-26T10:30:00Z',
    absoluteExpiresAtUtc: '2026-08-26T18:00:00Z',
    user: { userId: '22222222-2222-4222-8222-222222222222', email: 'user@example.test', displayName: 'Local User', status: 'active', lastLoginAtUtc: null, version: 'AQ==' },
  };
  const me = { user: session.user, scopes: [{ gymId: '33333333-3333-4333-8333-333333333333', scopeType: 'gym', permissions: ['auth.logout'] }], permissions: ['auth.logout'] };
  vi.mocked(fetch).mockImplementation(async (input) => {
    const url = String(input);
    if (url.endsWith('/auth/login')) return new Response(JSON.stringify({ data: session, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.endsWith('/auth/me')) return new Response(JSON.stringify({ data: me, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    return new Response(JSON.stringify({ data: { status: 'ready', service: 'api', database: 'test' }, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
  });

  render(<App />);
  fireEvent.change(screen.getByLabelText('البريد الإلكتروني'), { target: { value: session.user.email } });
  fireEvent.change(screen.getByLabelText('كلمة المرور'), { target: { value: 'Local Test Password 123!' } });
  fireEvent.click(screen.getByRole('button', { name: 'دخول آمن' }));

  expect(await screen.findByText(/AUTHENTICATED SHELL/)).toBeInTheDocument();
  expect(sessionStorage.getItem('logicfit.session.accessToken')).toBe(session.accessToken);
});

test('MFA verification uses the approved combined TOTP/recovery endpoint before opening the shell', async () => {
  window.history.pushState({}, '', '/login');
  const pending = {
    accessToken: 'opaque-pending-session',
    sessionId: '44444444-4444-4444-8444-444444444444',
    requiresMfa: true,
    challenge: '44444444-4444-4444-8444-444444444444',
    mfaVerified: false,
    expiresAtUtc: '2026-08-26T18:00:00Z',
    idleExpiresAtUtc: '2026-08-26T10:30:00Z',
    absoluteExpiresAtUtc: '2026-08-26T18:00:00Z',
    user: { userId: '55555555-5555-4555-8555-555555555555', email: 'mfa@example.test', displayName: 'MFA User', status: 'active', lastLoginAtUtc: null, version: 'AQ==' },
  };
  const verified = { ...pending, accessToken: 'opaque-verified-session', requiresMfa: false, challenge: null, mfaVerified: true };
  const me = { user: verified.user, scopes: [], permissions: ['auth.logout'] };
  vi.mocked(fetch).mockImplementation(async (input, init) => {
    const url = String(input);
    if (url.endsWith('/auth/login')) return new Response(JSON.stringify({ data: pending, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.endsWith('/auth/mfa/verify')) {
      expect(init?.body).toContain('recovery_code');
      return new Response(JSON.stringify({ data: { verified: true, session: verified }, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    }
    if (url.endsWith('/auth/me')) return new Response(JSON.stringify({ data: me, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    return new Response(JSON.stringify({ data: { status: 'ready', service: 'api', database: 'test' }, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
  });

  render(<App />);
  fireEvent.change(screen.getByLabelText('البريد الإلكتروني'), { target: { value: pending.user.email } });
  fireEvent.change(screen.getByLabelText('كلمة المرور'), { target: { value: 'Local Test Password 123!' } });
  fireEvent.click(screen.getByRole('button', { name: 'دخول آمن' }));
  expect(await screen.findByText('تحقق MFA')).toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('طريقة التحقق'), { target: { value: 'recovery_code' } });
  fireEvent.change(screen.getByLabelText('الرمز'), { target: { value: 'recovery-code' } });
  fireEvent.click(screen.getByRole('button', { name: 'تحقق' }));

  expect(await screen.findByText(/AUTHENTICATED SHELL/)).toBeInTheDocument();
  expect(vi.mocked(fetch)).toHaveBeenCalledWith(expect.stringContaining('/auth/mfa/verify'), expect.objectContaining({ method: 'POST' }));
});

test('protected routes redirect to login when the server session is not available', async () => {
  window.history.pushState({}, '', '/app');
  render(<App />);

  expect(await screen.findByRole('heading', { name: 'تسجيل الدخول' })).toBeInTheDocument();
});

test('platform security scope opens the access administration contract without inventing a Gym id', async () => {
  window.history.pushState({}, '', '/login');
  const session = {
    accessToken: 'opaque-platform-session',
    sessionId: '66666666-6666-4666-8666-666666666666',
    requiresMfa: false,
    challenge: null,
    mfaVerified: true,
    expiresAtUtc: '2026-08-26T18:00:00Z',
    idleExpiresAtUtc: '2026-08-26T10:30:00Z',
    absoluteExpiresAtUtc: '2026-08-26T18:00:00Z',
    user: { userId: '77777777-7777-4777-8777-777777777777', email: 'platform@example.test', displayName: 'Platform User', status: 'active', lastLoginAtUtc: null, version: 'AQ==' },
  };
  const me = { user: session.user, scopes: [{ gymId: null, scopeType: 'platform', permissions: ['platform.security.manage'] }], permissions: ['platform.security.manage'] };
  const catalog = {
    permissions: [{ permissionId: '88888888-8888-4888-8888-888888888888', key: 'platform.security.manage', domain: 'platform', action: 'security.manage', riskLevel: 'critical', description: 'Manage security.' }],
    roles: [{ roleId: '99999999-9999-4999-8999-999999999999', scopeType: 'platform', name: 'Platform Security Admin', status: 'active', permissions: [] }],
    rolePermissionAssignmentCount: 14,
  };
  vi.mocked(fetch).mockImplementation(async (input) => {
    const url = String(input);
    if (url.endsWith('/auth/login')) return new Response(JSON.stringify({ data: session, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.endsWith('/auth/me')) return new Response(JSON.stringify({ data: me, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.endsWith('/platform/access/catalog')) return new Response(JSON.stringify({ data: catalog, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
    if (url.includes('/platform/access/users?')) {
      expect(url).toContain('scopeType=platform');
      expect(url).not.toContain('gymId=');
      return new Response(JSON.stringify({ data: [], meta: { requestId: 'test', version: 'v1', page: 1, pageSize: 100, total: 0, hasNext: false } }), { status: 200 });
    }
    return new Response(JSON.stringify({ data: { status: 'ready', service: 'api', database: 'test' }, meta: { requestId: 'test', version: 'v1' } }), { status: 200 });
  });

  render(<App />);
  fireEvent.change(screen.getByLabelText('البريد الإلكتروني'), { target: { value: session.user.email } });
  fireEvent.change(screen.getByLabelText('كلمة المرور'), { target: { value: 'Local Test Password 123!' } });
  fireEvent.click(screen.getByRole('button', { name: 'دخول آمن' }));
  fireEvent.click(await screen.findByRole('button', { name: 'فتح إدارة الوصول' }));

  expect(await screen.findByText('المستخدمون والأدوار والصلاحيات')).toBeInTheDocument();
  expect(screen.getByText('مستخدمو Platform')).toBeInTheDocument();
});
