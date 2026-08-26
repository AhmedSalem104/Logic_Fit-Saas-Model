import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { apiClient, clearAccessToken, getAccessToken, getStoredSessionState, persistSessionState, setAccessToken, type AccessCatalog, type AccessUser, type AuthMe, type AuthSession, type AuthSessionItem, type MfaEnrollment, type MfaVerification } from './api';

type AuthContextValue = {
  ready: boolean;
  session: AuthSession | null;
  me: AuthMe | null;
  signIn: (email: string, password: string) => Promise<AuthSession>;
  signOut: () => Promise<void>;
  verifyMfa: (challenge: string, method: 'totp' | 'recovery_code', code: string) => Promise<MfaVerification>;
  refresh: () => Promise<AuthSession>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  listSessions: () => Promise<{ data: AuthSessionItem[]; meta: { page: number; pageSize: number; total: number; hasNext: boolean } }>;
  revokeSession: (sessionId: string, reason?: string) => Promise<void>;
  enrollMfa: () => Promise<MfaEnrollment>;
  disableMfa: (currentPassword?: string, code?: string) => Promise<void>;
  regenerateRecoveryCodes: (currentPassword?: string, code?: string) => Promise<string[]>;
  requestPasswordReset: (email: string) => Promise<void>;
  completePasswordReset: (token: string, newPassword: string) => Promise<void>;
  accessCatalog: () => Promise<AccessCatalog>;
  accessUsers: (options?: { gymId?: string | null; scopeType?: 'gym' | 'platform'; page?: number; pageSize?: number }) => Promise<{ data: AccessUser[]; meta: { page: number; pageSize: number; total: number; hasNext: boolean } }>;
  createAccessUser: (input: { email: string; displayName: string; initialPassword: string; roleId: string; gymId: string | null }) => Promise<AccessUser>;
  changeAccessUserStatus: (userId: string, status: 'active' | 'disabled', reason: string, version: string) => Promise<void>;
  assignRole: (userId: string, roleId: string, gymId: string | null, reason: string, version?: string) => Promise<void>;
  revokeRole: (userId: string, assignmentId: string, reason: string, version: string) => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(false);
  const [session, setSession] = useState<AuthSession | null>(null);
  const [me, setMe] = useState<AuthMe | null>(null);

  useEffect(() => {
    const storedSession = getStoredSessionState();
    if (!getAccessToken() || !storedSession) {
      setReady(true);
      return;
    }

    setSession(storedSession);
    apiClient.me()
      .then((response) => setMe(response.data))
      .catch(() => {
        clearAccessToken();
        setMe(null);
      })
      .finally(() => setReady(true));
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    ready,
    session,
    me,
    signIn: async (email, password) => {
      const response = await apiClient.login(email, password);
      setAccessToken(response.data.accessToken);
      persistSessionState(response.data);
      setSession(response.data);
      if (!response.data.requiresMfa) {
        const current = await apiClient.me();
        setMe(current.data);
      }
      return response.data;
    },
    verifyMfa: async (challenge, method, code) => {
      const response = await apiClient.verifyMfa(challenge, method, code);
      if (response.data.session) {
        setAccessToken(response.data.session.accessToken);
        persistSessionState(response.data.session);
        setSession(response.data.session);
        const current = await apiClient.me();
        setMe(current.data);
      } else if (session) {
        const verifiedSession = { ...session, mfaVerified: true, requiresMfa: false, challenge: null };
        persistSessionState(verifiedSession);
        setSession(verifiedSession);
      }
      return response.data;
    },
    refresh: async () => {
      if (!session) throw new Error('No active session.');
      const response = await apiClient.refresh(session.accessToken);
      setAccessToken(response.data.accessToken);
      persistSessionState(response.data);
      setSession(response.data);
      if (!response.data.requiresMfa) {
        const current = await apiClient.me();
        setMe(current.data);
      }
      return response.data;
    },
    changePassword: async (currentPassword, newPassword) => {
      await apiClient.changePassword(currentPassword, newPassword);
      clearAccessToken();
      setSession(null);
      setMe(null);
    },
    listSessions: () => apiClient.listSessions(),
    revokeSession: async (sessionId, reason) => {
      await apiClient.revokeSession(sessionId, reason);
      if (session?.sessionId === sessionId) {
        clearAccessToken();
        setSession(null);
        setMe(null);
      }
    },
    enrollMfa: async () => (await apiClient.enrollMfa()).data,
    disableMfa: async (currentPassword, code) => { await apiClient.disableMfa(currentPassword, code); clearAccessToken(); setSession(null); setMe(null); },
    regenerateRecoveryCodes: async (currentPassword, code) => (await apiClient.regenerateRecoveryCodes(currentPassword, code)).data.codes,
    requestPasswordReset: async (email) => { await apiClient.requestPasswordReset(email); },
    completePasswordReset: async (token, newPassword) => { await apiClient.completePasswordReset(token, newPassword); },
    accessCatalog: async () => (await apiClient.accessCatalog()).data,
    accessUsers: (options) => apiClient.accessUsers(options),
    createAccessUser: async (input) => (await apiClient.createAccessUser(input)).data,
    changeAccessUserStatus: async (userId, status, reason, version) => { await apiClient.changeAccessUserStatus(userId, status, reason, version); },
    assignRole: async (userId, roleId, gymId, reason, version) => { await apiClient.assignRole(userId, roleId, gymId, reason, version); },
    revokeRole: async (userId, assignmentId, reason, version) => { await apiClient.revokeRole(userId, assignmentId, reason, version); },
    signOut: async () => {
      const current = session;
      try {
        if (current) await apiClient.logout(current.sessionId);
      } finally {
        clearAccessToken();
        setSession(null);
        setMe(null);
      }
    },
  }), [me, ready, session]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider.');
  return context;
}
