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
  const params = new URLSearchParams();
  if (isActive !== undefined) params.append('isActive', String(isActive));
  if (search) params.append('search', search);

  const url = `${API_BASE_URL}/workflows${params.toString() ? `?${params.toString()}` : ''}`;
  const response = await fetch(url, {
    headers: { Accept: 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Lỗi tải danh sách quy trình duyệt (${response.status})`);
  }

  return response.json();
}

/**
 * Lấy chi tiết một luồng duyệt theo ID
 */
export async function getWorkflowById(id: string): Promise<WorkflowDefinitionDto> {
  const response = await fetch(`${API_BASE_URL}/workflows/${id}`, {
    headers: { Accept: 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Không tìm thấy luồng duyệt (${response.status})`);
  }

  return response.json();
}

/**
 * Tạo mới luồng duyệt
 */
export async function createWorkflow(data: CreateWorkflowDefinitionRequest): Promise<WorkflowDefinitionDto> {
  const response = await fetch(`${API_BASE_URL}/workflows`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    let message = `Lỗi tạo luồng duyệt (${response.status})`;
    try {
      const err = await response.json();
      if (err.error) message = err.error;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return response.json();
}

/**
 * Tạo phiên bản mới cho luồng duyệt (Versioning)
 */
export async function createNewWorkflowVersion(
  id: string,
  data: CreateWorkflowVersionRequest
): Promise<WorkflowDefinitionDto> {
  const response = await fetch(`${API_BASE_URL}/workflows/${id}/versions`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    let message = `Lỗi tạo phiên bản mới (${response.status})`;
    try {
      const err = await response.json();
      if (err.error) message = err.error;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return response.json();
}

/**
 * Cập nhật cấu hình luồng duyệt
 */
export async function updateWorkflow(
  id: string,
  data: UpdateWorkflowDefinitionRequest
): Promise<WorkflowDefinitionDto> {
  const response = await fetch(`${API_BASE_URL}/workflows/${id}`, {
    method: 'PUT',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    let message = `Lỗi cập nhật luồng duyệt (${response.status})`;
    try {
      const err = await response.json();
      if (err.error) message = err.error;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return response.json();
}

/**
 * Bật/Tắt hoạt động của luồng duyệt
 */
export async function toggleWorkflowStatus(id: string, isActive: boolean): Promise<WorkflowDefinitionDto> {
  const response = await fetch(`${API_BASE_URL}/workflows/${id}/status?isActive=${isActive}`, {
    method: 'PATCH',
    headers: { Accept: 'application/json' },
  });

  if (!response.ok) {
    let message = `Lỗi thay đổi trạng thái (${response.status})`;
    try {
      const err = await response.json();
      if (err.error) message = err.error;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return response.json();
}

/**
 * Xóa luồng duyệt chưa từng được sử dụng
 */
export async function deleteWorkflow(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/workflows/${id}`, {
    method: 'DELETE',
    headers: { Accept: 'application/json' },
  });

  if (!response.ok) {
    let message = `Lỗi xóa luồng duyệt (${response.status})`;
    try {
      const err = await response.json();
      if (err.error) message = err.error;
    } catch {
      // ignore
    }
    throw new Error(message);
  }
}

/**
 * Lấy danh sách các bước phê duyệt đang chờ xử lý
 */
export async function getPendingApprovals(approverId?: string): Promise<PendingApprovalItemDto[]> {
  const url = `${API_BASE_URL}/approvals/pending${approverId ? `?approverId=${approverId}` : ''}`;
  const response = await fetch(url, {
    headers: { Accept: 'application/json' },
  });

  if (!response.ok) {
    throw new Error(`Lỗi tải danh sách chờ duyệt (${response.status})`);
  }

  return response.json();
}

/**
 * Ra quyết định phê duyệt (Approve) hoặc Từ chối (Reject)
 */
export async function processApprovalDecision(
  data: ProcessApprovalDecisionRequest
): Promise<ContractApprovalProgressDto> {
  const response = await fetch(`${API_BASE_URL}/approvals/decision`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    let message = `Lỗi ra quyết định phê duyệt (${response.status})`;
    try {
      const err = await response.json();
      if (err.error) message = err.error;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return response.json();
}

/**
 * Lấy tiến trình phê duyệt của hợp đồng
 */
export async function getApprovalProgress(contractId: string): Promise<ContractApprovalProgressDto | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/approvals/contract/${contractId}`, {
      headers: { Accept: 'application/json' },
    });

    if (!response.ok) {
      return null;
    }

    return response.json();
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
  const response = await fetch(`${API_BASE_URL}/workflows/evaluate-condition`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ expression, contractValue }),
  });

  if (!response.ok) {
    throw new Error(`Lỗi kiểm tra biểu thức (${response.status})`);
  }

  return response.json();
}

/**
 * Ký kết hợp đồng điện tử (Sign)
 */
export async function signContract(data: CreateSignatureRequest): Promise<SignatureDto> {
  const response = await fetch(`${API_BASE_URL}/signatures`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    let message = `Lỗi ký hợp đồng (${response.status})`;
    try {
      const err = await response.json();
      if (err.error) message = err.error;
    } catch {
      // ignore
    }
    throw new Error(message);
  }

  return response.json();
}

/**
 * Lấy danh sách chữ ký đã ký của hợp đồng
 */
export async function getSignaturesByContractId(contractId: string): Promise<SignatureDto[]> {
  try {
    const response = await fetch(`${API_BASE_URL}/signatures/contract/${contractId}`, {
      headers: { Accept: 'application/json' },
    });
    if (!response.ok) return [];
    return response.json();
  } catch {
    return [];
  }
}

/**
 * Lấy trạng thái tiến độ ký kết của các bên
 */
export async function getSignatureStatus(contractId: string): Promise<ContractSignatureStatusDto | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/signatures/contract/${contractId}/status`, {
      headers: { Accept: 'application/json' },
    });
    if (!response.ok) return null;
    return response.json();
  } catch {
    return null;
  }
}
