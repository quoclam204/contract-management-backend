export const ContractStatus = {
  Draft: 0,
  PendingApproval: 1,
  Approved: 2,
  Signed: 3,
  Active: 4,
  Expiring: 5,
  Renewed: 6,
  Terminated: 7,
} as const;

export type ContractStatus = (typeof ContractStatus)[keyof typeof ContractStatus];

export interface ContractDto {
  id: string;
  contractNumber: string;
  contractTypeId: string;
  contractTypeName?: string | null;
  templateVersionUsedId: string;
  partnerId: string;
  ownerId: string;
  title: string;
  value: number;
  signedDate?: string | null;
  effectiveDate: string;
  expiryDate: string;
  status: ContractStatus;
  fileUrl?: string | null;
  parentContractId?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateContractRequest {
  contractNumber: string;
  contractTypeId: string;
  templateVersionUsedId: string;
  partnerId: string;
  ownerId: string;
  title: string;
  value: number;
  signedDate?: string | null;
  effectiveDate: string;
  expiryDate: string;
  fileUrl?: string | null;
  parentContractId?: string | null;
}

export interface UpdateContractRequest {
  contractNumber?: string;
  contractTypeId?: string;
  templateVersionUsedId?: string;
  partnerId?: string;
  ownerId?: string;
  title?: string;
  value?: number;
  signedDate?: string | null;
  effectiveDate?: string;
  expiryDate?: string;
  fileUrl?: string | null;
  parentContractId?: string | null;
}

export interface ContractTypeDto {
  id: string;
  name: string;
  createdAt: string;
}

export interface ApprovalStepDto {
  id: string;
  contractId: string;
  workflowDefinitionId: string;
  stepOrder: number;
  approverRole: number;
  approverRoleName?: string;
  approverId: string;
  decision: number;
  decisionName?: string;
  comment?: string | null;
  decidedAt?: string | null;
  createdAt: string;
}

export interface ContractApprovalProgressDto {
  contractId: string;
  workflowDefinitionId: string;
  workflowName: string;
  workflowVersion: number;
  overallStatus: string;
  currentPendingStepOrder: number;
  totalSteps: number;
  approvedStepsCount: number;
  steps: ApprovalStepDto[];
}

export interface ContractStatusInfo {
  label: string;
  description: string;
  badgeClass: string;
  dotColor: string;
}

export const CONTRACT_STATUS_MAP: Record<ContractStatus, ContractStatusInfo> = {
  [ContractStatus.Draft]: {
    label: 'Bản nháp',
    description: 'Đang soạn thảo, chưa gửi duyệt',
    badgeClass: 'bg-slate-100 text-slate-700 border border-slate-300',
    dotColor: 'bg-slate-400',
  },
  [ContractStatus.PendingApproval]: {
    label: 'Chờ phê duyệt',
    description: 'Đang trong tiến trình phê duyệt đa bước',
    badgeClass: 'bg-amber-50 text-amber-800 border border-amber-300',
    dotColor: 'bg-amber-500',
  },
  [ContractStatus.Approved]: {
    label: 'Đã phê duyệt',
    description: 'Quy trình phê duyệt hoàn tất, chờ ký',
    badgeClass: 'bg-sky-50 text-sky-800 border border-sky-300',
    dotColor: 'bg-sky-500',
  },
  [ContractStatus.Signed]: {
    label: 'Đã ký kết',
    description: 'Các bên đã ký, chờ ngày kích hoạt',
    badgeClass: 'bg-indigo-50 text-indigo-800 border border-indigo-300',
    dotColor: 'bg-indigo-500',
  },
  [ContractStatus.Active]: {
    label: 'Đang hiệu lực',
    description: 'Hợp đồng đang trong thời gian thực thi',
    badgeClass: 'bg-emerald-50 text-emerald-800 border border-emerald-300',
    dotColor: 'bg-emerald-500',
  },
  [ContractStatus.Expiring]: {
    label: 'Sắp hết hạn',
    description: 'Gần đến thời hạn kết thúc hiệu lực',
    badgeClass: 'bg-orange-50 text-orange-800 border border-orange-300',
    dotColor: 'bg-orange-500',
  },
  [ContractStatus.Renewed]: {
    label: 'Đã gia hạn',
    description: 'Đã được gia hạn sang chu kỳ mới',
    badgeClass: 'bg-blue-50 text-blue-800 border border-blue-300',
    dotColor: 'bg-blue-500',
  },
  [ContractStatus.Terminated]: {
    label: 'Đã chấm dứt',
    description: 'Đã thanh lý hoặc chấm dứt hiệu lực',
    badgeClass: 'bg-rose-50 text-rose-800 border border-rose-300',
    dotColor: 'bg-rose-500',
  },
};
