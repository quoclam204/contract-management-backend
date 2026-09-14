export const UserRole = {
  Admin: 0,
  Manager: 1,
  Staff: 2,
  Approver: 3,
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const UserRoleLabels: Record<number, string> = {
  0: 'Admin',
  1: 'Trưởng phòng (Manager)',
  2: 'Nhân viên (Staff)',
  3: 'Người duyệt (Approver)',
};

export interface UserDto {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  roleName: string;
  departmentId?: string | null;
  departmentName?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface LoginResponseDto {
  token: string;
  user: UserDto;
}

export interface RegisterUserDto {
  fullName: string;
  email: string;
  password: string;
  departmentId?: string | null;
  role?: UserRole;
}
