import { useState, useEffect, useCallback } from 'react';
import { getStoredToken, getStoredUser, setStoredToken, setStoredUser } from '../../../api/client';
import type { UserDto, LoginRequestDto } from '../types/auth.types';
import { login as loginService, logout as logoutService, getCurrentUser } from '../services/authApi';

export function useAuth() {
  const [user, setUser] = useState<UserDto | null>(() => getStoredUser() as UserDto | null);
  const [token, setToken] = useState<string | null>(() => getStoredToken());
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Sync state if another tab or client event changes auth
  useEffect(() => {
    const handleUnauthorized = () => {
      setStoredToken(null);
      setStoredUser(null);
      setToken(null);
      setUser(null);
    };

    window.addEventListener('auth:unauthorized', handleUnauthorized);
    return () => window.removeEventListener('auth:unauthorized', handleUnauthorized);
  }, []);

  // Fetch /api/auth/me on mount if we have a token to ensure token is valid and data is fresh
  useEffect(() => {
    if (token && !user) {
      getCurrentUser()
        .then((freshUser) => {
          setUser(freshUser);
          setStoredUser(freshUser);
        })
        .catch(() => {
          // Token might be invalid
          logoutService();
          setToken(null);
          setUser(null);
        });
    }
  }, [token, user]);

  const handleLogin = useCallback(async (credentials: LoginRequestDto) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await loginService(credentials);
      setToken(response.token);
      setUser(response.user);
      return response;
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Đăng nhập không thành công';
      setError(msg);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const handleLogout = useCallback(() => {
    logoutService();
    setToken(null);
    setUser(null);
  }, []);

  return {
    user,
    token,
    isAuthenticated: Boolean(token),
    isLoading,
    error,
    login: handleLogin,
    logout: handleLogout,
  };
}
