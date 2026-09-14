import { apiGet, apiUpload } from '../../../api/client';
import type { AttachmentDto } from '../types/attachment.types';

export async function getAttachmentsByContract(contractId: string): Promise<AttachmentDto[]> {
  try {
    return await apiGet<AttachmentDto[]>(`/api/v1/contracts/${contractId}/attachments`);
  } catch {
    return [];
  }
}

export async function uploadAttachment(contractId: string, file: File): Promise<AttachmentDto> {
  const formData = new FormData();
  formData.append('file', file);
  return apiUpload<AttachmentDto>(`/api/v1/contracts/${contractId}/attachments`, formData);
}

export function getAttachmentDownloadUrl(attachmentId: string): string {
  return `/api/v1/attachments/${attachmentId}/download`;
}
