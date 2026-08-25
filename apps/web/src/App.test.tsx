import { render, screen } from '@testing-library/react';
import { beforeEach, expect, test, vi } from 'vitest';
import App from './App';

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);
    const body = url.endsWith('/health')
      ? { data: { status: 'ok', service: 'logicfit-api', environment: 'test', version: '0.1.0' }, meta: { requestId: 'test', version: 'v1' } }
      : { data: { status: 'ready', service: 'logicfit-api', database: 'not_configured' }, meta: { requestId: 'test', version: 'v1' } };
    return new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } });
  }));
  localStorage.clear();
});

test('renders the RTL foundation shell and health state', async () => {
  render(<App />);
  expect(await screen.findByText('الأساس التقني المحلي')).toBeInTheDocument();
  expect(document.documentElement.dir).toBe('rtl');
  expect(screen.getByText('API يعمل')).toBeInTheDocument();
});
