import type { FC } from 'react';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { ContractDto } from '../types/contract.types';
import { ContractStatus, CONTRACT_STATUS_MAP } from '../types/contract.types';
import { getContractById, submitContract, getApprovalProgress } from '../services/contractApi';
import { ContractStatusBadge } from '../components/ContractStatusBadge';
import { EditContractModal } from '../components/EditContractModal';
import { SignContractModal } from '../../workflows/components/SignContractModal';
import { getSignaturesByContractId, getSignatureStatus } from '../../workflows/services/workflowApi';
import type { SignatureDto } from '../../workflows/types/workflow.types';
import { getPartnerById } from '../../partners/services/partnerApi';
import { getAttachmentsByContract, uploadAttachment, getAttachmentDownloadUrl } from '../services/attachmentApi';
import type { AttachmentDto } from '../types/attachment.types';
import { AIAssistantModal } from '../../ai/components/AIAssistantModal';

interface ContractDetailPageProps {
  contractId: string;
  onBack: () => void;
}

export const ContractDetailPage: FC<ContractDetailPageProps> = ({ contractId, onBack }) => {
  const queryClient = useQueryClient();
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isSignModalOpen, setIsSignModalOpen] = useState(false);
  const [isAIModalOpen, setIsAIModalOpen] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Fetch contract detail
  const {
    data: contract,
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery<ContractDto>({
    queryKey: ['contract', contractId],
    queryFn: () => getContractById(contractId),
  });

  // Fetch partner info
  const { data: partner } = useQuery({
    queryKey: ['partner', contract?.partnerId],
    queryFn: () => getPartnerById(contract!.partnerId),
    enabled: !!contract?.partnerId,
  });

  // Fetch attachments
  const { data: attachments = [], refetch: refetchAttachments, isLoading: isAttachmentsLoading } = useQuery<AttachmentDto[]>({
    queryKey: ['attachments', contractId],
    queryFn: () => getAttachmentsByContract(contractId),
    enabled: !!contractId,
  });

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setIsUploading(true);
    try {
      await uploadAttachment(contractId, file);
      await refetchAttachments();
      setNotification({ type: 'success', message: `Đã tải lên tệp "${file.name}" thành công!` });
      setTimeout(() => setNotification(null), 5000);
    } catch (err: unknown) {
      setNotification({
        type: 'error',
        message: `Lỗi tải lên tệp: ${err instanceof Error ? err.message : 'Thất bại'}`,
      });
      setTimeout(() => setNotification(null), 7000);
    } finally {
      setIsUploading(false);
      e.target.value = '';
    }
  };

  // Fetch approval steps / progress if any
  const { data: approvalProgress } = useQuery({
    queryKey: ['approval-progress', contractId],
    queryFn: () => getApprovalProgress(contractId),
    enabled: !!contract && contract.status !== ContractStatus.Draft,
  });

  // Fetch signatures & signature status if contract is Approved or Signed
  const canHaveSignatures = !!contract && (contract.status === ContractStatus.Approved || contract.status === ContractStatus.Signed);
  const { data: signatures = [] } = useQuery<SignatureDto[]>({
    queryKey: ['signatures', contractId],
    queryFn: () => getSignaturesByContractId(contractId),
    enabled: canHaveSignatures,
  });

  const { data: signatureStatus } = useQuery({
    queryKey: ['signature-status', contractId],
    queryFn: () => getSignatureStatus(contractId),
    enabled: canHaveSignatures,
  });

  // Submit contract mutation
  const submitMutation = useMutation({
    mutationFn: (id: string) => submitContract(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contract', contractId] });
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      queryClient.invalidateQueries({ queryKey: ['approval-progress', contractId] });
      setNotification({
        type: 'success',
        message: 'Đã đệ trình hợp đồng để phê duyệt thành công! Tiến trình duyệt đã khởi tạo.',
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

  const formatCurrency = (val?: number) => {
    if (val === undefined || val === null) return '0 ₫';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(val);
  };

  const formatDate = (dateString?: string | null) => {
    if (!dateString) return '—';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('vi-VN', {
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

  // Loading skeleton state
  if (isLoading) {
    return (
      <div className="p-6 max-w-6xl mx-auto w-full space-y-6 animate-pulse">
        <div className="h-8 bg-slate-200 rounded-lg w-48" />
        <div className="bg-white rounded-2xl p-6 border border-slate-200 space-y-4">
          <div className="h-10 bg-slate-200 rounded-lg w-3/4" />
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-4">
            <div className="h-20 bg-slate-100 rounded-xl" />
            <div className="h-20 bg-slate-100 rounded-xl" />
            <div className="h-20 bg-slate-100 rounded-xl" />
            <div className="h-20 bg-slate-100 rounded-xl" />
          </div>
        </div>
      </div>
    );
  }

  // Error / Not found state
  if (isError || !contract) {
    return (
      <div className="p-6 max-w-4xl mx-auto w-full">
        <div className="bg-white rounded-2xl p-8 border border-slate-200 text-center space-y-4 shadow-sm">
          <div className="w-12 h-12 rounded-full bg-rose-50 border border-rose-200 text-rose-600 flex items-center justify-center mx-auto">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <h2 className="text-xl font-bold text-slate-900">Không tìm thấy hợp đồng</h2>
          <p className="text-sm text-slate-500 max-w-md mx-auto">
            {error instanceof Error ? error.message : `Hợp đồng với Id: ${contractId} không tồn tại hoặc đã bị xóa.`}
          </p>
          <div className="flex items-center justify-center gap-3 pt-2">
            <button
              type="button"
              onClick={onBack}
              className="px-4 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-xl transition-colors"
            >
              ← Quay lại danh sách
            </button>
            <button
              type="button"
              onClick={() => refetch()}
              className="px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl transition-colors"
            >
              Thử tải lại
            </button>
          </div>
        </div>
      </div>
    );
  }

  const isDraft = contract.status === ContractStatus.Draft;
  const statusInfo = CONTRACT_STATUS_MAP[contract.status];

  return (
    <div className="p-6 max-w-6xl mx-auto w-full space-y-6">
      {/* Toast Notification */}
      {notification && (
        <div
          className={`p-4 rounded-xl text-sm flex items-center justify-between border shadow-md animate-in slide-in-from-top duration-200 ${
            notification.type === 'success'
              ? 'bg-emerald-50 text-emerald-800 border-emerald-200'
              : 'bg-rose-50 text-rose-800 border-rose-200'
          }`}
        >
          <div className="flex items-center gap-2.5">
            {notification.type === 'success' ? (
              <svg className="w-5 h-5 text-emerald-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
              </svg>
            ) : (
              <svg className="w-5 h-5 text-rose-600" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
            )}
            <span className="font-medium">{notification.message}</span>
          </div>
          <button
            type="button"
            onClick={() => setNotification(null)}
            className="text-xs underline hover:opacity-80"
          >
            Đóng
          </button>
        </div>
      )}

      {/* Top Bar: Back & Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <button
          type="button"
          onClick={onBack}
          className="inline-flex items-center gap-2 text-xs font-semibold text-indigo-600 hover:text-indigo-800 transition-colors w-fit"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Quay lại danh sách Hợp đồng
        </button>

        {/* Action buttons */}
        <div className="flex items-center gap-2.5">
          {/* Edit Button: ONLY for Draft contracts */}
          {isDraft ? (
            <button
              type="button"
              onClick={() => setIsEditModalOpen(true)}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-slate-700 bg-white hover:bg-slate-50 border border-slate-300 rounded-xl shadow-xs transition-all hover:border-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <svg className="w-3.5 h-3.5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
              <span>Chỉnh sửa</span>
            </button>
          ) : (
            <div className="flex items-center gap-1 text-xs text-slate-400 bg-slate-100 px-3 py-1.5 rounded-lg border border-slate-200">
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
              </svg>
              <span>Không thể sửa ({statusInfo?.label})</span>
            </div>
          )}

          {/* AI Assistant Button */}
          <button
            type="button"
            onClick={() => setIsAIModalOpen(true)}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-purple-700 bg-purple-50 hover:bg-purple-100 border border-purple-200 rounded-xl shadow-xs transition-colors"
          >
            <span>✨</span>
            <span>Trợ lý AI</span>
          </button>

          {/* Sign Button: ONLY for Approved contracts */}
          {contract.status === ContractStatus.Approved && (
            <button
              type="button"
              onClick={() => setIsSignModalOpen(true)}
              className="inline-flex items-center gap-2 px-5 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
              </svg>
              <span>Ký số hợp đồng</span>
            </button>
          )}

          {/* Submit Button: ONLY for Draft contracts */}
          {isDraft && (
            <button
              type="button"
              disabled={submitMutation.isPending}
              onClick={() => submitMutation.mutate(contract.id)}
              className="inline-flex items-center gap-2 px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 disabled:opacity-50 disabled:cursor-not-allowed rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              {submitMutation.isPending ? (
                <>
                  <svg className="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                  </svg>
                  <span>Đang gửi duyệt...</span>
                </>
              ) : (
                <>
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 5l7 7-7 7" />
                  </svg>
                  <span>Gửi duyệt ngay</span>
                </>
              )}
            </button>
          )}
        </div>
      </div>

      {/* Main Header Card */}
      <div className="bg-white rounded-2xl p-6 sm:p-8 border border-slate-200 shadow-xs space-y-6">
        <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-4">
          <div className="space-y-2">
            <div className="flex items-center gap-2.5 flex-wrap">
              <span className="font-mono text-sm font-bold px-2.5 py-1 bg-indigo-50 text-indigo-700 rounded-lg border border-indigo-200">
                {contract.contractNumber}
              </span>
              <span className="text-xs px-2.5 py-1 bg-slate-100 text-slate-700 rounded-lg font-medium">
                {contract.contractTypeName || 'Hợp đồng tiêu chuẩn'}
              </span>
              <ContractStatusBadge status={contract.status} />
            </div>
            <h1 className="text-2xl sm:text-3xl font-bold text-slate-900 tracking-tight">
              {contract.title}
            </h1>
            <p className="text-xs text-slate-400 font-mono">ID: {contract.id}</p>
          </div>

          {/* Value Display */}
          <div className="bg-slate-50 border border-slate-200/80 rounded-2xl p-4 md:text-right shrink-0">
            <span className="block text-xs uppercase tracking-wider text-slate-500 font-semibold">
              Tổng giá trị hợp đồng
            </span>
            <span className="block text-2xl font-bold text-indigo-700 mt-0.5">
              {formatCurrency(contract.value)}
            </span>
          </div>
        </div>

        {/* 4 Quick Stat Highlights */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 pt-4 border-t border-slate-100 text-xs">
          <div className="p-3.5 bg-slate-50/70 rounded-xl">
            <span className="text-slate-400 block mb-1">Hiệu lực từ ngày</span>
            <span className="font-semibold text-slate-800">{formatDate(contract.effectiveDate)}</span>
          </div>
          <div className="p-3.5 bg-slate-50/70 rounded-xl">
            <span className="text-slate-400 block mb-1">Thời hạn kết thúc</span>
            <span className="font-semibold text-slate-800">{formatDate(contract.expiryDate)}</span>
          </div>
          <div className="p-3.5 bg-slate-50/70 rounded-xl">
            <span className="text-slate-400 block mb-1">Ngày ký kết</span>
            <span className="font-semibold text-slate-800">{formatDate(contract.signedDate)}</span>
          </div>
          <div className="p-3.5 bg-slate-50/70 rounded-xl">
            <span className="text-slate-400 block mb-1">Cập nhật lần cuối</span>
            <span className="font-semibold text-slate-800">{formatDate(contract.updatedAt || contract.createdAt)}</span>
          </div>
        </div>
      </div>

      {/* Detail Sections: 2 Columns */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Left Column: Related Entities & Information */}
        <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-xs space-y-4">
          <h2 className="text-base font-bold text-slate-900 border-b border-slate-100 pb-3 flex items-center gap-2">
            <svg className="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
            Đối tác & Đơn vị phụ trách
          </h2>

          <dl className="divide-y divide-slate-100 text-xs">
            <div className="py-2.5 flex justify-between items-center">
              <dt className="text-slate-500 font-medium">Đối tác liên kết</dt>
              <dd className="text-slate-800 font-semibold text-right">
                {partner ? (
                  <div>
                    <div>{partner.name}</div>
                    {partner.taxCode && (
                      <span className="text-[11px] font-normal text-slate-500 font-mono">
                        MST: {partner.taxCode}
                      </span>
                    )}
                  </div>
                ) : (
                  <span className="font-mono text-xs">{contract.partnerId}</span>
                )}
              </dd>
            </div>
            <div className="py-2.5 flex justify-between">
              <dt className="text-slate-500 font-medium">Người tạo / Quản lý (Owner ID)</dt>
              <dd className="font-mono text-slate-800 font-semibold">{contract.ownerId}</dd>
            </div>
            <div className="py-2.5 flex justify-between">
              <dt className="text-slate-500 font-medium">Mẫu hợp đồng (Template Version ID)</dt>
              <dd className="font-mono text-slate-800">{contract.templateVersionUsedId}</dd>
            </div>
            {contract.parentContractId && (
              <div className="py-2.5 flex justify-between">
                <dt className="text-slate-500 font-medium">Hợp đồng gốc (Parent Contract ID)</dt>
                <dd className="font-mono text-indigo-600 font-semibold">{contract.parentContractId}</dd>
              </div>
            )}
            <div className="py-2.5 flex justify-between">
              <dt className="text-slate-500 font-medium">Thời điểm tạo</dt>
              <dd className="text-slate-700">{formatDate(contract.createdAt)}</dd>
            </div>
          </dl>
        </div>

        {/* Right Column: Attachment / Document */}
        <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h2 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <svg className="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13" />
              </svg>
              Tài liệu đính kèm ({attachments.length})
            </h2>
            <label className="cursor-pointer inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold text-indigo-600 bg-indigo-50 hover:bg-indigo-100 rounded-lg transition-colors">
              <span>{isUploading ? 'Đang tải...' : '+ Tải tệp lên'}</span>
              <input
                type="file"
                className="hidden"
                disabled={isUploading}
                onChange={handleFileUpload}
              />
            </label>
          </div>

          {isAttachmentsLoading ? (
            <div className="py-6 text-center text-xs text-slate-400">Đang tải danh sách tài liệu...</div>
          ) : attachments.length > 0 ? (
            <div className="divide-y divide-slate-100 max-h-64 overflow-y-auto">
              {attachments.map((att) => (
                <div key={att.id} className="py-2.5 flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2.5 min-w-0">
                    <span className="w-7 h-7 rounded bg-indigo-50 text-indigo-600 flex items-center justify-center text-xs font-bold shrink-0">
                      v{att.version}
                    </span>
                    <div className="min-w-0 truncate">
                      <p className="text-xs font-semibold text-slate-800 truncate">{att.fileName}</p>
                      <p className="text-[10px] text-slate-400">{formatDate(att.uploadedAt)}</p>
                    </div>
                  </div>
                  <a
                    href={getAttachmentDownloadUrl(att.id)}
                    target="_blank"
                    rel="noopener noreferrer"
                    download
                    className="px-2.5 py-1 text-xs font-medium text-indigo-600 hover:bg-indigo-50 border border-indigo-200 rounded-md transition-colors shrink-0"
                  >
                    Tải về
                  </a>
                </div>
              ))}
            </div>
          ) : contract.fileUrl ? (
            <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl flex items-center justify-between">
              <div className="flex items-center gap-3 overflow-hidden">
                <div className="w-10 h-10 rounded-lg bg-indigo-50 border border-indigo-200 text-indigo-600 flex items-center justify-center shrink-0">
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                  </svg>
                </div>
                <div className="truncate">
                  <span className="block text-xs font-semibold text-slate-800 truncate">{contract.title}.pdf</span>
                  <span className="block text-[11px] text-slate-400 font-mono truncate">{contract.fileUrl}</span>
                </div>
              </div>
              <a
                href={contract.fileUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="px-3 py-1.5 text-xs font-semibold text-indigo-600 bg-white hover:bg-indigo-50 border border-indigo-200 rounded-lg transition-colors shrink-0 flex items-center gap-1"
              >
                <span>Xem tệp</span>
              </a>
            </div>
          ) : (
            <div className="p-6 bg-slate-50 border border-dashed border-slate-200 rounded-xl text-center space-y-2">
              <p className="text-xs text-slate-400">Chưa có tệp đính kèm nào được tải lên cho hợp đồng này.</p>
              <label className="cursor-pointer text-xs text-indigo-600 font-semibold hover:underline inline-block">
                Bấm vào đây để tải lên tệp đầu tiên
                <input
                  type="file"
                  className="hidden"
                  disabled={isUploading}
                  onChange={handleFileUpload}
                />
              </label>
            </div>
          )}
        </div>
      </div>

      {/* Approval Process Runtime Steps (if available) */}
      {approvalProgress && approvalProgress.steps.length > 0 && (
        <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-xs space-y-4">
          <div className="flex items-center justify-between border-b border-slate-100 pb-3">
            <h2 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <svg className="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Tiến trình Phê duyệt ({approvalProgress.workflowName})
            </h2>
            <span className="text-xs px-2.5 py-0.5 rounded-full bg-slate-100 text-slate-700 font-medium">
              Đã duyệt: {approvalProgress.approvedStepsCount} / {approvalProgress.totalSteps} bước
            </span>
          </div>

          <div className="divide-y divide-slate-100 text-xs">
            {approvalProgress.steps.map((step) => {
              const isApproved = step.decision === 1;
              const isRejected = step.decision === 2;
              const isPending = step.decision === 0;

              return (
                <div key={step.id} className="py-3 flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <span className="w-6 h-6 rounded-full bg-slate-100 font-bold flex items-center justify-center text-slate-600">
                      {step.stepOrder}
                    </span>
                    <div>
                      <span className="font-semibold text-slate-800">
                        Vai trò: {step.approverRoleName || `Role #${step.approverRole}`}
                      </span>
                      <span className="block text-[11px] text-slate-400 font-mono">
                        Người duyệt ID: {step.approverId}
                      </span>
                      {step.comment && (
                        <p className="mt-1 text-slate-600 italic bg-slate-50 p-1.5 rounded border border-slate-200">
                          &ldquo;{step.comment}&rdquo;
                        </p>
                      )}
                    </div>
                  </div>

                  <div>
                    {isApproved && (
                      <span className="px-2.5 py-1 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold">
                        ✓ Đã duyệt
                      </span>
                    )}
                    {isRejected && (
                      <span className="px-2.5 py-1 rounded-full bg-rose-50 text-rose-700 border border-rose-200 font-semibold">
                        ✕ Từ chối
                      </span>
                    )}
                    {isPending && (
                      <span className="px-2.5 py-1 rounded-full bg-amber-50 text-amber-700 border border-amber-200 font-semibold flex items-center gap-1">
                        <span className="w-1.5 h-1.5 rounded-full bg-amber-500 animate-pulse" />
                        Chờ duyệt
                      </span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Digital Signature & Tracking (for Approved & Signed contracts) */}
      {canHaveSignatures && (
        <div className="bg-white rounded-2xl p-6 border border-slate-200 shadow-xs space-y-5">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 border-b border-slate-100 pb-4">
            <div>
              <h2 className="text-base font-bold text-slate-900 flex items-center gap-2">
                <svg className="w-4 h-4 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                </svg>
                Chữ ký điện tử & Ký kết hợp đồng
              </h2>
              <p className="text-xs text-slate-500 mt-0.5">
                Yêu cầu đầy đủ chữ ký từ Đại diện Nội bộ và Đại diện Đối tác để hợp đồng có hiệu lực pháp lý
              </p>
            </div>

            {contract.status === ContractStatus.Approved && !signatureStatus?.isFullySigned && (
              <button
                type="button"
                onClick={() => setIsSignModalOpen(true)}
                className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-xl shadow-xs transition-colors shrink-0"
              >
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
                </svg>
                <span>Thực hiện ký số ngay</span>
              </button>
            )}
          </div>

          {/* 2-Party Signature Progress Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Internal Signer */}
            <div className={`p-4 rounded-xl border transition-all ${
              signatureStatus?.hasInternalSignature
                ? 'bg-emerald-50/60 border-emerald-200'
                : 'bg-slate-50 border-slate-200'
            }`}>
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-slate-800">1. Đại diện Nội bộ</span>
                {signatureStatus?.hasInternalSignature ? (
                  <span className="text-xs font-semibold text-emerald-700 bg-emerald-100/80 px-2 py-0.5 rounded-full flex items-center gap-1">
                    ✓ Đã ký
                  </span>
                ) : (
                  <span className="text-xs font-medium text-amber-700 bg-amber-100/80 px-2 py-0.5 rounded-full">
                    Chưa ký
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-500 mt-2">
                {signatureStatus?.hasInternalSignature
                  ? 'Đại diện thẩm quyền của công ty đã ký duyệt và xác thực.'
                  : 'Cần tài khoản nội bộ (Admin/Manager) ký phê duyệt.'}
              </p>
            </div>

            {/* Partner Signer */}
            <div className={`p-4 rounded-xl border transition-all ${
              signatureStatus?.hasPartnerSignature
                ? 'bg-emerald-50/60 border-emerald-200'
                : 'bg-slate-50 border-slate-200'
            }`}>
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-slate-800">2. Đại diện Đối tác</span>
                {signatureStatus?.hasPartnerSignature ? (
                  <span className="text-xs font-semibold text-emerald-700 bg-emerald-100/80 px-2 py-0.5 rounded-full flex items-center gap-1">
                    ✓ Đã ký
                  </span>
                ) : (
                  <span className="text-xs font-medium text-amber-700 bg-amber-100/80 px-2 py-0.5 rounded-full">
                    Chưa ký
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-500 mt-2">
                {signatureStatus?.hasPartnerSignature
                  ? 'Đại diện hợp pháp của đối tác đã ký số và xác nhận.'
                  : 'Cần đại diện đối tác ký xác nhận thông qua OTP hoặc Mock CA.'}
              </p>
            </div>
          </div>

          {/* Signatures List Table */}
          {signatures.length > 0 ? (
            <div className="overflow-x-auto pt-2">
              <table className="w-full text-left text-xs border border-slate-100 rounded-xl overflow-hidden">
                <thead className="bg-slate-50 text-slate-600 font-semibold border-b border-slate-100">
                  <tr>
                    <th className="py-2.5 px-4">Bên ký</th>
                    <th className="py-2.5 px-4">Họ và tên người ký</th>
                    <th className="py-2.5 px-4">Phương thức</th>
                    <th className="py-2.5 px-4">Thời gian ký</th>
                    <th className="py-2.5 px-4">Chữ ký điện tử / Băm SHA-256</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {signatures.map((sig: SignatureDto) => (
                    <tr key={sig.id} className="hover:bg-slate-50/70">
                      <td className="py-3 px-4 font-semibold">
                        {sig.signerType === 0 ? (
                          <span className="text-indigo-700 bg-indigo-50 px-2 py-0.5 rounded border border-indigo-200">
                            Nội bộ
                          </span>
                        ) : (
                          <span className="text-cyan-700 bg-cyan-50 px-2 py-0.5 rounded border border-cyan-200">
                            Đối tác
                          </span>
                        )}
                      </td>
                      <td className="py-3 px-4 font-medium text-slate-900">
                        {sig.signerNameSnapshot}
                      </td>
                      <td className="py-3 px-4 text-slate-600">
                        {sig.signatureMethod === 0
                          ? 'Mock SHA-256'
                          : sig.signatureMethod === 1
                          ? 'OTP SMS'
                          : 'Digital CA'}
                      </td>
                      <td className="py-3 px-4 text-slate-600">
                        {formatDate(sig.signedAt)}
                      </td>
                      <td className="py-3 px-4 font-mono text-[11px] text-slate-500 max-w-xs truncate" title={sig.signatureHash}>
                        {sig.signatureHash}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="p-4 bg-slate-50/70 rounded-xl text-center text-xs text-slate-400 border border-slate-100">
              Chưa có chữ ký nào được ghi nhận cho hợp đồng này.
            </div>
          )}
        </div>
      )}

      {/* Edit Contract Modal */}
      <EditContractModal
        contract={contract}
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        onSuccess={(updated) => {
          queryClient.setQueryData(['contract', contractId], updated);
          queryClient.invalidateQueries({ queryKey: ['contracts'] });
          setNotification({
            type: 'success',
            message: 'Đã cập nhật thông tin hợp đồng thành công!',
          });
          setTimeout(() => setNotification(null), 5000);
        }}
      />

      {/* Sign Contract Modal */}
      {contract && (
        <SignContractModal
          isOpen={isSignModalOpen}
          contract={contract}
          onClose={() => setIsSignModalOpen(false)}
          onSuccess={() => {
            queryClient.invalidateQueries({ queryKey: ['contract', contractId] });
            queryClient.invalidateQueries({ queryKey: ['contracts'] });
            queryClient.invalidateQueries({ queryKey: ['signatures', contractId] });
            queryClient.invalidateQueries({ queryKey: ['signature-status', contractId] });
            setNotification({
              type: 'success',
              message: 'Đã ký hợp đồng thành công!',
            });
            setTimeout(() => setNotification(null), 5000);
          }}
        />
      )}

      {/* AI Assistant Modal */}
      {contract && (
        <AIAssistantModal
          isOpen={isAIModalOpen}
          contractId={contract.id}
          contractTitle={contract.title}
          onClose={() => setIsAIModalOpen(false)}
        />
      )}
    </div>
  );
};
