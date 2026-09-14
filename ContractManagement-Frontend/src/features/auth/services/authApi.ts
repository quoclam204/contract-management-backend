import { apiGet, apiPost, setStoredToken, setStoredUser } from '../../../api/client';
import type { LoginRequestDto, LoginResponseDto, RegisterUserDto, UserDto } from '../types/auth.types';

export async function login(credentials: LoginRequestDto): Promise<LoginResponseDto> {
  const response = await apiPost<LoginResponseDto>('/api/auth/login', credentials);
  setStoredToken(response.token);
  setStoredUser(response.user);
  return response;
}

export async function register(data: RegisterUserDto): Promise<UserDto> {
  return apiPost<UserDto>('/api/auth/register', data);
}

export async function getCurrentUser(): Promise<UserDto> {
  return apiGet<UserDto>('/api/auth/me');
}

export function logout(): void {
  setStoredToken(null);
  setStoredUser(null);
}
