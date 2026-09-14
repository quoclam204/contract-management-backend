import { apiGet, apiPost, apiPut, apiPatch, apiDelete } from '../../../api/client';
import type {
  WorkflowDefinitionDto,
  CreateWorkflowDefinitionRequest,
  CreateWorkflowVersionRequest,
  UpdateWorkflowDefinitionRequest,
  PendingApprovalItemDto,
  ProcessApprovalDecisionRequest,
  ContractApprovalProgressDto,
  EvaluateConditionResponse,
  CreateSignatureRequest,
  SignatureDto,
  ContractSignatureStatusDto,
} from '../types/workflow.types';

const API_BASE_URL = '/api';

/**
 * Lấy danh sách cấu hình luồng duyệt
 */
export async function getWorkflows(isActive?: boolean, search?: string): Promise<WorkflowDefinitionDto[]> {
  return apiGet<WorkflowDefinitionDto[]>(`${API_BASE_URL}/workflows`, {
    isActive,
    search,
  });
}

/**
 * Lấy chi tiết một luồng duyệt theo ID
 */
export async function getWorkflowById(id: string): Promise<WorkflowDefinitionDto> {
  return apiGet<WorkflowDefinitionDto>(`${API_BASE_URL}/workflows/${id}`);
}

/**
 * Tạo mới luồng duyệt
 */
export async function createWorkflow(data: CreateWorkflowDefinitionRequest): Promise<WorkflowDefinitionDto> {
  return apiPost<WorkflowDefinitionDto>(`${API_BASE_URL}/workflows`, data);
}

/**
 * Tạo phiên bản mới cho luồng duyệt (Versioning)
 */
export async function createNewWorkflowVersion(
  id: string,
  data: CreateWorkflowVersionRequest
): Promise<WorkflowDefinitionDto> {
  return apiPost<WorkflowDefinitionDto>(`${API_BASE_URL}/workflows/${id}/versions`, data);
}

/**
 * Cập nhật cấu hình luồng duyệt
 */
export async function updateWorkflow(
  id: string,
  data: UpdateWorkflowDefinitionRequest
): Promise<WorkflowDefinitionDto> {
  return apiPut<WorkflowDefinitionDto>(`${API_BASE_URL}/workflows/${id}`, data);
}

/**
 * Bật/Tắt hoạt động của luồng duyệt
 */
export async function toggleWorkflowStatus(id: string, isActive: boolean): Promise<WorkflowDefinitionDto> {
  return apiPatch<WorkflowDefinitionDto>(`${API_BASE_URL}/workflows/${id}/status`, undefined, {
    params: { isActive },
  });
}

/**
 * Xóa luồng duyệt chưa từng được sử dụng
 */
export async function deleteWorkflow(id: string): Promise<void> {
  return apiDelete<void>(`${API_BASE_URL}/workflows/${id}`);
}

/**
 * Lấy danh sách các bước phê duyệt đang chờ xử lý
 */
export async function getPendingApprovals(approverId?: string): Promise<PendingApprovalItemDto[]> {
  return apiGet<PendingApprovalItemDto[]>(`${API_BASE_URL}/approvals/pending`, { approverId });
}

/**
 * Ra quyết định phê duyệt (Approve) hoặc Từ chối (Reject)
 */
export async function processApprovalDecision(
  data: ProcessApprovalDecisionRequest
): Promise<ContractApprovalProgressDto> {
  return apiPost<ContractApprovalProgressDto>(`${API_BASE_URL}/approvals/decision`, data);
}

/**
 * Lấy tiến trình phê duyệt của hợp đồng
 */
export async function getApprovalProgress(contractId: string): Promise<ContractApprovalProgressDto | null> {
  try {
    return await apiGet<ContractApprovalProgressDto>(`${API_BASE_URL}/approvals/contract/${contractId}`);
  } catch {
    return null;
  }
}

/**
 * Kiểm tra thử nghiệm biểu thức điều kiện với giá trị hợp đồng
 */
export async function evaluateCondition(
  expression: string,
  contractValue: number
): Promise<EvaluateConditionResponse> {
  return apiPost<EvaluateConditionResponse>(`${API_BASE_URL}/workflows/evaluate-condition`, {
    expression,
    contractValue,
  });
}

/**
 * Ký kết hợp đồng điện tử (Sign)
 */
export async function signContract(data: CreateSignatureRequest): Promise<SignatureDto> {
  return apiPost<SignatureDto>(`${API_BASE_URL}/signatures`, data);
}

/**
 * Lấy danh sách chữ ký đã ký của hợp đồng
 */
export async function getSignaturesByContractId(contractId: string): Promise<SignatureDto[]> {
  try {
    return await apiGet<SignatureDto[]>(`${API_BASE_URL}/signatures/contract/${contractId}`);
  } catch {
    return [];
  }
}

/**
 * Lấy trạng thái tiến độ ký kết của các bên
 */
export async function getSignatureStatus(contractId: string): Promise<ContractSignatureStatusDto | null> {
  try {
    return await apiGet<ContractSignatureStatusDto>(`${API_BASE_URL}/signatures/contract/${contractId}/status`);
  } catch {
    return null;
  }
}
