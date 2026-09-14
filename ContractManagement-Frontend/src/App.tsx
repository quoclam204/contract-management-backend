import { useState } from 'react';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { ContractListPage } from './features/contracts/pages/ContractListPage';
import { ContractDetailPage } from './features/contracts/pages/ContractDetailPage';
import { WorkflowConfigPage } from './features/workflows/pages/WorkflowConfigPage';
import { ApprovalWaitingListPage } from './features/workflows/pages/ApprovalWaitingListPage';
import { getPendingApprovals } from './features/workflows/services/workflowApi';
import './App.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 5000,
    },
  },
});

type AppTab = 'contracts' | 'workflows' | 'approvals';

function AppLayout() {
  const [activeTab, setActiveTab] = useState<AppTab>('contracts');
  const [selectedContractId, setSelectedContractId] = useState<string | null>(null);

  // Live query for pending approvals count badge
  const { data: pendingApprovals = [] } = useQuery({
    queryKey: ['pending-approvals'],
    queryFn: () => getPendingApprovals(),
    refetchInterval: 15000,
  });

  const pendingCount = pendingApprovals.length;

  const handleNavigateToContract = (contractId: string) => {
    setSelectedContractId(contractId);
    setActiveTab('contracts');
  };

  return (
    <div className="min-h-screen flex flex-col bg-slate-50 text-slate-900">
      {/* Navigation Header */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-30 shadow-xs">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16 items-center">
            {/* Logo & Brand */}
            <button
              type="button"
              onClick={() => {
                setSelectedContractId(null);
                setActiveTab('contracts');
              }}
              className="flex items-center gap-3 text-left hover:opacity-90 transition-opacity cursor-pointer"
            >
              <div className="w-10 h-10 rounded-xl bg-indigo-600 flex items-center justify-center text-white shadow-sm shadow-indigo-200 shrink-0">
                <svg
                  className="w-6 h-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
              </div>
              <div>
                <span className="text-lg font-bold text-slate-900 tracking-tight flex items-center gap-2">
                  ContractFlow
                  <span className="text-xs font-medium px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200">
                    Modular Monolith
                  </span>
                </span>
                <span className="block text-xs text-slate-500">
                  Hệ thống Quản lý Vòng đời & Phê duyệt Hợp đồng
                </span>
              </div>
            </button>

            {/* Navigation Tabs */}
            <nav className="hidden md:flex items-center gap-1.5">
              {/* Contracts Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('contracts');
                }}
                className={`px-3.5 py-2 text-sm font-semibold rounded-lg flex items-center gap-2 cursor-pointer transition-colors ${
                  activeTab === 'contracts'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
                </svg>
                <span>Hợp đồng</span>
              </button>

              {/* Workflows Config Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('workflows');
                }}
                className={`px-3.5 py-2 text-sm font-semibold rounded-lg flex items-center gap-2 cursor-pointer transition-colors ${
                  activeTab === 'workflows'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
                </svg>
                <span>Cấu hình Luồng duyệt</span>
              </button>

              {/* Pending Approvals Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('approvals');
                }}
                className={`px-3.5 py-2 text-sm font-semibold rounded-lg flex items-center gap-2 cursor-pointer transition-colors ${
                  activeTab === 'approvals'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Chờ duyệt</span>
                {pendingCount > 0 && (
                  <span className="ml-0.5 px-2 py-0.5 text-[11px] font-bold rounded-full bg-amber-500 text-white animate-pulse shadow-xs">
                    {pendingCount}
                  </span>
                )}
              </button>
            </nav>

            {/* User / Environment badge */}
            <div className="flex items-center gap-3">
              <div className="text-right hidden sm:block">
                <div className="text-xs font-semibold text-slate-700">API: Connected</div>
                <div className="text-[11px] text-emerald-600 font-mono flex items-center justify-end gap-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
                  Clean Architecture
                </div>
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1">
        {activeTab === 'contracts' && (
          selectedContractId ? (
            <ContractDetailPage
              contractId={selectedContractId}
              onBack={() => setSelectedContractId(null)}
            />
          ) : (
            <ContractListPage
              onSelectContract={(id) => setSelectedContractId(id)}
            />
          )
        )}

        {activeTab === 'workflows' && (
          <WorkflowConfigPage />
        )}

        {activeTab === 'approvals' && (
          <ApprovalWaitingListPage
            onSelectContract={handleNavigateToContract}
          />
        )}
      </main>

      {/* Footer */}
      <footer className="bg-white border-t border-slate-200 py-6 text-center text-xs text-slate-500">
        <div className="max-w-7xl mx-auto px-4">
          Contract Management System &copy; {new Date().getFullYear()} &bull; Target: .NET 10.0 + React 19 &bull; Clean Architecture & State Machine Verified
        </div>
      </footer>
    </div>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppLayout />
    </QueryClientProvider>
  );
}

export default App;
