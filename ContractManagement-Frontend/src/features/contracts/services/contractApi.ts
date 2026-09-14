import type { ContractDto, UpdateContractRequest, ContractTypeDto, ContractApprovalProgressDto } from '../types/contract.types';

const API_BASE_URL = '/api';

/**
 * Lấy danh sách hợp đồng từ backend
 */
export async function getContracts(): Promise<ContractDto[]> {
  const response = await fetch(`${API_BASE_URL}/contracts`, {
    headers: {
      'Accept': 'application/json',
    },
  });

  if (!response.ok) {
    const errorBody = await response.text();
    throw new Error(`Lỗi tải danh sách hợp đồng (${response.status}): ${errorBody || response.statusText}`);
  }

  return response.json();
}

/**
 * Lấy thông tin chi tiết một hợp đồng theo ID
 */
export async function getContractById(contractId: string): Promise<ContractDto> {
  const response = await fetch(`${API_BASE_URL}/contracts/${contractId}`, {
    headers: {
      'Accept': 'application/json',
    },
  });

  if (!response.ok) {
    let errorMessage = `Không thể tải thông tin hợp đồng (${response.status})`;
    try {
      const errorJson = await response.json();
      if (errorJson.message) errorMessage = errorJson.message;
      else if (errorJson.error) errorMessage = errorJson.error;
    } catch {
      // ignore
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Cập nhật thông tin hợp đồng (Chỉ hỗ trợ khi ở trạng thái Draft)
 */
export async function updateContract(contractId: string, data: UpdateContractRequest): Promise<ContractDto> {
  const response = await fetch(`${API_BASE_URL}/contracts/${contractId}`, {
    method: 'PUT',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(data),
  });

  if (!response.ok) {
    let errorMessage = `Lỗi cập nhật hợp đồng (${response.status})`;
    try {
      const errorJson = await response.json();
      if (errorJson.error) errorMessage = errorJson.error;
      else if (errorJson.message) errorMessage = errorJson.message;
    } catch {
      const errorText = await response.text();
      if (errorText) errorMessage = errorText;
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Đệ trình hợp đồng vào quy trình duyệt (Draft -> PendingApproval)
 */
export async function submitContract(contractId: string): Promise<{ message: string }> {
  const response = await fetch(`${API_BASE_URL}/contracts/${contractId}/submit`, {
    method: 'POST',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    let errorMessage = `Không thể gửi duyệt hợp đồng (${response.status})`;
    try {
      const errorJson = await response.json();
      if (errorJson.error) errorMessage = errorJson.error;
      else if (errorJson.message) errorMessage = errorJson.message;
    } catch {
      const errorText = await response.text();
      if (errorText) errorMessage = errorText;
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

/**
 * Lấy danh sách loại hợp đồng để hiển thị dropdown
 */
export async function getContractTypes(): Promise<ContractTypeDto[]> {
  try {
    const response = await fetch(`${API_BASE_URL}/contract-types`, {
      headers: {
        'Accept': 'application/json',
      },
    });

    if (!response.ok) {
      return [];
    }

    return response.json();
  } catch {
    return [];
  }
}

/**
 * Lấy tiến trình phê duyệt của hợp đồng (nếu đã gửi duyệt)
 */
export async function getApprovalProgress(contractId: string): Promise<ContractApprovalProgressDto | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/approvals/contract/${contractId}`, {
      headers: {
        'Accept': 'application/json',
      },
    });

    if (!response.ok) {
      return null;
    }

    return response.json();
  } catch {
    return null;
  }
}
