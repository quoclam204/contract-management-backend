import { apiGet, apiPost, apiPut, apiDelete } from '../../../api/client';
import type {
  DepartmentDto,
  CreateDepartmentDto,
  UpdateDepartmentDto,
} from '../types/department.types';

export async function getDepartments(): Promise<DepartmentDto[]> {
  try {
    return await apiGet<DepartmentDto[]>('/api/departments');
  } catch {
    return [];
  }
}

export async function getDepartmentById(id: string): Promise<DepartmentDto> {
  return apiGet<DepartmentDto>(`/api/departments/${id}`);
}

export async function createDepartment(data: CreateDepartmentDto): Promise<DepartmentDto> {
  return apiPost<DepartmentDto>('/api/departments', data);
}

export async function updateDepartment(id: string, data: UpdateDepartmentDto): Promise<DepartmentDto> {
  return apiPut<DepartmentDto>(`/api/departments/${id}`, data);
}

export async function deleteDepartment(id: string): Promise<void> {
  return apiDelete<void>(`/api/departments/${id}`);
}
