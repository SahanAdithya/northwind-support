import { afterEach, describe, expect, it, vi } from 'vitest';
import { authStore } from './auth';
import { request } from './client';

describe('API authentication', () => {
  afterEach(() => { authStore.clear(); vi.unstubAllGlobals(); });

  it('attaches the signed-in JWT on requests', async () => {
    authStore.set({ token: 'fake-local-token', expiresAtUtc: '2099-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex', email: 'alex@example.com' } });
    const fetch = vi.fn().mockResolvedValue({ ok: true, status: 200, json: async () => ({}) });
    vi.stubGlobal('fetch', fetch);
    await request('/tickets');
    expect(fetch).toHaveBeenCalledWith(expect.any(String), expect.objectContaining({
      headers: expect.objectContaining({ Authorization: 'Bearer fake-local-token' }),
    }));
  });

  it('clears the session on an expired-token 401', async () => {
    authStore.set({ token: 'expired-token', expiresAtUtc: '2099-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex', email: 'alex@example.com' } });
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 401,
      statusText: 'Unauthorized', json: async () => ({ title: 'Unauthorized' }) }));
    await expect(request('/tickets')).rejects.toThrow();
    expect(authStore.get()).toBeNull();
  });
});
