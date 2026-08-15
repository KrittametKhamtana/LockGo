import { useMemo, useState, type ReactNode } from "react";
import { clearStoredAuth, getStoredAuth, setStoredAuth } from "../lib/authStorage";
import type { AuthUser } from "../types/auth";
import { AuthContext, type AuthContextValue } from "./AuthContextValue";

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => getStoredAuth()?.user ?? null);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      login: (response) => setUser(setStoredAuth(response).user),
      logout: () => {
        clearStoredAuth();
        setUser(null);
      },
    }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
