import type { FC, FormEvent } from 'react';
import { useState, useEffect } from 'react';
import type {
  WorkflowDefinitionDto,
  CreateWorkflowStepRequest,
  CreateWorkflowDefinitionRequest,
  CreateWorkflowVersionRequest,
} from '../types/workflow.types';
import { ApproverRole, APPROVER_ROLE_MAP } from '../types/workflow.types';

interface WorkflowModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (payload: CreateWorkflowDefinitionRequest | CreateWorkflowVersionRequest, isNewVersion: boolean) => void;
  workflowToEdit?: WorkflowDefinitionDto | null;
  isLoading: boolean;
}

export const WorkflowModal: FC<WorkflowModalProps> = ({
  isOpen,
  onClose,
  onSave,
  workflowToEdit,
  isLoading,
}) => {
  const isNewVersionMode = !!workflowToEdit;

  const [name, setName] = useState('');
  const [conditionExpression, setConditionExpression] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [steps, setSteps] = useState<CreateWorkflowStepRequest[]>([
    { stepOrder: 1, approverRole: ApproverRole.Manager, isRequired: true },
  ]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      if (workflowToEdit) {
        setName(workflowToEdit.name);
        setConditionExpression(workflowToEdit.conditionExpression || '');
        setIsActive(true);
        setSteps(
          workflowToEdit.steps.length > 0
            ? workflowToEdit.steps.map((s) => ({
                stepOrder: s.stepOrder,
                approverRole: s.approverRole,
                isRequired: s.isRequired,
              }))
            : [{ stepOrder: 1, approverRole: ApproverRole.Manager, isRequired: true }]
        );
      } else {
        setName('');
        setConditionExpression('');
        setIsActive(true);
        setSteps([
          { stepOrder: 1, approverRole: ApproverRole.Manager, isRequired: true },
        ]);
      }
      setError(null);
    }
  }, [isOpen, workflowToEdit]);

  if (!isOpen) return null;

  const handleAddStep = () => {
    const nextOrder = steps.length + 1;
    setSteps([
      ...steps,
      {
        stepOrder: nextOrder,
        approverRole: ApproverRole.Approver,
        isRequired: true,
      },
    ]);
  };

  const handleRemoveStep = (index: number) => {
    if (steps.length <= 1) {
      setError('Quy trình duyệt phải có tối thiểu 1 bước.');
      return;
    }
    const updated = steps.filter((_, i) => i !== index);
    // Re-index step orders
    const reordered = updated.map((s, idx) => ({ ...s, stepOrder: idx + 1 }));
    setSteps(reordered);
  };

  const handleMoveStep = (index: number, direction: 'up' | 'down') => {
    const targetIndex = direction === 'up' ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= steps.length) return;

    const copy = [...steps];
    const temp = copy[index];
    copy[index] = copy[targetIndex];
    copy[targetIndex] = temp;

    // Reassign step order based on new index
    const reordered = copy.map((s, idx) => ({ ...s, stepOrder: idx + 1 }));
    setSteps(reordered);
  };

  const handleRoleChange = (index: number, role: ApproverRole) => {
    const copy = [...steps];
    copy[index] = { ...copy[index], approverRole: role };
    setSteps(copy);
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim() && !isNewVersionMode) {
      setError('Vui lòng nhập tên quy trình duyệt.');
      return;
    }

    if (steps.length === 0) {
      setError('Quy trình phải có ít nhất 1 bước duyệt.');
      return;
    }

    if (isNewVersionMode) {
      const payload: CreateWorkflowVersionRequest = {
        conditionExpression: conditionExpression.trim() || null,
        steps,
      };
      onSave(payload, true);
    } else {
      const payload: CreateWorkflowDefinitionRequest = {
        name: name.trim(),
        conditionExpression: conditionExpression.trim() || null,
        isActive,
        steps,
      };
      onSave(payload, false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/70">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-indigo-50 border border-indigo-200 text-indigo-600 flex items-center justify-center">
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
              </svg>
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                {isNewVersionMode ? `Tạo Phiên bản Mới (v${workflowToEdit.version + 1})` : 'Tạo Cấu hình Luồng duyệt Mới'}
              </h3>
              <p className="text-xs text-slate-500">
                {isNewVersionMode
                  ? `Kế thừa từ quy trình: "${workflowToEdit.name}" (phiên bản cũ sẽ tự động lưu trữ)`
                  : 'Định nghĩa các bước duyệt theo thứ tự tuần tự và điều kiện giá trị hợp đồng'}
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isLoading}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-100 transition-colors"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Body */}
        <form id="workflow-form" onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-5">
          {error && (
            <div className="p-3.5 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-xl flex items-center gap-2">
              <svg className="w-4 h-4 text-rose-500 shrink-0" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
              <span>{error}</span>
            </div>
          )}

          {/* Workflow Name */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Tên quy trình duyệt <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              value={name}
              disabled={isNewVersionMode}
              onChange={(e) => setName(e.target.value)}
              placeholder="VD: Luồng duyệt hợp đồng dự án lớn (> 500 triệu)..."
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:bg-slate-100 disabled:text-slate-500"
              required
            />
          </div>

          {/* Condition Expression */}
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">
              Điều kiện áp dụng theo giá trị hợp đồng (Condition Expression)
            </label>
            <input
              type="text"
              value={conditionExpression}
              onChange={(e) => setConditionExpression(e.target.value)}
              placeholder="VD: Value >= 500000000 hoặc Value >= 100000000 AND Value < 500000000 (để trống nếu áp dụng mặc định)"
              className="w-full px-3 py-2 text-xs rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500 font-mono"
            />
            <div className="flex items-center gap-2 mt-2 flex-wrap text-[11px] text-slate-500">
              <span>Mẫu gợi ý nhanh:</span>
              <button
                type="button"
                onClick={() => setConditionExpression('Value < 100000000')}
                className="px-2 py-0.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md border border-slate-200"
              >
                &lt; 100M
              </button>
              <button
                type="button"
                onClick={() => setConditionExpression('Value >= 100000000 AND Value < 500000000')}
                className="px-2 py-0.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md border border-slate-200"
              >
                100M - 500M
              </button>
              <button
                type="button"
                onClick={() => setConditionExpression('Value >= 500000000')}
                className="px-2 py-0.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md border border-slate-200"
              >
                &gt;= 500M
              </button>
              <button
                type="button"
                onClick={() => setConditionExpression('')}
                className="px-2 py-0.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-md border border-slate-200"
              >
                Mặc định (Không điều kiện)
              </button>
            </div>
          </div>

          {/* Sequential Steps Builder */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="block text-xs font-semibold text-slate-700">
                Các bước phê duyệt tuần tự ({steps.length} bước) <span className="text-rose-500">*</span>
              </label>
              <button
                type="button"
                onClick={handleAddStep}
                className="inline-flex items-center gap-1 text-xs text-indigo-600 hover:text-indigo-800 font-semibold cursor-pointer"
              >
                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
                </svg>
                <span>Thêm bước</span>
              </button>
            </div>

            <div className="space-y-2.5">
              {steps.map((step, index) => {
                const roleInfo = APPROVER_ROLE_MAP[step.approverRole];
                return (
                  <div
                    key={index}
                    className="p-3 bg-slate-50 border border-slate-200 rounded-xl flex items-center justify-between gap-3 text-xs"
                  >
                    <div className="flex items-center gap-3">
                      <span className="w-6 h-6 rounded-full bg-indigo-600 text-white font-bold flex items-center justify-center text-xs shrink-0">
                        {step.stepOrder}
                      </span>
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="text-slate-500 font-medium">Bước {step.stepOrder}:</span>
                        <select
                          value={step.approverRole}
                          onChange={(e) => handleRoleChange(index, Number(e.target.value) as ApproverRole)}
                          className="px-2.5 py-1.5 rounded-lg border border-slate-300 bg-white font-semibold text-slate-800 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                        >
                          <option value={ApproverRole.Staff}>{APPROVER_ROLE_MAP[ApproverRole.Staff].label}</option>
                          <option value={ApproverRole.Manager}>{APPROVER_ROLE_MAP[ApproverRole.Manager].label}</option>
                          <option value={ApproverRole.Approver}>{APPROVER_ROLE_MAP[ApproverRole.Approver].label}</option>
                          <option value={ApproverRole.Admin}>{APPROVER_ROLE_MAP[ApproverRole.Admin].label}</option>
                        </select>
                        <span className={`px-2 py-0.5 rounded-full border text-[11px] font-medium ${roleInfo.badgeClass}`}>
                          {roleInfo.label.split(' ')[0]}
                        </span>
                      </div>
                    </div>

                    <div className="flex items-center gap-1 shrink-0">
                      {/* Move Up */}
                      <button
                        type="button"
                        disabled={index === 0}
                        onClick={() => handleMoveStep(index, 'up')}
                        title="Di chuyển lên trước"
                        className="p-1 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-200 disabled:opacity-30 transition-colors"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 15l7-7 7 7" />
                        </svg>
                      </button>

                      {/* Move Down */}
                      <button
                        type="button"
                        disabled={index === steps.length - 1}
                        onClick={() => handleMoveStep(index, 'down')}
                        title="Di chuyển xuống sau"
                        className="p-1 rounded-md text-slate-400 hover:text-slate-700 hover:bg-slate-200 disabled:opacity-30 transition-colors"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 9l-7 7-7-7" />
                        </svg>
                      </button>

                      {/* Delete */}
                      <button
                        type="button"
                        disabled={steps.length <= 1}
                        onClick={() => handleRemoveStep(index)}
                        title="Xóa bước này"
                        className="p-1 rounded-md text-rose-400 hover:text-rose-600 hover:bg-rose-50 disabled:opacity-30 transition-colors"
                      >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </form>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-slate-100 flex items-center justify-end gap-3 bg-slate-50/70">
          <button
            type="button"
            onClick={onClose}
            disabled={isLoading}
            className="px-4 py-2 text-xs font-semibold text-slate-600 hover:bg-slate-200/70 rounded-xl transition-colors"
          >
            Hủy bỏ
          </button>
          <button
            type="submit"
            form="workflow-form"
            disabled={isLoading}
            className="inline-flex items-center gap-2 px-5 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 rounded-xl shadow-xs transition-all disabled:opacity-50"
          >
            {isLoading ? (
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
                <span>{isNewVersionMode ? 'Tạo phiên bản mới' : 'Tạo luồng duyệt'}</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
