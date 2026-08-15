import { createContext } from "react";
import type { AuthResponse, AuthUser } from "../types/auth";

export interface AuthContextValue {
  user: AuthUser | null;
  login: (response: AuthResponse) => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
