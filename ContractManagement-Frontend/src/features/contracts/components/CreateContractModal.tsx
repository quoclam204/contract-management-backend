import type { FC, FormEvent } from 'react';
import { useState, useEffect } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { CreateContractRequest, ContractDto } from '../types/contract.types';
import { createContract, getContractTypes } from '../services/contractApi';
import { getPartners } from '../../partners/services/partnerApi';
import { getStoredUser } from '../../../api/client';

interface CreateContractModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (newContract: ContractDto) => void;
}

export const CreateContractModal: FC<CreateContractModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [title, setTitle] = useState('');
  const [contractNumber, setContractNumber] = useState('');
  const [contractTypeId, setContractTypeId] = useState('');
  const [partnerId, setPartnerId] = useState('');
  const [value, setValue] = useState<number>(0);
  const [effectiveDate, setEffectiveDate] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [fileUrl, setFileUrl] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  // Load contract types
  const { data: contractTypes = [] } = useQuery({
    queryKey: ['contract-types'],
    queryFn: getContractTypes,
    staleTime: 60000,
  });

  // Load partners
  const { data: partnersData } = useQuery({
    queryKey: ['partners-dropdown'],
    queryFn: () => getPartners({ pageSize: 100 }),
    staleTime: 60000,
  });

  const partners = partnersData?.items || [];

  useEffect(() => {
    if (contractTypes.length > 0 && !contractTypeId) {
      setContractTypeId(contractTypes[0].id);
    }
  }, [contractTypes, contractTypeId]);

  useEffect(() => {
    if (partners.length > 0 && !partnerId) {
      setPartnerId(partners[0].id);
    }
  }, [partners, partnerId]);

  useEffect(() => {
    if (isOpen) {
      // Auto-generate contract number suggestion
      const randomNum = Math.floor(1000 + Math.random() * 9000);
      const year = new Date().getFullYear();
      setContractNumber(`HD-${year}-${randomNum}`);
      
      const today = new Date().toISOString().split('T')[0];
      const nextYear = new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
      setEffectiveDate(today);
      setExpiryDate(nextYear);
      setValidationError(null);
    }
  }, [isOpen]);

  const createMutation = useMutation({
    mutationFn: (request: CreateContractRequest) => createContract(request),
    onSuccess: (created) => {
      onSuccess(created);
      onClose();
    },
    onError: (err: Error) => {
      setValidationError(err.message || 'Có lỗi xảy ra khi tạo hợp đồng.');
    },
  });

  if (!isOpen) return null;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    setValidationError(null);

    if (!title.trim()) {
      setValidationError('Vui lòng nhập tiêu đề hợp đồng.');
      return;
    }

    if (!contractNumber.trim()) {
      setValidationError('Vui lòng nhập số hợp đồng.');
      return;
    }

    if (!contractTypeId) {
      setValidationError('Vui lòng chọn loại hợp đồng.');
      return;
    }

    if (!partnerId) {
      setValidationError('Vui lòng chọn đối tác.');
      return;
    }

    if (!effectiveDate || !expiryDate) {
      setValidationError('Vui lòng nhập đầy đủ ngày hiệu lực và ngày hết hạn.');
      return;
    }

    if (new Date(expiryDate) < new Date(effectiveDate)) {
      setValidationError('Ngày hết hạn phải sau hoặc cùng ngày với ngày bắt đầu hiệu lực.');
      return;
    }

    const currentUser = getStoredUser();
    const ownerId = currentUser?.id || '11111111-1111-1111-1111-111111111111';
    const templateVersionUsedId = '00000000-0000-0000-0000-000000000000';

    const payload: CreateContractRequest = {
      contractNumber: contractNumber.trim(),
      contractTypeId,
      templateVersionUsedId,
      partnerId,
      ownerId,
      title: title.trim(),
      value: Number(value),
      effectiveDate: new Date(effectiveDate).toISOString(),
      expiryDate: new Date(expiryDate).toISOString(),
      fileUrl: fileUrl.trim() || null,
    };

    createMutation.mutate(payload);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-blue-50 border border-blue-200 text-blue-600 flex items-center justify-center font-bold">
              +
            </div>
            <div>
              <h3 className="text-lg font-bold text-slate-900">Tạo mới Hợp đồng</h3>
              <p className="text-xs text-slate-500">Khởi tạo hợp đồng ở trạng thái Bản nháp (Draft)</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={createMutation.isPending}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-100 transition-colors"
          >
            &times;
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-4">
          {validationError && (
            <div className="p-3.5 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-start gap-2.5">
              <span>{validationError}</span>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Số hợp đồng <span className="text-rose-500">*</span>
              </label>
              <input
                type="text"
                value={contractNumber}
                onChange={(e) => setContractNumber(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono"
                required
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Loại hợp đồng <span className="text-rose-500">*</span>
              </label>
              <select
                value={contractTypeId}
                onChange={(e) => setContractTypeId(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                required
              >
                <option value="" disabled>-- Chọn loại hợp đồng --</option>
                {contractTypes.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Tiêu đề hợp đồng <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="VD: Hợp đồng cung cấp dịch vụ công nghệ thông tin năm 2026..."
              required
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Đối tác <span className="text-rose-500">*</span>
              </label>
              <select
                value={partnerId}
                onChange={(e) => setPartnerId(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
                required
              >
                <option value="" disabled>-- Chọn đối tác --</option>
                {partners.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name} {p.taxCode ? `(${p.taxCode})` : ''}
                  </option>
                ))}
              </select>
            </div>

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
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500 font-semibold"
                required
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Ngày bắt đầu hiệu lực <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                value={effectiveDate}
                onChange={(e) => setEffectiveDate(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Ngày kết thúc hiệu lực <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                value={expiryDate}
                onChange={(e) => setExpiryDate(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Đường dẫn tệp tài liệu (tùy chọn)
            </label>
            <input
              type="text"
              value={fileUrl}
              onChange={(e) => setFileUrl(e.target.value)}
              placeholder="https://storage.example.com/contracts/doc.pdf"
              className="w-full px-3 py-2 text-sm rounded-lg border border-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="flex items-center justify-end space-x-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-slate-700 bg-white border border-slate-300 rounded-lg hover:bg-slate-50"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 shadow-sm"
            >
              {createMutation.isPending ? 'Đang tạo...' : 'Tạo hợp đồng'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
