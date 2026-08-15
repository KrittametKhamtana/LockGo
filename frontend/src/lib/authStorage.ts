import type { AuthResponse, AuthUser } from "../types/auth";

const STORAGE_KEY = "lockgo.auth";

interface StoredAuth {
  token: string;
  user: AuthUser;
}

/**
 * Single source of truth for the persisted session, read by both AuthContext
 * (React state) and the axios interceptor (outside any component) — keeping
 * them as two independent readers of the same storage avoids a circular
 * import between the two.
 */
export function getStoredAuth(): StoredAuth | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as StoredAuth;
  } catch {
    return null;
  }
}

export function setStoredAuth(response: AuthResponse): StoredAuth {
  const stored: StoredAuth = {
    token: response.token,
    user: {
      userId: response.userId,
      firstName: response.firstName,
      lastName: response.lastName,
      email: response.email,
      username: response.username,
    },
  };
  localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  return stored;
}

export function clearStoredAuth(): void {
  localStorage.removeItem(STORAGE_KEY);
}

export function getToken(): string | null {
  return getStoredAuth()?.token ?? null;
}
