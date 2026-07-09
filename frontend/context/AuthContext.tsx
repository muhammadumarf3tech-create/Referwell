'use client';

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { useRouter } from 'next/navigation';

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'TriageNurse' | 'GP';
  token: string;
}

interface AuthContextType {
  user: AuthUser | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

function parseJwt(token: string) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(window.atob(base64));
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const stored = localStorage.getItem('referwell_token');
    const storedUser = localStorage.getItem('referwell_user');
    if (stored && storedUser) {
      const parsed = parseJwt(stored);
      if (parsed && parsed.exp * 1000 > Date.now()) {
        setUser(JSON.parse(storedUser));
      } else {
        localStorage.removeItem('referwell_token');
        localStorage.removeItem('referwell_user');
      }
    }
    setIsLoading(false);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.message || 'Login failed');
    }

    const data = await res.json();
    const authUser: AuthUser = {
      id: parseJwt(data.token)?.sub || '',
      fullName: data.fullName,
      email: data.email,
      role: data.role,
      token: data.token,
    };

    localStorage.setItem('referwell_token', data.token);
    localStorage.setItem('referwell_user', JSON.stringify(authUser));
    setUser(authUser);
    router.push('/dashboard');
  }, [router]);

  const logout = useCallback(() => {
    localStorage.removeItem('referwell_token');
    localStorage.removeItem('referwell_user');
    setUser(null);
    router.push('/login');
  }, [router]);

  return (
    <AuthContext.Provider value={{ user, login, logout, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
