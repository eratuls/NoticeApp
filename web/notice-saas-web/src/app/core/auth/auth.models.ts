export interface LoginUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  organizationId: string;
  organizationName: string;
}

export interface LoginSuccess {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  sessionExpiresAtUtc: string;
  sessionId: string;
  user: LoginUser;
}

export interface ActiveSessionInfo {
  ipAddress: string | null;
  loggedInAtUtc: string;
  lastActivityAtUtc: string;
  expiresAtUtc: string;
}

export interface SessionConflict {
  code: string;
  message: string;
  session: ActiveSessionInfo;
}

export interface SessionStatus {
  sessionId: string;
  expiresAtUtc: string;
  remainingSeconds: number;
  user: LoginUser;
}

const TOKEN_KEY = 'noticesaas.accessToken';
const SESSION_KEY = 'noticesaas.sessionExpiresAt';
const USER_KEY = 'noticesaas.user';

export function storeAuth(success: LoginSuccess): void {
  localStorage.setItem(TOKEN_KEY, success.accessToken);
  localStorage.setItem(SESSION_KEY, success.sessionExpiresAtUtc);
  localStorage.setItem(USER_KEY, JSON.stringify(success.user));
}

export function clearAuth(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(SESSION_KEY);
  localStorage.removeItem(USER_KEY);
}

export function readToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function readSessionExpiresAt(): string | null {
  return localStorage.getItem(SESSION_KEY);
}

export function readUser(): LoginUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as LoginUser;
  } catch {
    return null;
  }
}

export function writeSessionExpiresAt(value: string): void {
  localStorage.setItem(SESSION_KEY, value);
}
