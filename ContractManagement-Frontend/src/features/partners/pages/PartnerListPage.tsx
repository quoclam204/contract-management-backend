import React, { useEffect, useState } from 'react';
import { getPartners, deletePartner } from '../services/partnerApi';
import type { PartnerDto } from '../types/partner.types';
import { CreatePartnerModal } from '../components/CreatePartnerModal';
import { EditPartnerModal } from '../components/EditPartnerModal';

export const PartnerListPage: React.FC = () => {
  const [partners, setPartners] = useState<PartnerDto[]>([]);
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [isCreateOpen, setIsCreateOpen] = useState<boolean>(false);
  const [editingPartner, setEditingPartner] = useState<PartnerDto | null>(null);

  const fetchPartners = async (search?: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await getPartners({ searchTerm: search ?? searchTerm, pageSize: 100 });
      setPartners(res.items || []);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể tải danh sách đối tác');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchPartners();
  }, []);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchPartners(searchTerm);
  };

  const handleDelete = async (id: string, name: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa đối tác "${name}"?`)) return;
    try {
      await deletePartner(id);
      setPartners((prev) => prev.filter((p) => p.id !== id));
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : 'Không thể xóa đối tác');
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 m-0">Quản lý Đối tác</h1>
          <p className="text-xs text-gray-500 mt-1">
            Danh sách đối tác, khách hàng và nhà cung cấp ký kết hợp đồng
          </p>
        </div>
        <button
          onClick={() => setIsCreateOpen(true)}
          className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 shadow-sm"
        >
          + Thêm đối tác
        </button>
      </div>

      {/* Filter / Search Bar */}
      <form onSubmit={handleSearch} className="flex gap-2 max-w-md">
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder="Tìm theo tên, mã số thuế, đại diện..."
          className="flex-1 text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
        />
        <button
          type="submit"
          className="px-4 py-2 bg-gray-100 border border-gray-300 text-gray-700 rounded-lg text-sm font-medium hover:bg-gray-200"
        >
          Tìm kiếm
        </button>
      </form>

      {/* Content Table */}
      {isLoading ? (
        <div className="flex flex-col items-center justify-center min-h-[300px] space-y-3">
          <div className="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
          <p className="text-gray-500 text-sm">Đang tải danh sách đối tác...</p>
        </div>
      ) : error ? (
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm flex items-center justify-between">
          <span>{error}</span>
          <button
            onClick={() => fetchPartners()}
            className="px-3 py-1 bg-red-600 text-white rounded text-xs font-semibold hover:bg-red-700"
          >
            Thử lại
          </button>
        </div>
      ) : partners.length === 0 ? (
        <div className="text-center py-12 bg-white rounded-xl border border-gray-200">
          <span className="text-4xl">🏢</span>
          <h3 className="mt-2 text-sm font-medium text-gray-900">Không tìm thấy đối tác nào</h3>
          <p className="mt-1 text-xs text-gray-500">
            {searchTerm ? 'Thử tìm kiếm với từ khóa khác.' : 'Bắt đầu bằng cách thêm đối tác mới.'}
          </p>
          <div className="mt-4">
            <button
              onClick={() => setIsCreateOpen(true)}
              className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
            >
              + Thêm đối tác
            </button>
          </div>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-gray-50 border-b border-gray-200 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="py-3 px-4 font-semibold">Tên đối tác</th>
                  <th className="py-3 px-4 font-semibold">Mã số thuế</th>
                  <th className="py-3 px-4 font-semibold">Người đại diện</th>
                  <th className="py-3 px-4 font-semibold">Email</th>
                  <th className="py-3 px-4 font-semibold">Địa chỉ</th>
                  <th className="py-3 px-4 font-semibold text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {partners.map((p) => (
                  <tr key={p.id} className="hover:bg-gray-50">
                    <td className="py-3 px-4 font-medium text-gray-900">{p.name}</td>
                    <td className="py-3 px-4 text-gray-600 font-mono text-xs">{p.taxCode || '—'}</td>
                    <td className="py-3 px-4 text-gray-600">{p.representative || '—'}</td>
                    <td className="py-3 px-4 text-gray-600">{p.contactEmail || '—'}</td>
                    <td className="py-3 px-4 text-gray-500 text-xs max-w-xs truncate" title={p.address || ''}>
                      {p.address || '—'}
                    </td>
                    <td className="py-3 px-4 text-right space-x-2">
                      <button
                        onClick={() => setEditingPartner(p)}
                        className="text-xs font-semibold text-blue-600 hover:text-blue-800"
                      >
                        Sửa
                      </button>
                      <button
                        onClick={() => handleDelete(p.id, p.name)}
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

      {/* Modals */}
      <CreatePartnerModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onCreated={() => fetchPartners()}
      />

      <EditPartnerModal
        partner={editingPartner}
        isOpen={!!editingPartner}
        onClose={() => setEditingPartner(null)}
        onUpdated={() => fetchPartners()}
      />
    </div>
  );
};
