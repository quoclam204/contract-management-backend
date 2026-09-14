import { apiGet, apiPost, apiPut, apiDelete } from '../../../api/client';
import type {
  ContractDto,
  CreateContractRequest,
  UpdateContractRequest,
  ContractTypeDto,
  ContractApprovalProgressDto,
} from '../types/contract.types';

/**
 * Lấy danh sách hợp đồng từ backend
 */
export async function getContracts(params?: {
  search?: string;
  status?: number;
  contractTypeId?: string;
}): Promise<ContractDto[]> {
  return apiGet<ContractDto[]>('/api/contracts', {
    search: params?.search,
    status: params?.status,
    contractTypeId: params?.contractTypeId,
  });
}

/**
 * Lấy thông tin chi tiết một hợp đồng theo ID
 */
export async function getContractById(contractId: string): Promise<ContractDto> {
  return apiGet<ContractDto>(`/api/contracts/${contractId}`);
}

/**
 * Tạo mới hợp đồng
 */
export async function createContract(data: CreateContractRequest): Promise<ContractDto> {
  return apiPost<ContractDto>('/api/contracts', data);
}

/**
 * Cập nhật thông tin hợp đồng (Chỉ hỗ trợ khi ở trạng thái Draft)
 */
export async function updateContract(contractId: string, data: UpdateContractRequest): Promise<ContractDto> {
  return apiPut<ContractDto>(`/api/contracts/${contractId}`, data);
}

/**
 * Xóa hợp đồng (nếu được hỗ trợ ở trạng thái Draft)
 */
export async function deleteContract(contractId: string): Promise<void> {
  return apiDelete<void>(`/api/contracts/${contractId}`);
}

/**
 * Đệ trình hợp đồng vào quy trình duyệt (Draft -> PendingApproval)
 */
export async function submitContract(contractId: string): Promise<{ message: string }> {
  return apiPost<{ message: string }>(`/api/contracts/${contractId}/submit`);
}

/**
 * Lấy danh sách loại hợp đồng để hiển thị dropdown
 */
export async function getContractTypes(): Promise<ContractTypeDto[]> {
  try {
    return await apiGet<ContractTypeDto[]>('/api/contract-types');
  } catch {
    return [];
  }
}

/**
 * Lấy tiến trình phê duyệt của hợp đồng (nếu đã gửi duyệt)
 */
export async function getApprovalProgress(contractId: string): Promise<ContractApprovalProgressDto | null> {
  try {
    return await apiGet<ContractApprovalProgressDto>(`/api/approvals/contract/${contractId}`);
  } catch {
    return null;
  }
}
