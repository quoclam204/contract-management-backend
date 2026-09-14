import type { UserRole } from '../../auth/types/auth.types';
import type { PagedResult } from '../../partners/types/partner.types';

export interface UserDetailDto {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  roleName: string;
  departmentId?: string | null;
  departmentName?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface UserFilterDto {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  role?: UserRole;
  departmentId?: string;
  isActive?: boolean;
}

export interface CreateUserDto {
  fullName: string;
  email: string;
  password: string;
  role: UserRole;
  departmentId?: string | null;
}

export interface UpdateUserDto {
  fullName: string;
  email: string;
  role: UserRole;
  departmentId?: string | null;
  isActive?: boolean;
}

export type { PagedResult };
