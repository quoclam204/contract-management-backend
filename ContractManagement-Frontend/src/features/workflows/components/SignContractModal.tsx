import type { FC, FormEvent } from 'react';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ContractDto } from '../../contracts/types/contract.types';
import type { CreateSignatureRequest } from '../types/workflow.types';
import { SignerType, SignatureMethod } from '../types/workflow.types';
import { signContract } from '../services/workflowApi';

interface SignContractModalProps {
  isOpen: boolean;
  onClose: () => void;
  contract: ContractDto;
  onSuccess?: () => void;
}

export const SignContractModal: FC<SignContractModalProps> = ({
  isOpen,
  onClose,
  contract,
  onSuccess,
}) => {
  const queryClient = useQueryClient();
  const [signerType, setSignerType] = useState<SignerType>(SignerType.InternalUser);
  const [signerName, setSignerName] = useState('');
  const [signatureMethod, setSignatureMethod] = useState<SignatureMethod>(SignatureMethod.Mock);
  const [otpCode, setOtpCode] = useState('123456');
  const [error, setError] = useState<string | null>(null);

  const signMutation = useMutation({
    mutationFn: (payload: CreateSignatureRequest) => signContract(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contract', contract.id] });
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      queryClient.invalidateQueries({ queryKey: ['signatures', contract.id] });
      queryClient.invalidateQueries({ queryKey: ['signature-status', contract.id] });
      if (onSuccess) onSuccess();
      onClose();
    },
    onError: (err: Error) => {
      setError(err.message || 'Lỗi khi thực hiện ký kết.');
    },
  });

  if (!isOpen) return null;

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!signerName.trim()) {
      setError('Vui lòng nhập tên người ký.');
      return;
    }

    if (signatureMethod === SignatureMethod.Otp && !otpCode.trim()) {
      setError('Vui lòng nhập mã OTP.');
      return;
    }

    const payload: CreateSignatureRequest = {
      contractId: contract.id,
      signerType,
      internalSignerId: signerType === SignerType.InternalUser ? contract.ownerId : null,
      partnerSignerId: signerType === SignerType.PartnerRepresentative ? contract.partnerId : null,
      signerName: signerName.trim(),
      signatureMethod,
      otpCode: signatureMethod === SignatureMethod.Otp ? otpCode.trim() : null,
    };

    signMutation.mutate(payload);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-md overflow-hidden">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-indigo-50/50">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-indigo-100 text-indigo-700 flex items-center justify-center font-bold">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
              </svg>
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Ký kết Hợp đồng Điện tử</h3>
              <p className="text-xs text-slate-500">Mã HĐ: <span className="font-mono">{contract.contractNumber}</span></p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={signMutation.isPending}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-100 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 text-xs">
          {error && (
            <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded-xl">
              {error}
            </div>
          )}

          {/* Signer Type */}
          <div>
            <label className="block font-semibold text-slate-700 mb-1">
              Bên tham gia ký kết <span className="text-rose-500">*</span>
            </label>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => {
                  setSignerType(SignerType.InternalUser);
                  setSignerName('Đại diện Nội bộ');
                }}
                className={`p-2.5 rounded-xl border text-center font-medium transition-all ${
                  signerType === SignerType.InternalUser
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700 font-semibold'
                    : 'border-slate-200 hover:bg-slate-50 text-slate-600'
                }`}
              >
                1. Nội bộ doanh nghiệp
              </button>
              <button
                type="button"
                onClick={() => {
                  setSignerType(SignerType.PartnerRepresentative);
                  setSignerName('Đại diện Đối tác');
                }}
                className={`p-2.5 rounded-xl border text-center font-medium transition-all ${
                  signerType === SignerType.PartnerRepresentative
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700 font-semibold'
                    : 'border-slate-200 hover:bg-slate-50 text-slate-600'
                }`}
              >
                2. Đại diện Đối tác
              </button>
            </div>
          </div>

          {/* Signer Name */}
          <div>
            <label className="block font-semibold text-slate-700 mb-1">
              Họ và tên người ký <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              value={signerName}
              onChange={(e) => setSignerName(e.target.value)}
              placeholder="VD: Nguyễn Văn A..."
              className="w-full px-3 py-2 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              required
            />
          </div>

          {/* Signature Method */}
          <div>
            <label className="block font-semibold text-slate-700 mb-1">
              Phương thức xác thực ký <span className="text-rose-500">*</span>
            </label>
            <div className="grid grid-cols-2 gap-2">
              <label
                className={`p-2.5 rounded-xl border flex items-center gap-2 cursor-pointer ${
                  signatureMethod === SignatureMethod.Mock
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700 font-semibold'
                    : 'border-slate-200 text-slate-600'
                }`}
              >
                <input
                  type="radio"
                  name="sigMethod"
                  checked={signatureMethod === SignatureMethod.Mock}
                  onChange={() => setSignatureMethod(SignatureMethod.Mock)}
                  className="text-indigo-600"
                />
                <span>Mock (Ký giả lập nhanh)</span>
              </label>

              <label
                className={`p-2.5 rounded-xl border flex items-center gap-2 cursor-pointer ${
                  signatureMethod === SignatureMethod.Otp
                    ? 'border-indigo-600 bg-indigo-50 text-indigo-700 font-semibold'
                    : 'border-slate-200 text-slate-600'
                }`}
              >
                <input
                  type="radio"
                  name="sigMethod"
                  checked={signatureMethod === SignatureMethod.Otp}
                  onChange={() => setSignatureMethod(SignatureMethod.Otp)}
                  className="text-indigo-600"
                />
                <span>OTP (Xác thực 6 số)</span>
              </label>
            </div>
          </div>

          {/* OTP Input (if OTP selected) */}
          {signatureMethod === SignatureMethod.Otp && (
            <div>
              <label className="block font-semibold text-slate-700 mb-1">
                Mã xác thực OTP (6 chữ số) <span className="text-rose-500">*</span>
              </label>
              <input
                type="text"
                maxLength={6}
                value={otpCode}
                onChange={(e) => setOtpCode(e.target.value)}
                placeholder="Nhập mã OTP (VD: 123456)..."
                className="w-full px-3 py-2 rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono text-center tracking-widest text-sm font-bold"
                required
              />
              <span className="text-[11px] text-slate-400 block mt-1">
                Gợi ý thử nghiệm: Nhập 123456 để ký thành công.
              </span>
            </div>
          )}

          <div className="pt-2 flex items-center justify-end gap-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={signMutation.isPending}
              className="px-4 py-2 font-semibold text-slate-600 hover:bg-slate-100 rounded-xl transition-colors"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={signMutation.isPending}
              className="inline-flex items-center gap-1.5 px-5 py-2 font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 rounded-xl shadow-xs transition-all disabled:opacity-50"
            >
              {signMutation.isPending ? (
                <>
                  <svg className="animate-spin h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                  </svg>
                  <span>Đang ký...</span>
                </>
              ) : (
                <>
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                  </svg>
                  <span>Xác nhận Ký kết</span>
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
