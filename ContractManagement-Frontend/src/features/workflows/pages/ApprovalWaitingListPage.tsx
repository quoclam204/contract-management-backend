import type { FC } from 'react';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { PendingApprovalItemDto, ProcessApprovalDecisionRequest } from '../types/workflow.types';
import { ApprovalDecision, APPROVER_ROLE_MAP } from '../types/workflow.types';
import { getPendingApprovals, processApprovalDecision } from '../services/workflowApi';
import { getContractById } from '../../contracts/services/contractApi';
import { RejectDecisionModal } from '../components/RejectDecisionModal';

interface PendingApprovalCardProps {
  item: PendingApprovalItemDto;
  onApprove: (item: PendingApprovalItemDto) => void;
  onReject: (item: PendingApprovalItemDto) => void;
  isProcessing: boolean;
  onViewContractDetail?: (contractId: string) => void;
}

const PendingApprovalCard: FC<PendingApprovalCardProps> = ({
  item,
  onApprove,
  onReject,
  isProcessing,
  onViewContractDetail,
}) => {
  // Load contract info in parallel
  const { data: contract, isLoading: isContractLoading } = useQuery({
    queryKey: ['contract', item.contractId],
    queryFn: () => getContractById(item.contractId),
    staleTime: 30000,
  });

  const roleInfo = APPROVER_ROLE_MAP[item.approverRole];

  const formatCurrency = (val?: number) => {
    if (val === undefined || val === null) return '0 ₫';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(val);
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return '—';
    try {
      return new Date(dateString).toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateString;
    }
  };

  return (
    <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-xs hover:border-indigo-200 hover:shadow-md transition-all space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
        <div className="space-y-1">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-xs font-mono font-bold px-2.5 py-1 bg-indigo-50 text-indigo-700 rounded-lg border border-indigo-200">
              Bước #{item.stepOrder}
            </span>
            <span className={`text-xs px-2.5 py-1 rounded-lg border font-semibold ${roleInfo?.badgeClass}`}>
              Vai trò: {roleInfo?.label || `Role #${item.approverRole}`}
            </span>
            <span className="text-xs px-2.5 py-1 rounded-lg bg-amber-50 text-amber-700 border border-amber-200 font-semibold flex items-center gap-1">
              <span className="w-1.5 h-1.5 rounded-full bg-amber-500 animate-pulse" />
              Đang chờ bạn duyệt
            </span>
          </div>

          <h3 className="text-base font-bold text-slate-900 pt-1">
            {isContractLoading ? (
              <span className="inline-block h-5 bg-slate-200 rounded w-48 animate-pulse" />
            ) : contract ? (
              contract.title
            ) : (
              `Hợp đồng ID: ${item.contractId}`
            )}
          </h3>

          <div className="flex items-center gap-3 text-xs text-slate-500 flex-wrap">
            {contract && (
              <span className="font-mono font-semibold text-slate-700">
                Số HĐ: {contract.contractNumber}
              </span>
            )}
            <span>Luồng duyệt: <strong className="text-slate-800">{item.workflowName}</strong></span>
            <span>Gửi lúc: {formatDate(item.createdAt)}</span>
          </div>
        </div>

        {/* Contract Value Highlight */}
        {contract && (
          <div className="bg-slate-50 border border-slate-100 rounded-xl p-3 sm:text-right shrink-0">
            <span className="block text-[11px] text-slate-400 font-medium">Giá trị hợp đồng</span>
            <span className="block text-base font-bold text-indigo-700 mt-0.5">
              {formatCurrency(contract.value)}
            </span>
          </div>
        )}
      </div>

      {/* Contract metadata brief */}
      {contract && (
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 p-3 bg-slate-50/70 rounded-xl text-xs border border-slate-100">
          <div>
            <span className="text-slate-400 block text-[11px]">Loại hợp đồng</span>
            <span className="font-semibold text-slate-700">{contract.contractTypeName || 'Hợp đồng dịch vụ'}</span>
          </div>
          <div>
            <span className="text-slate-400 block text-[11px]">Đối tác ID</span>
            <span className="font-mono text-slate-700 truncate block">{contract.partnerId}</span>
          </div>
          <div>
            <span className="text-slate-400 block text-[11px]">Thời hạn</span>
            <span className="text-slate-700 font-medium">
              {new Date(contract.effectiveDate).toLocaleDateString('vi-VN')} &rarr;{' '}
              {new Date(contract.expiryDate).toLocaleDateString('vi-VN')}
            </span>
          </div>
        </div>
      )}

      {/* Footer Action Buttons */}
      <div className="pt-2 flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-t border-slate-100">
        <div className="flex items-center gap-2">
          {onViewContractDetail && (
            <button
              type="button"
              onClick={() => onViewContractDetail(item.contractId)}
              className="text-xs font-semibold text-indigo-600 hover:text-indigo-800 flex items-center gap-1"
            >
              <span>Xem chi tiết hợp đồng</span>
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
              </svg>
            </button>
          )}
        </div>

        <div className="flex items-center gap-2.5">
          {/* Reject button */}
          <button
            type="button"
            disabled={isProcessing}
            onClick={() => onReject(item)}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-rose-700 bg-rose-50 hover:bg-rose-100 active:bg-rose-200 border border-rose-200 rounded-xl transition-all disabled:opacity-50"
          >
            <svg className="w-4 h-4 text-rose-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
            <span>Từ chối</span>
          </button>

          {/* Approve button */}
          <button
            type="button"
            disabled={isProcessing}
            onClick={() => onApprove(item)}
            className="inline-flex items-center gap-1.5 px-5 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-emerald-500 disabled:opacity-50"
          >
            <svg className="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
            </svg>
            <span>Phê duyệt</span>
          </button>
        </div>
      </div>
    </div>
  );
};

interface ApprovalWaitingListPageProps {
  onSelectContract?: (contractId: string) => void;
}

export const ApprovalWaitingListPage: FC<ApprovalWaitingListPageProps> = ({ onSelectContract }) => {
  const queryClient = useQueryClient();
  const [rejectingItem, setRejectingItem] = useState<PendingApprovalItemDto | null>(null);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Fetch pending approval items
  const {
    data: pendingList = [],
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ['pending-approvals'],
    queryFn: () => getPendingApprovals(),
    refetchInterval: 10000,
  });

  // Process approval decision mutation
  const decisionMutation = useMutation({
    mutationFn: (payload: ProcessApprovalDecisionRequest) => processApprovalDecision(payload),
    onSuccess: (progress, variables) => {
      queryClient.invalidateQueries({ queryKey: ['pending-approvals'] });
      queryClient.invalidateQueries({ queryKey: ['contract', variables.approvalStepId] });
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      queryClient.invalidateQueries({ queryKey: ['approval-progress'] });

      setRejectingItem(null);
      const isApproved = variables.decision === ApprovalDecision.Approved;
      setToast({
        type: 'success',
        message: isApproved
          ? `Đã phê duyệt thành công! Tiến trình tổng thể hiện tại: ${progress.overallStatus}.`
          : `Đã từ chối bước duyệt. Hợp đồng được chuyển về trạng thái Bản nháp (Draft).`,
      });
      setTimeout(() => setToast(null), 5000);
    },
    onError: (err: Error) => {
      setToast({ type: 'error', message: `Lỗi ra quyết định: ${err.message}` });
      setTimeout(() => setToast(null), 7000);
    },
  });

  const handleApprove = (item: PendingApprovalItemDto) => {
    decisionMutation.mutate({
      approvalStepId: item.approvalStepId,
      approverId: item.approverId,
      decision: ApprovalDecision.Approved,
      comment: 'Đồng ý phê duyệt',
    });
  };

  const handleConfirmReject = (comment: string) => {
    if (!rejectingItem) return;
    decisionMutation.mutate({
      approvalStepId: rejectingItem.approvalStepId,
      approverId: rejectingItem.approverId,
      decision: ApprovalDecision.Rejected,
      comment,
    });
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
      {/* Toast alert */}
      {toast && (
        <div
          className={`p-4 rounded-xl text-xs flex items-center justify-between border shadow-md animate-in slide-in-from-top duration-200 ${
            toast.type === 'success'
              ? 'bg-emerald-50 text-emerald-800 border-emerald-200'
              : 'bg-rose-50 text-rose-800 border-rose-200'
          }`}
        >
          <div className="flex items-center gap-2">
            {toast.type === 'success' ? (
              <svg className="w-4 h-4 text-emerald-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            ) : (
              <svg className="w-4 h-4 text-rose-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
            )}
            <span className="font-medium">{toast.message}</span>
          </div>
          <button type="button" onClick={() => setToast(null)} className="underline hover:opacity-80">
            Đóng
          </button>
        </div>
      )}

      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-200 pb-5">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="text-2xl font-bold text-slate-900 tracking-tight">
              Danh sách Chờ phê duyệt
            </h1>
            <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200 flex items-center gap-1">
              <span className="w-1.5 h-1.5 rounded-full bg-amber-500 animate-pulse" />
              {pendingList.length} hồ sơ đang chờ
            </span>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            Xem xét thông tin hợp đồng, kiểm tra chứng từ và thực hiện Duyệt hoặc Từ chối kèm lý do
          </p>
        </div>

        <button
          type="button"
          onClick={() => refetch()}
          disabled={isLoading}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white hover:bg-slate-50 border border-slate-300 rounded-xl shadow-xs transition-colors shrink-0"
        >
          <svg className={`w-3.5 h-3.5 text-slate-500 ${isLoading ? 'animate-spin' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          <span>Làm mới</span>
        </button>
      </div>

      {/* Loading Skeleton */}
      {isLoading && (
        <div className="space-y-4 animate-pulse">
          {[1, 2, 3].map((i) => (
            <div key={i} className="bg-white rounded-2xl p-6 border border-slate-200 space-y-3">
              <div className="h-6 bg-slate-200 rounded w-1/3" />
              <div className="h-4 bg-slate-100 rounded w-2/3" />
              <div className="h-12 bg-slate-50 rounded-xl" />
            </div>
          ))}
        </div>
      )}

      {/* Error State */}
      {isError && (
        <div className="bg-white rounded-2xl p-8 border border-slate-200 text-center space-y-3">
          <div className="w-12 h-12 rounded-full bg-rose-50 border border-rose-200 text-rose-600 flex items-center justify-center mx-auto">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <h3 className="text-base font-bold text-slate-900">Không thể tải danh sách chờ duyệt</h3>
          <p className="text-xs text-slate-500 max-w-md mx-auto">
            {error instanceof Error ? error.message : 'Đã có lỗi xảy ra.'}
          </p>
          <button
            type="button"
            onClick={() => refetch()}
            className="px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl transition-colors"
          >
            Thử tải lại
          </button>
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !isError && pendingList.length === 0 && (
        <div className="bg-white rounded-2xl p-12 border border-slate-200 text-center space-y-4 shadow-xs">
          <div className="w-12 h-12 rounded-2xl bg-emerald-50 border border-emerald-200 text-emerald-600 flex items-center justify-center mx-auto">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h3 className="text-base font-bold text-slate-900">Không có hồ sơ nào đang chờ duyệt</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Tất cả các hợp đồng đệ trình đã được xử lý xong. Khi có hợp đồng mới gửi duyệt, thông báo sẽ hiển thị tại đây.
          </p>
        </div>
      )}

      {/* Pending Items List */}
      {!isLoading && !isError && pendingList.length > 0 && (
        <div className="space-y-4">
          {pendingList.map((item) => (
            <PendingApprovalCard
              key={item.approvalStepId}
              item={item}
              onApprove={handleApprove}
              onReject={(it) => setRejectingItem(it)}
              isProcessing={decisionMutation.isPending}
              onViewContractDetail={onSelectContract}
            />
          ))}
        </div>
      )}

      {/* Rejection Reason Modal */}
      {rejectingItem && (
        <RejectDecisionModal
          isOpen={!!rejectingItem}
          contractId={rejectingItem.contractId}
          stepOrder={rejectingItem.stepOrder}
          isLoading={decisionMutation.isPending}
          onClose={() => setRejectingItem(null)}
          onConfirm={handleConfirmReject}
        />
      )}
    </div>
  );
};
