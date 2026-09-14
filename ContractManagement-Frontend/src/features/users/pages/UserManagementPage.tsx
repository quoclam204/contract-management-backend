import React, { useEffect, useState } from 'react';
import { getUsers, createUser, toggleUserActive } from '../services/userApi';
import { getDepartments } from '../../departments/services/departmentApi';
import type { UserDetailDto, CreateUserDto } from '../types/user.types';
import type { DepartmentDto } from '../../departments/types/department.types';
import { UserRole, UserRoleLabels } from '../../auth/types/auth.types';

export const UserManagementPage: React.FC = () => {
  const [users, setUsers] = useState<UserDetailDto[]>([]);
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [search, setSearch] = useState<string>('');
  const [roleFilter, setRoleFilter] = useState<string>('all');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [isCreateOpen, setIsCreateOpen] = useState<boolean>(false);
  const [createForm, setCreateForm] = useState<CreateUserDto>({
    fullName: '',
    email: '',
    password: '',
    role: UserRole.Staff,
    departmentId: null,
  });
  const [createError, setCreateError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  const fetchData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [userRes, deptRes] = await Promise.all([
        getUsers({
          pageSize: 100,
          search: search || undefined,
          role: roleFilter !== 'all' ? (Number(roleFilter) as UserRole) : undefined,
        }),
        getDepartments(),
      ]);
      setUsers(userRes.items || []);
      setDepartments(deptRes || []);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể tải danh sách người dùng');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [roleFilter]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchData();
  };

  const handleToggleActive = async (id: string, currentStatus: boolean) => {
    try {
      await toggleUserActive(id);
      setUsers((prev) =>
        prev.map((u) => (u.id === id ? { ...u, isActive: !currentStatus } : u))
      );
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : 'Không thể cập nhật trạng thái');
    }
  };

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setCreateError(null);
    try {
      await createUser(createForm);
      setIsCreateOpen(false);
      setCreateForm({
        fullName: '',
        email: '',
        password: '',
        role: UserRole.Staff,
        departmentId: null,
      });
      fetchData();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Không thể tạo người dùng');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 m-0">Quản lý Người dùng</h1>
          <p className="text-xs text-gray-500 mt-1">
            Quản lý tài khoản cán bộ, nhân viên, phân quyền vai trò và phòng ban trực thuộc
          </p>
        </div>
        <button
          onClick={() => setIsCreateOpen(true)}
          className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 shadow-sm"
        >
          + Thêm người dùng
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3 items-center justify-between">
        <form onSubmit={handleSearch} className="flex gap-2 w-full sm:w-auto max-w-md">
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm theo tên, email..."
            className="flex-1 text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
          />
          <button
            type="submit"
            className="px-4 py-2 bg-gray-100 border border-gray-300 text-gray-700 rounded-lg text-sm font-medium hover:bg-gray-200"
          >
            Tìm
          </button>
        </form>

        <div className="flex items-center gap-2 w-full sm:w-auto">
          <span className="text-xs text-gray-500 font-medium">Vai trò:</span>
          <select
            value={roleFilter}
            onChange={(e) => setRoleFilter(e.target.value)}
            className="text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500 bg-white"
          >
            <option value="all">Tất cả vai trò</option>
            {Object.entries(UserRoleLabels).map(([key, label]) => (
              <option key={key} value={key}>
                {label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="flex flex-col items-center justify-center min-h-[300px] space-y-3">
          <div className="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
          <p className="text-gray-500 text-sm">Đang tải danh sách người dùng...</p>
        </div>
      ) : error ? (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center justify-between">
          <span>{error}</span>
          <button
            onClick={() => fetchData()}
            className="px-3 py-1 bg-red-600 text-white rounded text-xs font-semibold hover:bg-red-700"
          >
            Thử lại
          </button>
        </div>
      ) : users.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-xl border border-gray-200">
          <span className="text-4xl">👥</span>
          <h3 className="mt-2 text-sm font-medium text-gray-900">Không tìm thấy người dùng nào</h3>
          <p className="mt-1 text-xs text-gray-500">Thử tìm kiếm với điều kiện lọc khác.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-50 border-b border-gray-200 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="py-3 px-4 font-semibold">Họ và tên</th>
                  <th className="py-3 px-4 font-semibold">Email</th>
                  <th className="py-3 px-4 font-semibold">Vai trò</th>
                  <th className="py-3 px-4 font-semibold">Phòng ban</th>
                  <th className="py-3 px-4 font-semibold text-center">Trạng thái</th>
                  <th className="py-3 px-4 font-semibold text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {users.map((u) => (
                  <tr key={u.id} className="hover:bg-gray-50">
                    <td className="py-3 px-4 font-medium text-gray-900">{u.fullName}</td>
                    <td className="py-3 px-4 text-gray-600">{u.email}</td>
                    <td className="py-3 px-4">
                      <span className="text-xs px-2.5 py-0.5 rounded-full bg-blue-50 text-blue-700 border border-blue-200 font-medium">
                        {u.roleName || UserRoleLabels[u.role] || `Role #${u.role}`}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-gray-600">
                      {u.departmentName || '—'}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <span
                        className={`text-xs px-2 py-0.5 rounded font-semibold ${
                          u.isActive
                            ? 'bg-green-100 text-green-800'
                            : 'bg-gray-100 text-gray-500'
                        }`}
                      >
                        {u.isActive ? 'Hoạt động' : 'Tạm khóa'}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right">
                      <button
                        onClick={() => handleToggleActive(u.id, u.isActive)}
                        className={`text-xs font-semibold ${
                          u.isActive
                            ? 'text-amber-600 hover:text-amber-800'
                            : 'text-green-600 hover:text-green-800'
                        }`}
                      >
                        {u.isActive ? 'Khóa' : 'Kích hoạt'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Create Modal */}
      {isCreateOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
          <div className="relative w-full max-w-md rounded-xl bg-white shadow-xl overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
              <h2 className="text-lg font-bold text-gray-900 m-0">Thêm người dùng mới</h2>
              <button
                type="button"
                onClick={() => setIsCreateOpen(false)}
                className="text-gray-400 hover:text-gray-600 text-2xl font-bold"
              >
                &times;
              </button>
            </div>

            <form onSubmit={handleCreateSubmit} className="p-6 space-y-4">
              {createError && (
                <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-xs text-red-600">
                  {createError}
                </div>
              )}

              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Họ và tên <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={createForm.fullName}
                  onChange={(e) => setCreateForm({ ...createForm, fullName: e.target.value })}
                  placeholder="VD: Nguyễn Văn A"
                  className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Email đăng nhập <span className="text-red-500">*</span>
                </label>
                <input
                  type="email"
                  required
                  value={createForm.email}
                  onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                  placeholder="VD: user@contractflow.com"
                  className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Mật khẩu <span className="text-red-500">*</span>
                </label>
                <input
                  type="password"
                  required
                  value={createForm.password}
                  onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
                  placeholder="Tối thiểu 6 ký tự"
                  className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                    Vai trò <span className="text-red-500">*</span>
                  </label>
                  <select
                    value={createForm.role}
                    onChange={(e) => setCreateForm({ ...createForm, role: Number(e.target.value) as UserRole })}
                    className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 bg-white focus:ring-1 focus:ring-blue-500"
                  >
                    {Object.entries(UserRoleLabels).map(([key, label]) => (
                      <option key={key} value={key}>
                        {label}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                    Phòng ban
                  </label>
                  <select
                    value={createForm.departmentId || ''}
                    onChange={(e) =>
                      setCreateForm({
                        ...createForm,
                        departmentId: e.target.value ? e.target.value : null,
                      })
                    }
                    className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 bg-white focus:ring-1 focus:ring-blue-500"
                  >
                    <option value="">-- Không chọn --</option>
                    {departments.map((d) => (
                      <option key={d.id} value={d.id}>
                        {d.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="flex items-center justify-end space-x-3 pt-4 border-t border-gray-100">
                <button
                  type="button"
                  onClick={() => setIsCreateOpen(false)}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                >
                  {isSubmitting ? 'Đang tạo...' : 'Tạo người dùng'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
