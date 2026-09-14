import React, { useState } from 'react';
import { aiApi } from '../services/aiApi';
import { RiskLevelColors, RiskLevelLabels } from '../types/ai.types';
import type {
  ContractSummaryDto,
  ContractRiskAnalysisDto,
  ExtractedContractInfoDto,
} from '../types/ai.types';

interface AIAssistantModalProps {
  isOpen: boolean;
  onClose: () => void;
  contractId: string;
  contractTitle: string;
  initialContent?: string;
}

type AIFeature = 'summarize' | 'risk' | 'extract';

export const AIAssistantModal: React.FC<AIAssistantModalProps> = ({
  isOpen,
  onClose,
  contractId,
  contractTitle,
  initialContent = '',
}) => {
  const [feature, setFeature] = useState<AIFeature>('summarize');
  const [content, setContent] = useState<string>(
    initialContent || `HỢP ĐỒNG: ${contractTitle}\nMÃ: ${contractId}\nNỘI DUNG: Hợp đồng cung cấp dịch vụ và giải pháp phần mềm giữa các bên liên quan.`
  );
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const [summaryResult, setSummaryResult] = useState<ContractSummaryDto | null>(null);
  const [riskResult, setRiskResult] = useState<ContractRiskAnalysisDto | null>(null);
  const [extractResult, setExtractResult] = useState<ExtractedContractInfoDto | null>(null);

  if (!isOpen) return null;

  const handleExecute = async () => {
    setIsLoading(true);
    setError(null);
    try {
      if (feature === 'summarize') {
        const res = await aiApi.summarize({ contractId, contractContent: content });
        setSummaryResult(res);
      } else if (feature === 'risk') {
        const res = await aiApi.analyzeRisk({ contractId, contractContent: content });
        setRiskResult(res);
      } else if (feature === 'extract') {
        const res = await aiApi.extractInfo({ contractId, contractContent: content });
        setExtractResult(res);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Đã có lỗi xảy ra khi gọi AI Assistant');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div className="relative w-full max-w-3xl rounded-xl bg-white shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 bg-gradient-to-r from-indigo-600 to-purple-600 text-white">
          <div className="flex items-center space-x-2">
            <span className="text-xl">✨</span>
            <h2 className="text-lg font-bold text-white">Trợ lý AI Hợp đồng</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-white hover:text-gray-200 text-2xl font-bold p-1 leading-none"
          >
            &times;
          </button>
        </div>

        {/* Subheader / Tabs */}
        <div className="flex border-b border-gray-200 bg-gray-50 px-6 pt-2">
          <button
            type="button"
            onClick={() => setFeature('summarize')}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              feature === 'summarize'
                ? 'border-indigo-600 text-indigo-600 font-semibold'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            📝 Tóm tắt nội dung
          </button>
          <button
            type="button"
            onClick={() => setFeature('risk')}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              feature === 'risk'
                ? 'border-indigo-600 text-indigo-600 font-semibold'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            ⚠️ Đánh giá rủi ro
          </button>
          <button
            type="button"
            onClick={() => setFeature('extract')}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              feature === 'extract'
                ? 'border-indigo-600 text-indigo-600 font-semibold'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            🔍 Trích xuất thông tin
          </button>
        </div>

        {/* Content Area */}
        <div className="p-6 overflow-y-auto flex-1 space-y-4">
          <div>
            <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
              Văn bản / Điều khoản hợp đồng để AI phân tích:
            </label>
            <textarea
              rows={4}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Nhập hoặc dán nội dung điều khoản hợp đồng..."
              className="w-full text-sm rounded-lg border border-gray-300 p-3 focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 font-mono"
            />
          </div>

          <div className="flex justify-end">
            <button
              type="button"
              disabled={isLoading || !content.trim()}
              onClick={handleExecute}
              className="inline-flex items-center px-4 py-2 rounded-lg bg-indigo-600 text-white text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-sm transition-all"
            >
              {isLoading ? (
                <>
                  <svg
                    className="animate-spin -ml-1 mr-2 h-4 w-4 text-white"
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
                  Đang phân tích với AI...
                </>
              ) : (
                'Bắt đầu phân tích'
              )}
            </button>
          </div>

          {error && (
            <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-sm text-red-600">
              {error}
            </div>
          )}

          {/* Results Area */}
          {feature === 'summarize' && summaryResult && (
            <div className="rounded-lg border border-indigo-100 bg-indigo-50/50 p-4 space-y-3">
              <h4 className="font-semibold text-indigo-900 text-sm flex items-center gap-1">
                <span>📋</span> Kết quả tóm tắt:
              </h4>
              <p className="text-sm text-gray-800 leading-relaxed whitespace-pre-wrap">
                {summaryResult.summary}
              </p>
              {summaryResult.keyPoints?.length > 0 && (
                <div>
                  <h5 className="text-xs font-bold text-gray-700 uppercase tracking-wider mb-2">
                    Các điểm chính:
                  </h5>
                  <ul className="list-disc list-inside space-y-1 text-sm text-gray-700">
                    {summaryResult.keyPoints.map((pt, i) => (
                      <li key={i}>{pt}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}

          {feature === 'risk' && riskResult && (
            <div className="space-y-3">
              <h4 className="font-semibold text-gray-800 text-sm flex items-center justify-between">
                <span>Danh sách rủi ro phát hiện ({riskResult.risks.length}):</span>
              </h4>
              {riskResult.risks.length === 0 ? (
                <div className="p-4 rounded-lg bg-green-50 text-green-700 text-sm">
                  Không tìm thấy rủi ro đáng kể trong nội dung đã cung cấp.
                </div>
              ) : (
                <div className="space-y-2">
                  {riskResult.risks.map((risk, index) => (
                    <div
                      key={index}
                      className={`p-3 rounded-lg border text-sm ${
                        RiskLevelColors[risk.level] || 'bg-gray-50 border-gray-200 text-gray-800'
                      }`}
                    >
                      <div className="flex items-center justify-between mb-1">
                        <span className="font-bold">{risk.title}</span>
                        <span className="text-xs px-2 py-0.5 rounded font-semibold border">
                          Mức độ: {RiskLevelLabels[risk.level] || risk.levelName}
                        </span>
                      </div>
                      <p className="text-xs mt-1 leading-relaxed opacity-90">{risk.description}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {feature === 'extract' && extractResult && (
            <div className="rounded-lg border border-gray-200 bg-white p-4">
              <h4 className="font-semibold text-gray-800 text-sm mb-3">
                Thông tin trích xuất:
              </h4>
              <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-2 text-sm">
                <div>
                  <dt className="text-xs text-gray-500">Mã hợp đồng</dt>
                  <dd className="font-medium text-gray-900">{extractResult.contractNumber || '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Tiêu đề</dt>
                  <dd className="font-medium text-gray-900">{extractResult.title || '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Đối tác</dt>
                  <dd className="font-medium text-gray-900">{extractResult.partner || '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Loại hợp đồng</dt>
                  <dd className="font-medium text-gray-900">{extractResult.contractType || '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Giá trị</dt>
                  <dd className="font-medium text-gray-900">
                    {extractResult.value !== undefined && extractResult.value !== null
                      ? `${extractResult.value.toLocaleString('vi-VN')} VND`
                      : '—'}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Trạng thái</dt>
                  <dd className="font-medium text-gray-900">{extractResult.status || '—'}</dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Ngày ký</dt>
                  <dd className="font-medium text-gray-900">
                    {extractResult.signedDate ? new Date(extractResult.signedDate).toLocaleDateString('vi-VN') : '—'}
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-gray-500">Ngày hết hạn</dt>
                  <dd className="font-medium text-gray-900">
                    {extractResult.expiryDate ? new Date(extractResult.expiryDate).toLocaleDateString('vi-VN') : '—'}
                  </dd>
                </div>
              </dl>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-3 bg-gray-50 border-t border-gray-200 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 shadow-sm"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
};
