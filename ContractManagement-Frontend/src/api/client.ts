/**
 * Centralized HTTP/API client for Contract Management System.
 * Handles base URLs, JWT token injection, X-User-Id fallback, JSON parsing, and error formatting.
 */

export interface ApiError extends Error {
  status?: number;
  data?: unknown;
}

export const AUTH_TOKEN_KEY = 'jwt_token';
export const AUTH_USER_KEY = 'auth_user';

export function getStoredToken(): string | null {
  return localStorage.getItem(AUTH_TOKEN_KEY) || localStorage.getItem('token');
}

export function setStoredToken(token: string | null): void {
  if (token) {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
    localStorage.setItem('token', token);
  } else {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem('token');
  }
}

export function getStoredUser(): { id: string; email: string; fullName: string; role: number | string } | null {
  const raw = localStorage.getItem(AUTH_USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function setStoredUser(user: unknown): void {
  if (user) {
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(user));
  } else {
    localStorage.removeItem(AUTH_USER_KEY);
  }
}

interface RequestOptions extends RequestInit {
  params?: Record<string, string | number | boolean | undefined | null>;
}

async function request<T>(url: string, options: RequestOptions = {}): Promise<T> {
  const token = getStoredToken();
  const user = getStoredUser();

  const headers = new Headers(options.headers || {});
  headers.set('Accept', 'application/json');

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  if (user?.id) {
    headers.set('X-User-Id', user.id);
  }

  // Handle URL query params
  let targetUrl = url;
  if (options.params) {
    const searchParams = new URLSearchParams();
    for (const [key, value] of Object.entries(options.params)) {
      if (value !== undefined && value !== null && value !== '') {
        searchParams.append(key, String(value));
      }
    }
    const queryString = searchParams.toString();
    if (queryString) {
      targetUrl += (targetUrl.includes('?') ? '&' : '?') + queryString;
    }
  }

  const response = await fetch(targetUrl, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    window.dispatchEvent(new CustomEvent('auth:unauthorized'));
  }

  if (!response.ok) {
    let errorMessage = `Yêu cầu thất bại (${response.status} ${response.statusText})`;
    let errorData: unknown = null;

    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
      try {
        errorData = await response.json();
        if (typeof errorData === 'object' && errorData !== null) {
          const obj = errorData as Record<string, unknown>;
          if (typeof obj.error === 'string') errorMessage = obj.error;
          else if (typeof obj.message === 'string') errorMessage = obj.message;
          else if (typeof obj.title === 'string') errorMessage = obj.title;
        }
      } catch {
        // ignore json parse error
      }
    } else {
      try {
        const text = await response.text();
        if (text) errorMessage = text;
      } catch {
        // ignore text parse error
      }
    }

    const err: ApiError = new Error(errorMessage);
    err.status = response.status;
    err.data = errorData;
    throw err;
  }

  if (response.status === 204) {
    return undefined as unknown as T;
  }

  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    return response.json();
  }

  return response.text() as unknown as T;
}

export async function apiGet<T>(url: string, params?: Record<string, string | number | boolean | undefined | null>): Promise<T> {
  return request<T>(url, { method: 'GET', params });
}

export async function apiPost<T>(url: string, data?: unknown, options?: RequestOptions): Promise<T> {
  const isFormData = typeof FormData !== 'undefined' && data instanceof FormData;
  return request<T>(url, {
    ...options,
    method: 'POST',
    headers: isFormData ? options?.headers : { 'Content-Type': 'application/json', ...options?.headers },
    body: isFormData ? (data as FormData) : data !== undefined ? JSON.stringify(data) : undefined,
  });
}

export async function apiPut<T>(url: string, data?: unknown, options?: RequestOptions): Promise<T> {
  return request<T>(url, {
    ...options,
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    body: data !== undefined ? JSON.stringify(data) : undefined,
  });
}

export async function apiPatch<T>(url: string, data?: unknown, options?: RequestOptions): Promise<T> {
  return request<T>(url, {
    ...options,
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    body: data !== undefined ? JSON.stringify(data) : undefined,
  });
}

export async function apiDelete<T = void>(url: string, options?: RequestOptions): Promise<T> {
  return request<T>(url, {
    ...options,
    method: 'DELETE',
  });
}

export async function apiUpload<T>(url: string, formData: FormData): Promise<T> {
  return request<T>(url, {
    method: 'POST',
    body: formData,
  });
}
