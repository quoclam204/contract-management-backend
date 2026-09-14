import React, { useEffect, useState } from 'react';
import { getDepartments, createDepartment, deleteDepartment } from '../services/departmentApi';
import type { DepartmentDto, CreateDepartmentDto } from '../types/department.types';

export const DepartmentListPage: React.FC = () => {
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [isCreateOpen, setIsCreateOpen] = useState<boolean>(false);
  const [createForm, setCreateForm] = useState<CreateDepartmentDto>({
    name: '',
  });
  const [createError, setCreateError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  const fetchDepartments = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await getDepartments();
      setDepartments(data);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể tải danh sách phòng ban');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDepartments();
  }, []);

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!createForm.name.trim()) {
      setCreateError('Tên phòng ban không được để trống');
      return;
    }

    setIsSubmitting(true);
    setCreateError(null);
    try {
      await createDepartment({ name: createForm.name.trim() });
      setIsCreateOpen(false);
      setCreateForm({ name: '' });
      fetchDepartments();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Không thể tạo phòng ban');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (id: string, name: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa phòng ban "${name}"?`)) return;
    try {
      await deleteDepartment(id);
      setDepartments((prev) => prev.filter((d) => d.id !== id));
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : 'Không thể xóa phòng ban');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 m-0">Quản lý Phòng ban</h1>
          <p className="text-xs text-gray-500 mt-1">
            Cơ cấu tổ chức các phòng ban và đơn vị trực thuộc quản lý hợp đồng
          </p>
        </div>
        <button
          onClick={() => setIsCreateOpen(true)}
          className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 shadow-sm"
        >
          + Thêm phòng ban
        </button>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="flex flex-col items-center justify-center min-h-[300px] space-y-3">
          <div className="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
          <p className="text-gray-500 text-sm">Đang tải danh sách phòng ban...</p>
        </div>
      ) : error ? (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center justify-between">
          <span>{error}</span>
          <button
            onClick={() => fetchDepartments()}
            className="px-3 py-1 bg-red-600 text-white rounded text-xs font-semibold hover:bg-red-700"
          >
            Thử lại
          </button>
        </div>
      ) : departments.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-xl border border-gray-200">
          <span className="text-4xl">🏛️</span>
          <h3 className="mt-2 text-sm font-medium text-gray-900">Chưa có phòng ban nào</h3>
          <p className="mt-1 text-xs text-gray-500">Bắt đầu bằng cách tạo phòng ban đầu tiên.</p>
          <div className="mt-4">
            <button
              onClick={() => setIsCreateOpen(true)}
              className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
            >
              + Thêm phòng ban
            </button>
          </div>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-50 border-b border-gray-200 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="py-3 px-4 font-semibold">Tên phòng ban</th>
                  <th className="py-3 px-4 font-semibold">Mã phòng ban</th>
                  <th className="py-3 px-4 font-semibold">Ngày tạo</th>
                  <th className="py-3 px-4 font-semibold text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {departments.map((d) => (
                  <tr key={d.id} className="hover:bg-gray-50">
                    <td className="py-3 px-4 font-medium text-gray-900">{d.name}</td>
                    <td className="py-3 px-4 text-gray-600 font-mono text-xs">{d.id}</td>
                    <td className="py-3 px-4 text-gray-600 text-xs">
                      {new Date(d.createdAt).toLocaleDateString('vi-VN')}
                    </td>
                    <td className="py-3 px-4 text-right">
                      <button
                        onClick={() => handleDelete(d.id, d.name)}
                        className="text-xs font-semibold text-red-600 hover:text-red-800"
                      >
                        Xóa
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
              <h2 className="text-lg font-bold text-gray-900 m-0">Thêm phòng ban mới</h2>
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
                  Tên phòng ban <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={createForm.name}
                  onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })}
                  placeholder="VD: Phòng Kế hoạch Tài chính, Ban Pháp chế..."
                  className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500"
                />
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
                  {isSubmitting ? 'Đang tạo...' : 'Tạo phòng ban'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
