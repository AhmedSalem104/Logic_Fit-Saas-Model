import type { HealthResponse, ReadinessResponse } from '@logicfit/shared-contracts';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:5199/api/v1').replace(/\/$/, '');

export class ApiClientError extends Error {
  constructor(public readonly status: number, message: string, public readonly code?: string) {
    super(message);
    this.name = 'ApiClientError';
  }
}

async function request<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { Accept: 'application/json', 'X-Request-Id': crypto.randomUUID() },
    credentials: 'include',
  });
  const body = await response.json().catch(() => undefined);
  if (!response.ok) throw new ApiClientError(response.status, body?.error?.message ?? 'تعذر الاتصال بالخادم.', body?.error?.code);
  return body as T;
}

export const apiClient = {
  baseUrl: API_BASE_URL,
  health: () => request<HealthResponse>('/health'),
  readiness: () => request<ReadinessResponse>('/readiness'),
};
