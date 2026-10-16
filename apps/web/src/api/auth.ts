import { useSyncExternalStore } from 'react';
import { request } from './client';

export interface SignedInAgent { id: number; fullName: string; email: string; }
export interface LoginResult { token: string; expiresAtUtc: string; user: SignedInAgent; }

const KEY = 'northwind-support-session';
const CHANGE = 'northwind-auth-changed';

const subscribe = (notify: () => void) => {
  window.addEventListener(CHANGE, notify);
  window.addEventListener('storage', notify);
  return () => { window.removeEventListener(CHANGE, notify); window.removeEventListener('storage', notify); };
};

let lastRaw: string | null | undefined;
let lastSession: LoginResult | null = null;
function readSession(): LoginResult | null {
  const raw = localStorage.getItem(KEY);
  if (raw === lastRaw) return lastSession;
  lastRaw = raw;
  try {
    const parsed = raw ? JSON.parse(raw) as LoginResult : null;
    lastSession = parsed?.token && parsed?.user && Date.parse(parsed.expiresAtUtc) > Date.now() ? parsed : null;
  } catch { lastSession = null; }
  return lastSession;
}
function notify() { lastRaw = undefined; window.dispatchEvent(new Event(CHANGE)); }

export const authStore = {
  get: readSession,
  getToken: () => readSession()?.token,
  set: (session: LoginResult) => { localStorage.setItem(KEY, JSON.stringify(session)); notify(); },
  clear: () => { localStorage.removeItem(KEY); notify(); },
};

export const authApi = {
  login: (email: string, password: string) => request<LoginResult>('/auth/login', {
    method: 'POST', body: JSON.stringify({ email, password }),
  }),
  me: () => request<SignedInAgent>('/auth/me'),
};

export function useAuth() { return useSyncExternalStore(subscribe, readSession); }
