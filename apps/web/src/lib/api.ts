import type { HealthResponse, ReadinessResponse } from '@logicfit/shared-contracts';

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:5199/api/v1').replace(/\/$/, '');

export class ApiClientError extends Error {
  constructor(public readonly status: number, message: string, public readonly code?: string) {
    super(message);
    this.name = 'ApiClientError';
  }
}

export type AuthUser = {
  userId: string;
  email: string;
  displayName: string;
  status: string;
  lastLoginAtUtc: string | null;
  version: string;
};

export type AuthSession = {
  accessToken: string;
  sessionId: string;
  requiresMfa: boolean;
  challenge: string | null;
  mfaVerified: boolean;
  expiresAtUtc: string;
  idleExpiresAtUtc: string;
  absoluteExpiresAtUtc: string;
  user: AuthUser;
};

export type AuthMe = {
  user: AuthUser;
  scopes: Array<{ gymId: string | null; scopeType: string; permissions: string[] }>;
  permissions: string[];
};

export type AuthSessionItem = {
  sessionId: string;
  gymId: string | null;
  sessionKind: string;
  mfaVerified: boolean;
  createdAtUtc: string;
  lastSeenAtUtc: string;
  idleExpiresAtUtc: string;
  absoluteExpiresAtUtc: string;
  expiresAtUtc: string;
  userAgent: string | null;
  isCurrent: boolean;
};

export type MfaEnrollment = {
  factorId: string;
  status: string;
  secret: string;
  provisioningUri: string;
};

export type MfaVerification = {
  verified: boolean;
  session: AuthSession | null;
};

export type AccessPermission = {
  permissionId: string;
  key: string;
  domain: string;
  action: string;
  riskLevel: string;
  description: string;
};

export type AccessRole = {
  roleId: string;
  scopeType: string;
  name: string;
  status: string;
  permissions: AccessPermission[];
};

export type AccessCatalog = {
  permissions: AccessPermission[];
  roles: AccessRole[];
  rolePermissionAssignmentCount: number;
};

export type AccessAssignment = {
  assignmentId: string;
  userId: string;
  roleId: string;
  roleName: string;
  gymId: string | null;
  scopeType: string;
  status: string;
  version: string;
};

export type AccessUser = {
  userId: string;
  email: string;
  displayName: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  version: string;
  assignments: AccessAssignment[];
};

export type RecoveryCodes = { codes: string[] };

const ACCESS_TOKEN_KEY = 'logicfit.session.accessToken';
const SESSION_STATE_KEY = 'logicfit.session.state';

export function getAccessToken() {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY);
}

export function setAccessToken(token: string) {
  sessionStorage.setItem(ACCESS_TOKEN_KEY, token);
}

export function clearAccessToken() {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(SESSION_STATE_KEY);
}

export function persistSessionState(session: AuthSession) {
  const { accessToken: _accessToken, ...safeState } = session;
  sessionStorage.setItem(SESSION_STATE_KEY, JSON.stringify(safeState));
}

export function getStoredSessionState(): AuthSession | null {
  const accessToken = getAccessToken();
  const serialized = sessionStorage.getItem(SESSION_STATE_KEY);
  if (!accessToken || !serialized) return null;

  try {
    const state = JSON.parse(serialized) as Omit<AuthSession, 'accessToken'>;
    if (!state.sessionId || !state.user?.userId || !state.expiresAtUtc) return null;
    return { ...state, accessToken };
  } catch {
    return null;
  }
}

async function request<T>(path: string, init: RequestInit = {}, options: { preserveTokenOnUnauthorized?: boolean } = {}): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  headers.set('X-Request-Id', crypto.randomUUID());
  if (init.body) headers.set('Content-Type', 'application/json');
  const token = getAccessToken();
  if (token) headers.set('Authorization', `Bearer ${token}`);

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
    credentials: 'include',
  });
  const body = await response.json().catch(() => undefined);
  if (!response.ok) {
    if (response.status === 401 && !options.preserveTokenOnUnauthorized) clearAccessToken();
    throw new ApiClientError(response.status, body?.error?.message ?? 'Unable to complete the request.', body?.error?.code);
  }
  return body as T;
}

export const apiClient = {
  baseUrl: API_BASE_URL,
  health: () => request<HealthResponse>('/health'),
  readiness: () => request<ReadinessResponse>('/readiness'),
  login: (email: string, password: string) => request<{ data: AuthSession }>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  refresh: (refreshToken: string) => request<{ data: AuthSession }>('/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) }),
  me: () => request<{ data: AuthMe }>('/auth/me'),
  logout: (sessionId: string) => request<{ data: { sessionId: string; revoked: boolean } }>('/auth/logout', { method: 'POST', body: JSON.stringify({ sessionId }) }),
  verifyMfa: (challenge: string, method: 'totp' | 'recovery_code', code: string) => request<{ data: MfaVerification }>('/auth/mfa/verify', { method: 'POST', body: JSON.stringify({ challenge, method, code }) }, { preserveTokenOnUnauthorized: true }),
  enrollMfa: () => request<{ data: MfaEnrollment }>('/auth/mfa/enroll', { method: 'POST' }),
  disableMfa: (currentPassword?: string, code?: string) => request<{ data: { disabled: boolean } }>('/auth/mfa/disable', { method: 'POST', body: JSON.stringify({ currentPassword: currentPassword || null, code: code || null }) }),
  regenerateRecoveryCodes: (currentPassword?: string, code?: string) => request<{ data: RecoveryCodes }>('/auth/mfa/recovery-codes/regenerate', { method: 'POST', body: JSON.stringify({ currentPassword: currentPassword || null, code: code || null }) }),
  changePassword: (currentPassword: string, newPassword: string) => request<{ data: { changed: boolean; reauthenticationRequired: boolean } }>('/auth/password/change', { method: 'POST', body: JSON.stringify({ currentPassword, newPassword }) }),
  requestPasswordReset: (email: string) => request<{ data: { accepted: boolean } }>('/auth/password-reset/request', { method: 'POST', body: JSON.stringify({ email }) }),
  completePasswordReset: (token: string, newPassword: string) => request<{ data: { accepted: boolean } }>('/auth/password-reset/complete', { method: 'POST', body: JSON.stringify({ token, newPassword }) }),
  listSessions: (page = 1, pageSize = 25) => request<{ data: AuthSessionItem[]; meta: { page: number; pageSize: number; total: number; hasNext: boolean } }>(`/auth/sessions?page=${page}&pageSize=${pageSize}`),
  revokeSession: (sessionId: string, reason?: string) => request<{ data: { sessionId: string; revoked: boolean } }>(`/auth/sessions/${sessionId}/revoke`, { method: 'POST', body: JSON.stringify({ reason: reason || null }) }),
  accessCatalog: () => request<{ data: AccessCatalog }>('/platform/access/catalog'),
  accessUsers: (options: { gymId?: string | null; scopeType?: 'gym' | 'platform'; page?: number; pageSize?: number } = {}) => {
    const params = new URLSearchParams({ page: String(options.page ?? 1), pageSize: String(options.pageSize ?? 100) });
    if (options.gymId) params.set('gymId', options.gymId);
    if (options.scopeType) params.set('scopeType', options.scopeType);
    return request<{ data: AccessUser[]; meta: { page: number; pageSize: number; total: number; hasNext: boolean } }>(`/platform/access/users?${params.toString()}`);
  },
  createAccessUser: (input: { email: string; displayName: string; initialPassword: string; roleId: string; gymId: string | null }) => request<{ data: AccessUser }>('/platform/access/users', { method: 'POST', body: JSON.stringify(input) }),
  changeAccessUserStatus: (userId: string, status: 'active' | 'disabled', reason: string, version: string) => request<{ data: { userId: string; status: string; sessionsRevoked: boolean; version: string } }>(`/platform/access/users/${userId}/status`, { method: 'PATCH', headers: { 'If-Match': `"${version}"` }, body: JSON.stringify({ status, reason }) }),
  assignRole: (userId: string, roleId: string, gymId: string | null, reason: string, version?: string) => {
    const query = gymId ? `?gymId=${encodeURIComponent(gymId)}` : '';
    return request<{ data: AccessAssignment }>(`/platform/access/users/${userId}/role-assignments/${roleId}${query}`, {
      method: 'PUT',
      headers: version ? { 'If-Match': `"${version}"` } : undefined,
      body: JSON.stringify({ reason }),
    });
  },
  revokeRole: (userId: string, assignmentId: string, reason: string, version: string) => request<{ data: { assignmentId: string; revoked: boolean } }>(`/platform/access/users/${userId}/role-assignments/${assignmentId}/revoke`, { method: 'POST', headers: { 'If-Match': `"${version}"` }, body: JSON.stringify({ reason }) }),
};
