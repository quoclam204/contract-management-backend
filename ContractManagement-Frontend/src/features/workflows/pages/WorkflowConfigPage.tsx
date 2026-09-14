import type { FC } from 'react';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type {
  WorkflowDefinitionDto,
  CreateWorkflowDefinitionRequest,
  CreateWorkflowVersionRequest,
} from '../types/workflow.types';
import { APPROVER_ROLE_MAP } from '../types/workflow.types';
import {
  getWorkflows,
  createWorkflow,
  createNewWorkflowVersion,
  toggleWorkflowStatus,
} from '../services/workflowApi';
import { WorkflowModal } from '../components/WorkflowModal';

export const WorkflowConfigPage: FC = () => {
  const queryClient = useQueryClient();
  const [searchKeyword, setSearchKeyword] = useState('');
  const [filterActive, setFilterActive] = useState<boolean | undefined>(undefined);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingWorkflow, setEditingWorkflow] = useState<WorkflowDefinitionDto | null>(null);
  const [toast, setToast] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Fetch workflows
  const {
    data: workflows = [],
    isLoading,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ['workflows', filterActive, searchKeyword],
    queryFn: () => getWorkflows(filterActive, searchKeyword),
  });

  // Create workflow mutation
  const createMutation = useMutation({
    mutationFn: (data: CreateWorkflowDefinitionRequest) => createWorkflow(data),
    onSuccess: (newWf) => {
      queryClient.invalidateQueries({ queryKey: ['workflows'] });
      setIsModalOpen(false);
      setToast({ type: 'success', message: `Đã tạo thành công quy trình: "${newWf.name}"` });
      setTimeout(() => setToast(null), 5000);
    },
    onError: (err: Error) => {
      setToast({ type: 'error', message: `Lỗi tạo quy trình: ${err.message}` });
      setTimeout(() => setToast(null), 7000);
    },
  });

  // Create new version mutation
  const newVersionMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateWorkflowVersionRequest }) =>
      createNewWorkflowVersion(id, data),
    onSuccess: (newVer) => {
      queryClient.invalidateQueries({ queryKey: ['workflows'] });
      setIsModalOpen(false);
      setEditingWorkflow(null);
      setToast({
        type: 'success',
        message: `Đã tạo phiên bản mới v${newVer.version} cho "${newVer.name}". Phiên bản cũ đã lưu trữ.`,
      });
      setTimeout(() => setToast(null), 5000);
    },
    onError: (err: Error) => {
      setToast({ type: 'error', message: `Lỗi tạo phiên bản mới: ${err.message}` });
      setTimeout(() => setToast(null), 7000);
    },
  });

  // Toggle active status mutation
  const toggleMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      toggleWorkflowStatus(id, isActive),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({ queryKey: ['workflows'] });
      setToast({
        type: 'success',
        message: `Đã ${updated.isActive ? 'kích hoạt' : 'tạm dừng'} quy trình "${updated.name}".`,
      });
      setTimeout(() => setToast(null), 4000);
    },
    onError: (err: Error) => {
      setToast({ type: 'error', message: `Lỗi thay đổi trạng thái: ${err.message}` });
      setTimeout(() => setToast(null), 7000);
    },
  });

  const handleOpenCreateModal = () => {
    setEditingWorkflow(null);
    setIsModalOpen(true);
  };

  const handleOpenVersionModal = (wf: WorkflowDefinitionDto) => {
    setEditingWorkflow(wf);
    setIsModalOpen(true);
  };

  const handleSaveWorkflow = (
    payload: CreateWorkflowDefinitionRequest | CreateWorkflowVersionRequest,
    isNewVersion: boolean
  ) => {
    if (isNewVersion && editingWorkflow) {
      newVersionMutation.mutate({
        id: editingWorkflow.id,
        data: payload as CreateWorkflowVersionRequest,
      });
    } else {
      createMutation.mutate(payload as CreateWorkflowDefinitionRequest);
    }
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
              Cấu hình Quy trình Phê duyệt
            </h1>
            <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200">
              Module Workflow
            </span>
          </div>
          <p className="text-xs text-slate-500 mt-1">
            Thiết lập luồng duyệt đa bước, thứ tự người duyệt theo vai trò và điều kiện giá trị hợp đồng
          </p>
        </div>

        <button
          type="button"
          onClick={handleOpenCreateModal}
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 rounded-xl shadow-xs transition-all focus:outline-none focus:ring-2 focus:ring-indigo-500 shrink-0"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
          </svg>
          <span>Tạo quy trình mới</span>
        </button>
      </div>

      {/* Search & Filter Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white p-4 rounded-2xl border border-slate-200 shadow-xs">
        <div className="relative flex-1 max-w-md">
          <input
            type="text"
            value={searchKeyword}
            onChange={(e) => setSearchKeyword(e.target.value)}
            placeholder="Tìm theo tên hoặc điều kiện giá trị..."
            className="w-full pl-9 pr-4 py-2 text-xs rounded-xl border border-slate-300 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
          <svg
            className="w-4 h-4 text-slate-400 absolute left-3 top-2.5"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setFilterActive(undefined)}
            className={`px-3 py-1.5 text-xs font-medium rounded-lg transition-colors ${
              filterActive === undefined
                ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 font-semibold'
                : 'text-slate-600 hover:bg-slate-100 border border-transparent'
            }`}
          >
            Tất cả
          </button>
          <button
            type="button"
            onClick={() => setFilterActive(true)}
            className={`px-3 py-1.5 text-xs font-medium rounded-lg transition-colors ${
              filterActive === true
                ? 'bg-emerald-50 text-emerald-700 border border-emerald-200 font-semibold'
                : 'text-slate-600 hover:bg-slate-100 border border-transparent'
            }`}
          >
            Đang hoạt động
          </button>
          <button
            type="button"
            onClick={() => setFilterActive(false)}
            className={`px-3 py-1.5 text-xs font-medium rounded-lg transition-colors ${
              filterActive === false
                ? 'bg-slate-200 text-slate-800 border border-slate-300 font-semibold'
                : 'text-slate-600 hover:bg-slate-100 border border-transparent'
            }`}
          >
            Đã tắt
          </button>
        </div>
      </div>

      {/* Loading Skeleton */}
      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5 animate-pulse">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="bg-white rounded-2xl p-6 border border-slate-200 space-y-4">
              <div className="h-6 bg-slate-200 rounded-md w-3/4" />
              <div className="h-4 bg-slate-100 rounded-md w-1/2" />
              <div className="space-y-2 pt-2">
                <div className="h-8 bg-slate-100 rounded-lg w-full" />
                <div className="h-8 bg-slate-100 rounded-lg w-full" />
              </div>
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
          <h3 className="text-base font-bold text-slate-900">Không thể tải danh sách quy trình</h3>
          <p className="text-xs text-slate-500 max-w-md mx-auto">
            {error instanceof Error ? error.message : 'Đã có lỗi xảy ra từ máy chủ.'}
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
      {!isLoading && !isError && workflows.length === 0 && (
        <div className="bg-white rounded-2xl p-12 border border-slate-200 text-center space-y-4 shadow-xs">
          <div className="w-12 h-12 rounded-2xl bg-indigo-50 border border-indigo-200 text-indigo-600 flex items-center justify-center mx-auto">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
            </svg>
          </div>
          <h3 className="text-base font-bold text-slate-900">Chưa có quy trình duyệt nào</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Hãy bắt đầu tạo luồng phê duyệt đầu tiên để tự động hóa tiến trình duyệt hợp đồng theo giá trị.
          </p>
          <button
            type="button"
            onClick={handleOpenCreateModal}
            className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-xs transition-colors"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
            </svg>
            <span>Tạo quy trình ngay</span>
          </button>
        </div>
      )}

      {/* Workflows Cards Grid */}
      {!isLoading && !isError && workflows.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {workflows.map((wf) => (
            <div
              key={wf.id}
              className={`bg-white rounded-2xl p-6 border transition-all duration-200 flex flex-col justify-between shadow-xs ${
                wf.isActive
                  ? 'border-slate-200 hover:border-indigo-300 hover:shadow-md'
                  : 'border-slate-200/60 opacity-75 bg-slate-50/50'
              }`}
            >
              <div className="space-y-4">
                {/* Top header */}
                <div className="flex items-start justify-between gap-3">
                  <div className="space-y-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-bold text-base text-slate-900 tracking-tight">
                        {wf.name}
                      </span>
                      <span className="text-[11px] font-mono px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200 font-semibold">
                        v{wf.version}
                      </span>
                      {wf.isActive ? (
                        <span className="text-[11px] px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200 font-medium flex items-center gap-1">
                          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
                          Đang hoạt động
                        </span>
                      ) : (
                        <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-100 text-slate-500 border border-slate-300 font-medium">
                          Đã lưu trữ / Tắt
                        </span>
                      )}
                    </div>
                    <span className="block text-[11px] text-slate-400 font-mono truncate">ID: {wf.id}</span>
                  </div>

                  {/* Toggle Active button */}
                  <button
                    type="button"
                    disabled={toggleMutation.isPending}
                    onClick={() => toggleMutation.mutate({ id: wf.id, isActive: !wf.isActive })}
                    title={wf.isActive ? 'Tắt quy trình này' : 'Kích hoạt quy trình này'}
                    className={`text-xs font-semibold px-2.5 py-1 rounded-lg border transition-colors shrink-0 ${
                      wf.isActive
                        ? 'text-slate-600 bg-slate-100 hover:bg-slate-200 border-slate-300'
                        : 'text-emerald-700 bg-emerald-50 hover:bg-emerald-100 border-emerald-200'
                    }`}
                  >
                    {wf.isActive ? 'Tắt' : 'Bật'}
                  </button>
                </div>

                {/* Condition Expression Badge */}
                <div className="p-3 bg-slate-50 rounded-xl border border-slate-100 flex items-center justify-between gap-2 text-xs">
                  <span className="text-slate-500 font-medium shrink-0">Điều kiện:</span>
                  {wf.conditionExpression ? (
                    <span className="font-mono font-semibold text-indigo-700 bg-indigo-50/70 px-2 py-0.5 rounded border border-indigo-100 truncate">
                      {wf.conditionExpression}
                    </span>
                  ) : (
                    <span className="text-slate-500 italic">Áp dụng mặc định (không xét giá trị)</span>
                  )}
                </div>

                {/* Sequential Approval Steps */}
                <div className="space-y-2">
                  <span className="text-xs font-semibold text-slate-700 block">
                    Tiến trình phê duyệt ({wf.steps.length} bước tuần tự):
                  </span>
                  <div className="space-y-1.5">
                    {wf.steps.map((step) => {
                      const roleInfo = APPROVER_ROLE_MAP[step.approverRole];
                      return (
                        <div
                          key={step.id}
                          className="flex items-center justify-between p-2.5 rounded-xl bg-white border border-slate-100 text-xs shadow-2xs"
                        >
                          <div className="flex items-center gap-2.5">
                            <span className="w-5 h-5 rounded-full bg-slate-100 text-slate-700 font-bold flex items-center justify-center text-[11px]">
                              {step.stepOrder}
                            </span>
                            <span className="text-slate-700 font-medium">
                              Bước {step.stepOrder}:
                            </span>
                            <span className={`px-2 py-0.5 rounded-full border text-[11px] font-semibold ${roleInfo?.badgeClass}`}>
                              {roleInfo?.label || `Role #${step.approverRole}`}
                            </span>
                          </div>
                          {step.isRequired && (
                            <span className="text-[10px] text-slate-400 font-mono">Bắt buộc</span>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="pt-4 mt-4 border-t border-slate-100 flex items-center justify-end gap-2 text-xs">
                <button
                  type="button"
                  onClick={() => handleOpenVersionModal(wf)}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl font-semibold text-indigo-600 bg-indigo-50 hover:bg-indigo-100 transition-colors border border-indigo-200"
                >
                  <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 4v16m8-8H4" />
                  </svg>
                  <span>Tạo phiên bản mới (v{wf.version + 1})</span>
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Workflow Create / Versioning Modal */}
      <WorkflowModal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingWorkflow(null);
        }}
        onSave={handleSaveWorkflow}
        workflowToEdit={editingWorkflow}
        isLoading={createMutation.isPending || newVersionMutation.isPending}
      />
    </div>
  );
};
