import { useState } from 'react';
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';
import { ContractListPage } from './features/contracts/pages/ContractListPage';
import { ContractDetailPage } from './features/contracts/pages/ContractDetailPage';
import { WorkflowConfigPage } from './features/workflows/pages/WorkflowConfigPage';
import { ApprovalWaitingListPage } from './features/workflows/pages/ApprovalWaitingListPage';
import { DashboardPage } from './features/dashboard/pages/DashboardPage';
import { PartnerListPage } from './features/partners/pages/PartnerListPage';
import { UserManagementPage } from './features/users/pages/UserManagementPage';
import { DepartmentListPage } from './features/departments/pages/DepartmentListPage';
import { NotificationDropdown } from './features/notifications/components/NotificationDropdown';
import { LoginModal } from './features/auth/components/LoginModal';
import { useAuth } from './features/auth/hooks/useAuth';
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

type AppTab =
  | 'dashboard'
  | 'contracts'
  | 'workflows'
  | 'approvals'
  | 'partners'
  | 'users'
  | 'departments';

function AppLayout() {
  const [activeTab, setActiveTab] = useState<AppTab>('dashboard');
  const [selectedContractId, setSelectedContractId] = useState<string | null>(null);
  const [isLoginModalOpen, setIsLoginModalOpen] = useState<boolean>(false);

  const { user, isAuthenticated } = useAuth();

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
          <div className="flex justify-between h-16 items-center gap-2">
            {/* Logo & Brand */}
            <button
              type="button"
              onClick={() => {
                setSelectedContractId(null);
                setActiveTab('dashboard');
              }}
              className="flex items-center gap-3 text-left hover:opacity-90 transition-opacity cursor-pointer shrink-0"
            >
              <div className="w-9 h-9 rounded-xl bg-indigo-600 flex items-center justify-center text-white shadow-sm shadow-indigo-200 shrink-0">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
              </div>
              <div className="hidden lg:block">
                <span className="text-base font-bold text-slate-900 tracking-tight flex items-center gap-2">
                  ContractFlow
                  <span className="text-[10px] font-medium px-1.5 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200">
                    Enterprise
                  </span>
                </span>
                <span className="block text-[11px] text-slate-500">
                  Hệ thống Quản lý Vòng đời & Phê duyệt Hợp đồng
                </span>
              </div>
            </button>

            {/* Navigation Tabs */}
            <nav className="flex items-center gap-1 overflow-x-auto py-1">
              {/* Dashboard Tab */}
              <button
                type="button"
                onClick={() => {
                  setSelectedContractId(null);
                  setActiveTab('dashboard');
                }}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg flex items-center gap-1.5 cursor-pointer transition-colors whitespace-nowrap ${
                  activeTab === 'dashboard'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <span>📊</span>
                <span>Tổng quan</span>
              </button>

              {/* Contracts Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('contracts');
                }}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg flex items-center gap-1.5 cursor-pointer transition-colors whitespace-nowrap ${
                  activeTab === 'contracts'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <span>📄</span>
                <span>Hợp đồng</span>
              </button>

              {/* Workflows Config Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('workflows');
                }}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg flex items-center gap-1.5 cursor-pointer transition-colors whitespace-nowrap ${
                  activeTab === 'workflows'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <span>⚙️</span>
                <span>Luồng duyệt</span>
              </button>

              {/* Pending Approvals Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('approvals');
                }}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg flex items-center gap-1.5 cursor-pointer transition-colors whitespace-nowrap ${
                  activeTab === 'approvals'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <span>⏳</span>
                <span>Chờ duyệt</span>
                {pendingCount > 0 && (
                  <span className="ml-0.5 px-1.5 py-0.2 text-[10px] font-bold rounded-full bg-amber-500 text-white animate-pulse shadow-xs">
                    {pendingCount}
                  </span>
                )}
              </button>

              {/* Partners Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('partners');
                }}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg flex items-center gap-1.5 cursor-pointer transition-colors whitespace-nowrap ${
                  activeTab === 'partners'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <span>🏢</span>
                <span>Đối tác</span>
              </button>

              {/* Users Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('users');
                }}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg flex items-center gap-1.5 cursor-pointer transition-colors whitespace-nowrap ${
                  activeTab === 'users'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <span>👥</span>
                <span>Người dùng</span>
              </button>

              {/* Departments Tab */}
              <button
                type="button"
                onClick={() => {
                  setActiveTab('departments');
                }}
                className={`px-3 py-1.5 text-xs font-semibold rounded-lg flex items-center gap-1.5 cursor-pointer transition-colors whitespace-nowrap ${
                  activeTab === 'departments'
                    ? 'bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-xs'
                    : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100'
                }`}
              >
                <span>🏛️</span>
                <span>Phòng ban</span>
              </button>
            </nav>

            {/* Header Right: Notifications & User Profile */}
            <div className="flex items-center gap-2 shrink-0">
              {/* Notification Dropdown */}
              <NotificationDropdown onSelectContract={handleNavigateToContract} />

              {/* Auth Profile / Login Button */}
              {isAuthenticated && user ? (
                <button
                  type="button"
                  onClick={() => setIsLoginModalOpen(true)}
                  className="flex items-center gap-2 px-2.5 py-1.5 rounded-lg hover:bg-slate-100 transition-colors text-left"
                  title="Thông tin tài khoản"
                >
                  <div className="w-7 h-7 rounded-full bg-blue-600 text-white font-bold flex items-center justify-center text-xs">
                    {user.fullName ? user.fullName[0].toUpperCase() : 'U'}
                  </div>
                  <div className="hidden xl:block">
                    <span className="block text-xs font-semibold text-slate-800 leading-tight">
                      {user.fullName}
                    </span>
                    <span className="block text-[10px] text-slate-400 leading-tight">
                      {user.roleName || `Role #${user.role}`}
                    </span>
                  </div>
                </button>
              ) : (
                <button
                  type="button"
                  onClick={() => setIsLoginModalOpen(true)}
                  className="px-3 py-1.5 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm transition-colors"
                >
                  Đăng nhập
                </button>
              )}
            </div>
          </div>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 max-w-7xl mx-auto w-full p-4 sm:p-6 lg:p-8">
        {activeTab === 'dashboard' && <DashboardPage />}

        {activeTab === 'contracts' &&
          (selectedContractId ? (
            <ContractDetailPage
              contractId={selectedContractId}
              onBack={() => setSelectedContractId(null)}
            />
          ) : (
            <ContractListPage onSelectContract={(id) => setSelectedContractId(id)} />
          ))}

        {activeTab === 'workflows' && <WorkflowConfigPage />}

        {activeTab === 'approvals' && (
          <ApprovalWaitingListPage onSelectContract={handleNavigateToContract} />
        )}

        {activeTab === 'partners' && <PartnerListPage />}

        {activeTab === 'users' && <UserManagementPage />}

        {activeTab === 'departments' && <DepartmentListPage />}
      </main>

      {/* Footer */}
      <footer className="bg-white border-t border-slate-200 py-6 text-center text-xs text-slate-500">
        <div className="max-w-7xl mx-auto px-4">
          Contract Management System &copy; {new Date().getFullYear()} &bull; Target: .NET 10.0 + React 19 &bull; Clean Architecture & State Machine Verified
        </div>
      </footer>

      {/* Login / Profile Modal */}
      <LoginModal isOpen={isLoginModalOpen} onClose={() => setIsLoginModalOpen(false)} />
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
