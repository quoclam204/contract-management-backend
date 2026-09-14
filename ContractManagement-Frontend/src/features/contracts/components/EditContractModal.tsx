import type { FC, FormEvent } from 'react';
import { useState, useEffect } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ContractDto, UpdateContractRequest } from '../types/contract.types';
import { updateContract, getContractTypes } from '../services/contractApi';
import { getPartners } from '../../partners/services/partnerApi';

interface EditContractModalProps {
  contract: ContractDto;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (updated: ContractDto) => void;
}

export const EditContractModal: FC<EditContractModalProps> = ({
  contract,
  isOpen,
  onClose,
  onSuccess,
}) => {
  // Format ISO date string to YYYY-MM-DD for date inputs
  const formatDateForInput = (dateString?: string | null) => {
    if (!dateString) return '';
    try {
      return new Date(dateString).toISOString().split('T')[0];
    } catch {
      return '';
    }
  };

  const [title, setTitle] = useState(contract.title);
  const [contractNumber, setContractNumber] = useState(contract.contractNumber);
  const [contractTypeId, setContractTypeId] = useState(contract.contractTypeId);
  const [partnerId, setPartnerId] = useState(contract.partnerId);
  const [templateVersionUsedId, setTemplateVersionUsedId] = useState(contract.templateVersionUsedId);
  const [value, setValue] = useState(contract.value);
  const [effectiveDate, setEffectiveDate] = useState(formatDateForInput(contract.effectiveDate));
  const [expiryDate, setExpiryDate] = useState(formatDateForInput(contract.expiryDate));
  const [fileUrl, setFileUrl] = useState(contract.fileUrl || '');
  const [validationError, setValidationError] = useState<string | null>(null);

  // Load contract types for selection
  const { data: contractTypes = [] } = useQuery({
    queryKey: ['contract-types'],
    queryFn: getContractTypes,
    staleTime: 60000,
  });

  // Load partners for selection
  const { data: partnersData } = useQuery({
    queryKey: ['partners-dropdown'],
    queryFn: () => getPartners({ pageSize: 100 }),
    staleTime: 60000,
  });
  const partners = partnersData?.items || [];

  // Sync state whenever contract changes or modal reopens
  useEffect(() => {
    if (isOpen) {
      setTitle(contract.title);
      setContractNumber(contract.contractNumber);
      setContractTypeId(contract.contractTypeId);
      setPartnerId(contract.partnerId);
      setTemplateVersionUsedId(contract.templateVersionUsedId);
      setValue(contract.value);
      setEffectiveDate(formatDateForInput(contract.effectiveDate));
      setExpiryDate(formatDateForInput(contract.expiryDate));
      setFileUrl(contract.fileUrl || '');
      setValidationError(null);
    }
  }, [contract, isOpen]);

  const updateMutation = useMutation({
    mutationFn: (request: UpdateContractRequest) => updateContract(contract.id, request),
    onSuccess: (updated) => {
      onSuccess(updated);
      onClose();
    },
    onError: (err: Error) => {
      setValidationError(err.message || 'Có lỗi xảy ra khi lưu thông tin hợp đồng.');
    },
  });

  if (!isOpen) return null;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    setValidationError(null);

    // Frontend validation
    if (!title.trim()) {
      setValidationError('Vui lòng nhập tiêu đề hợp đồng.');
      return;
    }

    if (!contractNumber.trim()) {
      setValidationError('Vui lòng nhập số hợp đồng.');
      return;
    }

    if (!effectiveDate || !expiryDate) {
      setValidationError('Vui lòng nhập đầy đủ ngày bắt đầu và kết thúc hiệu lực.');
      return;
    }

    if (new Date(expiryDate) < new Date(effectiveDate)) {
      setValidationError('Ngày kết thúc hiệu lực phải sau hoặc cùng ngày với ngày bắt đầu.');
      return;
    }

    if (value < 0) {
      setValidationError('Giá trị hợp đồng không thể âm.');
      return;
    }

    const payload: UpdateContractRequest = {
      title: title.trim(),
      contractNumber: contractNumber.trim(),
      contractTypeId,
      partnerId,
      templateVersionUsedId,
      value: Number(value),
      effectiveDate: new Date(effectiveDate).toISOString(),
      expiryDate: new Date(expiryDate).toISOString(),
      fileUrl: fileUrl.trim() || null,
    };

    updateMutation.mutate(payload);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden">
        {/* Modal Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-indigo-50 border border-indigo-200 text-indigo-600 flex items-center justify-center font-bold">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </div>
            <div>
              <h3 className="text-lg font-bold text-slate-900">Chỉnh sửa Hợp đồng (Bản nháp)</h3>
              <p className="text-xs text-slate-500">Mã định danh: <span className="font-mono">{contract.id.substring(0, 8)}...</span></p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={updateMutation.isPending}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-100 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Modal Form Body */}
        <form id="edit-contract-form" onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          {/* Validation Alert */}
          {validationError && (
            <div className="p-3.5 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-start gap-2.5">
              <svg className="w-4 h-4 text-rose-500 shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
              </svg>
              <span>{validationError}</span>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Contract Number */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Số hợp đồng <span className="text-rose-500">*</span>
              </label>
              <input
                type="text"
                value={contractNumber}
                onChange={(e) => setContractNumber(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
                required
              />
            </div>

            {/* Contract Type */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Loại hợp đồng <span className="text-rose-500">*</span>
              </label>
              <select
                value={contractTypeId}
                onChange={(e) => setContractTypeId(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-white"
                required
              >
                {contractTypes.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
                {!contractTypes.some(t => t.id === contractTypeId) && (
                  <option value={contractTypeId}>{contract.contractTypeName || 'Loại hợp đồng hiện tại'}</option>
                )}
              </select>
            </div>
          </div>

          {/* Title */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Tiêu đề hợp đồng <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Nhập tiêu đề hoặc tên gọi của hợp đồng..."
              required
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Value */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Giá trị hợp đồng (VNĐ) <span className="text-rose-500">*</span>
              </label>
              <input
                type="number"
                min="0"
                step="1000"
                value={value}
                onChange={(e) => setValue(Number(e.target.value))}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-semibold"
                required
              />
              <p className="text-[11px] text-slate-500 mt-1">
                {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value || 0)}
              </p>
            </div>

            {/* Partner */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Đối tác <span className="text-rose-500">*</span>
              </label>
              <select
                value={partnerId}
                onChange={(e) => setPartnerId(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 bg-white"
                required
              >
                {partners.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} {p.taxCode ? `(${p.taxCode})` : ''}
                  </option>
                ))}
                {!partners.some((p) => p.id === partnerId) && partnerId && (
                  <option value={partnerId}>Đối tác hiện tại ({partnerId})</option>
                )}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Effective Date */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Ngày bắt đầu hiệu lực <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                value={effectiveDate}
                onChange={(e) => setEffectiveDate(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                required
              />
            </div>

            {/* Expiry Date */}
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Ngày kết thúc hiệu lực <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                value={expiryDate}
                onChange={(e) => setExpiryDate(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                required
              />
            </div>
          </div>

          {/* File URL */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              URL tệp đính kèm (File URL)
            </label>
            <input
              type="url"
              value={fileUrl}
              onChange={(e) => setFileUrl(e.target.value)}
              placeholder="https://storage.example.com/contracts/contract.pdf"
              className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono text-xs"
            />
          </div>
        </form>

        {/* Modal Footer */}
        <div className="px-6 py-4 border-t border-slate-100 flex items-center justify-end gap-3 bg-slate-50/50">
          <button
            type="button"
            onClick={onClose}
            disabled={updateMutation.isPending}
            className="px-4 py-2 text-xs font-semibold text-slate-700 hover:bg-slate-200/70 rounded-xl transition-colors disabled:opacity-50"
          >
            Hủy bỏ
          </button>
          <button
            type="submit"
            form="edit-contract-form"
            disabled={updateMutation.isPending}
            className="inline-flex items-center gap-2 px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {updateMutation.isPending ? (
              <>
                <svg className="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                </svg>
                <span>Đang lưu...</span>
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 13l4 4L19 7" />
                </svg>
                <span>Lưu thay đổi</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
