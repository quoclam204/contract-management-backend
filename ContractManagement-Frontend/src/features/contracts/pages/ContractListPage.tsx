import type { FC } from 'react';
import { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getContracts, submitContract } from '../services/contractApi';
import type { ContractDto } from '../types/contract.types';
import { ContractStatus, CONTRACT_STATUS_MAP } from '../types/contract.types';
import { ContractTable } from '../components/ContractTable';

interface ContractListPageProps {
  onSelectContract?: (contractId: string) => void;
}

export const ContractListPage: FC<ContractListPageProps> = ({ onSelectContract }) => {
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Fetch contracts using TanStack React Query
  const {
    data: contracts = [],
    isLoading,
    isError,
    error,
    refetch,
    isFetching,
  } = useQuery<ContractDto[]>({
    queryKey: ['contracts'],
    queryFn: getContracts,
  });

  // Mutation for submitting contract
  const submitMutation = useMutation({
    mutationFn: (contractId: string) => submitContract(contractId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      setNotification({
        type: 'success',
        message: `Đã gửi duyệt hợp đồng thành công! Tiến trình phê duyệt đã được khởi tạo.`,
      });
      setTimeout(() => setNotification(null), 5000);
    },
    onError: (err: Error) => {
      setNotification({
        type: 'error',
        message: `Lỗi khi gửi duyệt: ${err.message}`,
      });
      setTimeout(() => setNotification(null), 7000);
    },
  });

  // Filtered contracts
  const filteredContracts = useMemo(() => {
    return contracts.filter((c) => {
      const matchesSearch =
        searchTerm.trim() === '' ||
        c.contractNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
        c.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (c.contractTypeName && c.contractTypeName.toLowerCase().includes(searchTerm.toLowerCase()));

      const matchesStatus =
        statusFilter === 'all' || c.status.toString() === statusFilter;

      return matchesSearch && matchesStatus;
    });
  }, [contracts, searchTerm, statusFilter]);

  // Statistics
  const stats = useMemo(() => {
    return {
      total: contracts.length,
      draft: contracts.filter((c) => c.status === ContractStatus.Draft).length,
      pending: contracts.filter((c) => c.status === ContractStatus.PendingApproval).length,
      active: contracts.filter((c) => c.status === ContractStatus.Active).length,
    };
  }, [contracts]);

  const handleSubmit = (contract: ContractDto) => {
    submitMutation.mutate(contract.id);
  };

  return (
    <div className="p-6 max-w-7xl mx-auto w-full space-y-6">
      {/* Header section */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">
            Danh sách Hợp đồng
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Theo dõi vòng đời, trạng thái xử lý và quản lý phê duyệt hợp đồng trong hệ thống.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => refetch()}
            disabled={isFetching}
            className="inline-flex items-center gap-2 px-3.5 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg shadow-sm hover:bg-slate-50 active:bg-slate-100 disabled:opacity-50 transition-colors"
          >
            <svg
              className={`w-4 h-4 text-slate-500 ${isFetching ? 'animate-spin' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth="2"
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
              />
            </svg>
            <span>{isFetching ? 'Đang tải...' : 'Làm mới'}</span>
          </button>
        </div>
      </div>

      {/* Notification Toast/Banner */}
      {notification && (
        <div
          className={`p-4 rounded-xl border flex items-center justify-between shadow-sm transition-all animate-fadeIn ${
            notification.type === 'success'
              ? 'bg-emerald-50 text-emerald-900 border-emerald-200'
              : 'bg-rose-50 text-rose-900 border-rose-200'
          }`}
        >
          <div className="flex items-center gap-3">
            {notification.type === 'success' ? (
              <svg className="w-5 h-5 text-emerald-600 shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            ) : (
              <svg className="w-5 h-5 text-rose-600 shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
            )}
            <span className="text-sm font-medium">{notification.message}</span>
          </div>
          <button
            type="button"
            onClick={() => setNotification(null)}
            className="text-slate-400 hover:text-slate-600 p-1"
          >
            &times;
          </button>
        </div>
      )}

      {/* Metrics Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
            Tổng hợp đồng
          </div>
          <div className="mt-2 text-2xl font-bold text-slate-900">{stats.total}</div>
          <div className="text-xs text-slate-400 mt-1">Toàn bộ danh sách</div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
            Bản nháp (Draft)
          </div>
          <div className="mt-2 text-2xl font-bold text-slate-700">{stats.draft}</div>
          <div className="text-xs text-slate-400 mt-1">Chờ hoàn thiện để gửi duyệt</div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="text-xs font-semibold text-amber-600 uppercase tracking-wider">
            Chờ duyệt
          </div>
          <div className="mt-2 text-2xl font-bold text-amber-700">{stats.pending}</div>
          <div className="text-xs text-amber-600/70 mt-1">Đang trong quy trình Workflow</div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
          <div className="text-xs font-semibold text-emerald-600 uppercase tracking-wider">
            Đang hiệu lực
          </div>
          <div className="mt-2 text-2xl font-bold text-emerald-700">{stats.active}</div>
          <div className="text-xs text-emerald-600/70 mt-1">Hợp đồng đang thực thi</div>
        </div>
      </div>

      {/* Filter and Search Controls */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col sm:flex-row gap-3 items-center justify-between">
        {/* Search input */}
        <div className="relative w-full sm:w-80">
          <span className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </span>
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Tìm theo số HĐ, tiêu đề..."
            className="w-full pl-9 pr-3 py-2 text-sm bg-slate-50 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white transition-all text-slate-800 placeholder-slate-400"
          />
        </div>

        {/* Status filter dropdown */}
        <div className="flex items-center gap-2 w-full sm:w-auto">
          <span className="text-xs font-medium text-slate-500 whitespace-nowrap">Trạng thái:</span>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="w-full sm:w-48 py-2 px-3 text-sm bg-slate-50 border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:bg-white text-slate-800"
          >
            <option value="all">Tất cả trạng thái</option>
            {Object.entries(CONTRACT_STATUS_MAP).map(([statusKey, info]) => (
              <option key={statusKey} value={statusKey}>
                {info.label} ({statusKey})
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Content State Handling */}
      {isLoading ? (
        /* Loading Skeleton */
        <div className="bg-white rounded-xl border border-slate-200 p-6 space-y-4 shadow-sm animate-pulse">
          <div className="h-6 bg-slate-200 rounded w-1/4" />
          <div className="space-y-3">
            {[1, 2, 3, 4, 5].map((i) => (
              <div key={i} className="h-12 bg-slate-100 rounded-lg w-full" />
            ))}
          </div>
        </div>
      ) : isError ? (
        /* Error State */
        <div className="bg-rose-50 border border-rose-200 rounded-xl p-6 text-center space-y-3 shadow-sm">
          <div className="w-12 h-12 rounded-full bg-rose-100 text-rose-600 mx-auto flex items-center justify-center">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <h3 className="text-base font-semibold text-rose-900">Không thể kết nối đến máy chủ API</h3>
          <p className="text-sm text-rose-700 max-w-md mx-auto">
            {error instanceof Error ? error.message : 'Đã xảy ra lỗi không xác định khi tải danh sách hợp đồng.'}
          </p>
          <div className="pt-2">
            <button
              type="button"
              onClick={() => refetch()}
              className="inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold text-white bg-rose-600 hover:bg-rose-700 rounded-lg shadow-sm transition-colors"
            >
              Thử lại
            </button>
          </div>
        </div>
      ) : filteredContracts.length === 0 ? (
        /* Empty State */
        <div className="bg-white border border-slate-200 rounded-xl p-12 text-center space-y-3 shadow-sm">
          <div className="w-14 h-14 rounded-full bg-slate-100 text-slate-400 mx-auto flex items-center justify-center">
            <svg className="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <h3 className="text-base font-semibold text-slate-800">
            {contracts.length === 0 ? 'Chưa có hợp đồng nào' : 'Không tìm thấy kết quả phù hợp'}
          </h3>
          <p className="text-sm text-slate-500 max-w-sm mx-auto">
            {contracts.length === 0
              ? 'Hệ thống hiện tại chưa ghi nhận hợp đồng nào. Tạo hợp đồng mới để bắt đầu quy trình.'
              : 'Hãy thử tìm kiếm với từ khóa khác hoặc điều chỉnh bộ lọc trạng thái.'}
          </p>
          {contracts.length > 0 && (
            <div className="pt-2">
              <button
                type="button"
                onClick={() => {
                  setSearchTerm('');
                  setStatusFilter('all');
                }}
                className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 underline"
              >
                Xóa bộ lọc
              </button>
            </div>
          )}
        </div>
      ) : (
        /* Data Table */
        <ContractTable
          contracts={filteredContracts}
          onSubmitContract={handleSubmit}
          onSelectContract={(c) => onSelectContract?.(c.id)}
          submittingId={submitMutation.isPending ? submitMutation.variables ?? null : null}
        />
      )}
    </div>
  );
};
