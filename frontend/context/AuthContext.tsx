'use client';

import { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { authApi } from '@/lib/api';
import { storeUser, clearUser, getStoredUser, isTokenExpired } from '@/lib/auth';
import type { AuthUser, LoginRequest, Rol } from '@/lib/types';

interface AuthContextValue {
  user: AuthUser | null;
  loading: boolean;
  login: (dto: LoginRequest) => Promise<void>;
  logout: () => void;
  hasRole: (...roles: Rol[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const stored = getStoredUser();
    if (stored && !isTokenExpired(stored)) {
      setUser(stored);
    } else {
      clearUser();
    }
    setLoading(false);
  }, []);

  const login = useCallback(async (dto: LoginRequest) => {
    const data = await authApi.login(dto);
    storeUser(data);
    setUser(data);
    router.push(data.rol === 'Dueno' ? '/mi-cuenta' : '/dashboard');
  }, [router]);

  const logout = useCallback(() => {
    clearUser();
    setUser(null);
    router.push('/login');
  }, [router]);

  const hasRole = useCallback((...roles: Rol[]) => {
    return user ? roles.includes(user.rol) : false;
  }, [user]);

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, hasRole }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
