import type { FC } from 'react';
import type { ContractDto } from '../types/contract.types';
import { ContractStatus } from '../types/contract.types';
import { ContractStatusBadge } from './ContractStatusBadge';

interface ContractTableProps {
  contracts: ContractDto[];
  onSubmitContract: (contract: ContractDto) => void;
  onSelectContract?: (contract: ContractDto) => void;
  submittingId: string | null;
  partnerMap?: Record<string, string>;
}

export const ContractTable: FC<ContractTableProps> = ({
  contracts,
  onSubmitContract,
  onSelectContract,
  submittingId,
  partnerMap,
}) => {
  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
    }).format(value);
  };

  const formatDate = (dateString?: string | null) => {
    if (!dateString) return '—';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
      });
    } catch {
      return dateString;
    }
  };

  return (
    <div className="overflow-x-auto bg-white rounded-xl shadow-sm border border-slate-200">
      <table className="table w-full text-left border-collapse">
        <thead>
          <tr className="bg-slate-50/80 border-b border-slate-200 text-slate-600 text-xs uppercase tracking-wider">
            <th className="py-3.5 px-4 font-semibold">Số hợp đồng</th>
            <th className="py-3.5 px-4 font-semibold">Tiêu đề</th>
            <th className="py-3.5 px-4 font-semibold">Loại hợp đồng</th>
            <th className="py-3.5 px-4 font-semibold text-right">Giá trị (VNĐ)</th>
            <th className="py-3.5 px-4 font-semibold text-center">Trạng thái</th>
            <th className="py-3.5 px-4 font-semibold">Thời hạn</th>
            <th className="py-3.5 px-4 font-semibold">Ngày tạo</th>
            <th className="py-3.5 px-4 font-semibold text-center">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 text-sm text-slate-700">
          {contracts.map((contract) => {
            const isDraft = contract.status === ContractStatus.Draft;
            const isSubmitting = submittingId === contract.id;

            return (
              <tr
                key={contract.id}
                className="hover:bg-slate-50/60 transition-colors duration-150"
              >
                {/* Contract Number */}
                <td className="py-3.5 px-4 font-medium text-indigo-600">
                  <button
                    type="button"
                    onClick={() => onSelectContract?.(contract)}
                    className="font-mono bg-indigo-50/70 text-indigo-700 hover:bg-indigo-100 px-2 py-0.5 rounded border border-indigo-200/50 cursor-pointer text-left transition-colors"
                    title="Bấm để xem chi tiết hợp đồng"
                  >
                    {contract.contractNumber}
                  </button>
                </td>

                {/* Title */}
                <td className="py-3.5 px-4 max-w-xs">
                  <button
                    type="button"
                    onClick={() => onSelectContract?.(contract)}
                    className="font-medium text-slate-900 hover:text-indigo-600 truncate text-left block w-full transition-colors"
                    title={contract.title}
                  >
                    {contract.title}
                  </button>
                  {contract.partnerId && (
                    <div className="text-xs text-slate-500 truncate mt-0.5 flex items-center gap-1" title={`Partner: ${contract.partnerId}`}>
                      <span className="text-slate-400 font-normal">Đối tác:</span>
                      <span className="font-medium text-slate-700">
                        {partnerMap?.[contract.partnerId] || `ID: ${contract.partnerId.substring(0, 8)}...`}
                      </span>
                    </div>
                  )}
                </td>

                {/* Contract Type */}
                <td className="py-3.5 px-4 text-slate-600">
                  <span className="inline-block bg-slate-100 px-2 py-0.5 rounded text-xs text-slate-700 font-medium">
                    {contract.contractTypeName || 'Hợp đồng thông thường'}
                  </span>
                </td>

                {/* Value */}
                <td className="py-3.5 px-4 text-right font-semibold text-slate-900">
                  {formatCurrency(contract.value)}
                </td>

                {/* Status */}
                <td className="py-3.5 px-4 text-center">
                  <ContractStatusBadge status={contract.status} />
                </td>

                {/* Effective -> Expiry */}
                <td className="py-3.5 px-4 text-xs text-slate-600 whitespace-nowrap">
                  <div>{formatDate(contract.effectiveDate)}</div>
                  <div className="text-slate-400">→ {formatDate(contract.expiryDate)}</div>
                </td>

                {/* Created Date */}
                <td className="py-3.5 px-4 text-xs text-slate-500 whitespace-nowrap">
                  {formatDate(contract.createdAt)}
                </td>

                {/* Actions */}
                <td className="py-3.5 px-4 text-center">
                  <div className="flex items-center justify-center gap-1.5">
                    <button
                      type="button"
                      onClick={() => onSelectContract?.(contract)}
                      className="inline-flex items-center gap-1 px-2.5 py-1.5 text-xs font-semibold text-slate-700 hover:text-indigo-600 hover:bg-slate-100 rounded-lg transition-colors border border-slate-200"
                      title="Xem chi tiết hợp đồng"
                    >
                      <span>Chi tiết</span>
                    </button>

                    {isDraft ? (
                      <button
                        type="button"
                        disabled={isSubmitting}
                        onClick={() => onSubmitContract(contract)}
                        className="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 disabled:opacity-50 disabled:cursor-not-allowed rounded-lg shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-indigo-500"
                        title="Gửi duyệt hợp đồng này"
                      >
                        {isSubmitting ? (
                          <>
                            <svg
                              className="animate-spin h-3.5 w-3.5 text-white"
                              xmlns="http://www.w3.org/2000/svg"
                              fill="none"
                              viewBox="0 0 24 24"
                            >
                              <circle
                                className="opacity-25"
                                cx="12"
                                cy="12"
                                r="10"
                                stroke="currentColor"
                                strokeWidth="4"
                              />
                              <path
                                className="opacity-75"
                                fill="currentColor"
                                d="M4 12a8 8 0 018-8v8H4z"
                              />
                            </svg>
                            <span>Đang gửi...</span>
                          </>
                        ) : (
                          <>
                            <svg
                              className="w-3.5 h-3.5"
                              fill="none"
                              stroke="currentColor"
                              viewBox="0 0 24 24"
                            >
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth="2"
                                d="M9 5l7 7-7 7"
                              />
                            </svg>
                            <span>Gửi duyệt</span>
                          </>
                        )}
                      </button>
                    ) : (
                      <span className="text-xs text-slate-400 italic px-2">
                        {contract.status === ContractStatus.PendingApproval ? 'Đang duyệt' : '—'}
                      </span>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
};
