import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import {
  clearSession,
  getStoredUser,
  getToken,
  setSession,
  type AuthUser,
  api,
} from "../lib/api";

type AuthState = {
  user: AuthUser | null;
  token: string | null;
  login: (email: string, password: string) => Promise<AuthUser>;
  register: (input: {
    companyName: string;
    fullName: string;
    email: string;
    password: string;
    phone?: string;
  }) => Promise<AuthUser>;
  logout: () => void;
};

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => getStoredUser());
  const [token, setToken] = useState<string | null>(() => getToken());

  const value = useMemo<AuthState>(
    () => ({
      user,
      token,
      async login(email, password) {
        const res = await api.login({ email, password });
        setSession(res.token, res.user);
        setToken(res.token);
        setUser(res.user);
        return res.user;
      },
      async register(input) {
        const res = await api.register(input);
        setSession(res.token, res.user);
        setToken(res.token);
        setUser(res.user);
        return res.user;
      },
      logout() {
        clearSession();
        setToken(null);
        setUser(null);
      },
    }),
    [user, token],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth outside provider");
  return ctx;
}

export function homeForRole(role: string) {
  if (role === "PlatformAdmin") return "/admin";
  if (role === "Guard") return "/guard";
  return "/app";
}
