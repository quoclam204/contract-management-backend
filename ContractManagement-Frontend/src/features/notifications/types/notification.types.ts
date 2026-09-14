export type NotificationType = 0 | 1 | 2 | 3;

export const NotificationTypeLabels: Record<NotificationType, string> = {
  0: 'Yêu cầu duyệt',
  1: 'Yêu cầu ký số',
  2: 'Hợp đồng sắp hết hạn',
  3: 'Thông báo hệ thống',
};

export interface NotificationDto {
  id: string;
  userId: string;
  contractId?: string | null;
  type: NotificationType;
  typeName: string;
  isRead: boolean;
  createdAt: string;
}

export interface MarkNotificationsReadRequest {
  notificationIds: string[];
}
