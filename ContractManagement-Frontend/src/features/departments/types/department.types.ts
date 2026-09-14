export interface DepartmentDto {
  id: string;
  name: string;
  managerId?: string | null;
  createdAt: string;
}

export interface CreateDepartmentDto {
  name: string;
  managerId?: string | null;
}

export interface UpdateDepartmentDto {
  name: string;
  managerId?: string | null;
}
