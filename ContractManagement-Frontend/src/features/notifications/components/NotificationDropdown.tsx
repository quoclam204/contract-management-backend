import React, { useEffect, useState, useRef } from 'react';
import { notificationApi } from '../services/notificationApi';
import { NotificationTypeLabels } from '../types/notification.types';
import type { NotificationDto } from '../types/notification.types';

interface NotificationDropdownProps {
  onSelectContract?: (contractId: string) => void;
}

export const NotificationDropdown: React.FC<NotificationDropdownProps> = ({ onSelectContract }) => {
  const [isOpen, setIsOpen] = useState<boolean>(false);
  const [unreadCount, setUnreadCount] = useState<number>(0);
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const dropdownRef = useRef<HTMLDivElement>(null);

  const fetchUnreadCount = async () => {
    try {
      const count = await notificationApi.getUnreadCount();
      setUnreadCount(count);
    } catch {
      // Keep silent for background badge fetch
    }
  };

  const fetchNotifications = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await notificationApi.getMyNotifications();
      setNotifications(data);
      const unread = data.filter((n) => !n.isRead).length;
      setUnreadCount(unread);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Không thể tải thông báo');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchUnreadCount();
    const interval = setInterval(fetchUnreadCount, 30000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (isOpen) {
      fetchNotifications();
    }
  }, [isOpen]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleMarkAsRead = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await notificationApi.markAsRead([id]);
      setNotifications((prev) =>
        prev.map((item) => (item.id === id ? { ...item, isRead: true } : item))
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch (err) {
      console.error('Failed to mark notification as read', err);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await notificationApi.markAllAsRead();
      setNotifications((prev) => prev.map((item) => ({ ...item, isRead: true })));
      setUnreadCount(0);
    } catch (err) {
      console.error('Failed to mark all notifications as read', err);
    }
  };

  const handleItemClick = (item: NotificationDto) => {
    if (!item.isRead) {
      notificationApi.markAsRead([item.id]).catch(console.error);
      setNotifications((prev) =>
        prev.map((n) => (n.id === item.id ? { ...n, isRead: true } : n))
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    }
    if (item.contractId && onSelectContract) {
      onSelectContract(item.contractId);
      setIsOpen(false);
    }
  };

  return (
    <div className="relative inline-block text-left" ref={dropdownRef}>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-gray-600 hover:text-gray-900 rounded-full hover:bg-gray-100 focus:outline-none transition-colors"
        title="Thông báo"
      >
        <svg
          className="w-6 h-6"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth="2"
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
          />
        </svg>
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-bold text-white shadow-sm">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 rounded-lg bg-white shadow-xl ring-1 ring-black ring-opacity-5 z-50 overflow-hidden">
          <div className="flex items-center justify-between px-4 py-3 bg-gray-50 border-b border-gray-100">
            <h3 className="text-sm font-semibold text-gray-800">Thông báo</h3>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={handleMarkAllAsRead}
                className="text-xs text-blue-600 hover:text-blue-800 hover:underline"
              >
                Đã đọc tất cả
              </button>
            )}
          </div>

          <div className="max-h-96 overflow-y-auto divide-y divide-gray-100">
            {isLoading ? (
              <div className="py-8 text-center text-sm text-gray-500">Đang tải thông báo...</div>
            ) : error ? (
              <div className="p-4 text-center text-sm text-red-500">{error}</div>
            ) : notifications.length === 0 ? (
              <div className="py-8 text-center text-sm text-gray-400">Không có thông báo nào</div>
            ) : (
              notifications.map((item) => (
                <div
                  key={item.id}
                  onClick={() => handleItemClick(item)}
                  className={`p-4 hover:bg-gray-50 cursor-pointer transition-colors flex items-start justify-between gap-2 ${
                    !item.isRead ? 'bg-blue-50/50' : ''
                  }`}
                >
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span
                        className={`inline-block w-2 h-2 rounded-full ${
                          !item.isRead ? 'bg-blue-600' : 'bg-transparent'
                        }`}
                      />
                      <p className="text-xs font-semibold text-gray-900 truncate">
                        {NotificationTypeLabels[item.type] || item.typeName}
                      </p>
                    </div>
                    {item.contractId && (
                      <p className="text-xs text-gray-600 mt-1 truncate">
                        Hợp đồng: <span className="font-mono text-gray-800">{item.contractId.substring(0, 8)}...</span>
                      </p>
                    )}
                    <p className="text-[11px] text-gray-400 mt-1">
                      {new Date(item.createdAt).toLocaleString('vi-VN')}
                    </p>
                  </div>
                  {!item.isRead && (
                    <button
                      type="button"
                      onClick={(e) => handleMarkAsRead(item.id, e)}
                      title="Đánh dấu đã đọc"
                      className="text-xs text-gray-400 hover:text-blue-600 p-1"
                    >
                      ✓
                    </button>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};
