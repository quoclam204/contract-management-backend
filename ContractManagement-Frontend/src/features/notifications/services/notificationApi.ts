import { apiGet, apiPost } from '../../../api/client';
import type { NotificationDto, MarkNotificationsReadRequest } from '../types/notification.types';

export const notificationApi = {
  getMyNotifications: (isRead?: boolean): Promise<NotificationDto[]> => {
    const query = isRead !== undefined ? `?isRead=${isRead}` : '';
    return apiGet<NotificationDto[]>(`/api/notifications${query}`);
  },

  getUnreadCount: (): Promise<number> => {
    return apiGet<number>('/api/notifications/unread-count');
  },

  getUnreadNotifications: (): Promise<NotificationDto[]> => {
    return apiGet<NotificationDto[]>('/api/notifications/unread');
  },

  markAsRead: (notificationIds: string[]): Promise<number> => {
    const payload: MarkNotificationsReadRequest = { notificationIds };
    return apiPost<number>('/api/notifications/mark-read', payload);
  },

  markAllAsRead: (): Promise<number> => {
    return apiPost<number>('/api/notifications/mark-all-read', {});
  },
};
