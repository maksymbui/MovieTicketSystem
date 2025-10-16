export type Session = { token: string; userId: string; email: string; displayName: string; role: string };

const key = "mts_session";

export function saveSession(s: Session) { localStorage.setItem(key, JSON.stringify(s)); }
export function loadSession(): Session | null {
  try { const raw = localStorage.getItem(key); return raw ? JSON.parse(raw) as Session : null; } catch { return null; }
}
export function clearSession() { localStorage.removeItem(key); }