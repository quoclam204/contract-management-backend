import React, { useState } from 'react';
import { useAuth } from '../hooks/useAuth';
import { register } from '../services/authApi';
import { UserRole } from '../types/auth.types';

interface LoginModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const LoginModal: React.FC<LoginModalProps> = ({ isOpen, onClose }) => {
  const { user, isAuthenticated, login, logout } = useAuth();
  const [isRegisterMode, setIsRegisterMode] = useState<boolean>(false);

  const [email, setEmail] = useState<string>('admin@contractflow.com');
  const [password, setPassword] = useState<string>('Password123!');
  const [fullName, setFullName] = useState<string>('Quản trị viên');
  const [role, setRole] = useState<UserRole>(UserRole.Admin);

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);
    try {
      await login({ email, password });
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Đăng nhập thất bại');
    } finally {
      setIsLoading(false);
    }
  };

  const handleRegisterSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError(null);
    try {
      await register({ email, password, fullName, role });
      await login({ email, password });
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Đăng ký thất bại');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div className="relative w-full max-w-md rounded-xl bg-white shadow-2xl overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 bg-slate-900 text-white">
          <div className="flex items-center space-x-2">
            <span className="text-xl">🔐</span>
            <h2 className="text-base font-bold text-white">
              {isAuthenticated
                ? 'Thông tin tài khoản'
                : isRegisterMode
                ? 'Đăng ký tài khoản'
                : 'Đăng nhập hệ thống'}
            </h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-white text-2xl font-bold"
          >
            &times;
          </button>
        </div>

        {isAuthenticated && user ? (
          <div className="p-6 space-y-4">
            <div className="flex items-center space-x-3 pb-4 border-b border-gray-100">
              <div className="w-12 h-12 rounded-full bg-blue-600 text-white font-bold flex items-center justify-center text-lg">
                {user.fullName ? user.fullName[0].toUpperCase() : 'U'}
              </div>
              <div>
                <h4 className="text-base font-bold text-gray-900">{user.fullName}</h4>
                <p className="text-xs text-gray-500">{user.email}</p>
              </div>
            </div>

            <dl className="divide-y divide-gray-100 text-xs">
              <div className="py-2.5 flex justify-between">
                <dt className="text-gray-500">Vai trò:</dt>
                <dd className="font-semibold text-blue-600">{user.roleName || `Role #${user.role}`}</dd>
              </div>
              <div className="py-2.5 flex justify-between">
                <dt className="text-gray-500">Phòng ban:</dt>
                <dd className="text-gray-800">{user.departmentName || 'Chưa gắn phòng ban'}</dd>
              </div>
              <div className="py-2.5 flex justify-between">
                <dt className="text-gray-500">Mã người dùng (UserId):</dt>
                <dd className="font-mono text-gray-700 truncate max-w-[200px]">{user.id}</dd>
              </div>
            </dl>

            <div className="pt-4 flex justify-between border-t border-gray-100">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 text-xs font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg"
              >
                Đóng
              </button>
              <button
                type="button"
                onClick={() => {
                  logout();
                  onClose();
                }}
                className="px-4 py-2 text-xs font-semibold text-white bg-red-600 hover:bg-red-700 rounded-lg shadow-sm"
              >
                Đăng xuất
              </button>
            </div>
          </div>
        ) : (
          <form
            onSubmit={isRegisterMode ? handleRegisterSubmit : handleLoginSubmit}
            className="p-6 space-y-4"
          >
            {error && (
              <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-xs text-red-600">
                {error}
              </div>
            )}

            {isRegisterMode && (
              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Họ và tên <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  placeholder="VD: Nguyễn Văn A"
                  className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500"
                />
              </div>
            )}

            <div>
              <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                Email <span className="text-red-500">*</span>
              </label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="admin@contractflow.com"
                className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                Mật khẩu <span className="text-red-500">*</span>
              </label>
              <input
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 focus:ring-1 focus:ring-blue-500"
              />
            </div>

            {isRegisterMode && (
              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Vai trò
                </label>
                <select
                  value={role}
                  onChange={(e) => setRole(Number(e.target.value) as UserRole)}
                  className="w-full text-sm rounded-lg border border-gray-300 px-3 py-2 bg-white focus:ring-1 focus:ring-blue-500"
                >
                  <option value={UserRole.Admin}>Admin (Quản trị viên)</option>
                  <option value={UserRole.Manager}>Trưởng phòng (Manager)</option>
                  <option value={UserRole.Staff}>Nhân viên (Staff)</option>
                  <option value={UserRole.Approver}>Người duyệt (Approver)</option>
                </select>
              </div>
            )}

            <button
              type="submit"
              disabled={isLoading}
              className="w-full py-2.5 px-4 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm disabled:opacity-50"
            >
              {isLoading
                ? 'Đang xử lý...'
                : isRegisterMode
                ? 'Đăng ký tài khoản'
                : 'Đăng nhập'}
            </button>

            <div className="pt-2 text-center text-xs text-gray-500">
              {isRegisterMode ? (
                <span>
                  Đã có tài khoản?{' '}
                  <button
                    type="button"
                    onClick={() => {
                      setIsRegisterMode(false);
                      setError(null);
                    }}
                    className="font-semibold text-blue-600 hover:underline"
                  >
                    Đăng nhập ngay
                  </button>
                </span>
              ) : (
                <span>
                  Chưa có tài khoản?{' '}
                  <button
                    type="button"
                    onClick={() => {
                      setIsRegisterMode(true);
                      setError(null);
                    }}
                    className="font-semibold text-blue-600 hover:underline"
                  >
                    Đăng ký tài khoản mới
                  </button>
                </span>
              )}
            </div>
          </form>
        )}
      </div>
    </div>
  );
};
