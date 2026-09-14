import { apiGet, apiPost, apiPut, apiPatch } from '../../../api/client';
import type {
  UserDetailDto,
  UserFilterDto,
  CreateUserDto,
  UpdateUserDto,
  PagedResult,
} from '../types/user.types';

export async function getUsers(filter?: UserFilterDto): Promise<PagedResult<UserDetailDto>> {
  return apiGet<PagedResult<UserDetailDto>>('/api/users', {
    pageNumber: filter?.pageNumber ?? 1,
    pageSize: filter?.pageSize ?? 20,
    search: filter?.search,
    role: filter?.role,
    departmentId: filter?.departmentId,
    isActive: filter?.isActive,
  });
}

export async function getUserById(id: string): Promise<UserDetailDto> {
  return apiGet<UserDetailDto>(`/api/users/${id}`);
}

export async function createUser(data: CreateUserDto): Promise<UserDetailDto> {
  return apiPost<UserDetailDto>('/api/users', data);
}

export async function updateUser(id: string, data: UpdateUserDto): Promise<UserDetailDto> {
  return apiPut<UserDetailDto>(`/api/users/${id}`, data);
}

export async function toggleUserActive(id: string): Promise<{ message: string }> {
  return apiPatch<{ message: string }>(`/api/users/${id}/toggle-active`);
}

export async function getUsersByDepartment(departmentId: string): Promise<UserDetailDto[]> {
  return apiGet<UserDetailDto[]>(`/api/users/by-department/${departmentId}`);
}
