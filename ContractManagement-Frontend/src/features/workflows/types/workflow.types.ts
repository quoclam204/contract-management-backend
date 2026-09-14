export const ApproverRole = {
  Admin: 0,
  Manager: 1,
  Staff: 2,
  Approver: 3,
} as const;

export type ApproverRole = (typeof ApproverRole)[keyof typeof ApproverRole];

export const APPROVER_ROLE_MAP: Record<ApproverRole, { label: string; badgeClass: string }> = {
  [ApproverRole.Admin]: {
    label: 'Quản trị viên (Admin)',
    badgeClass: 'bg-purple-50 text-purple-700 border-purple-200',
  },
  [ApproverRole.Manager]: {
    label: 'Trưởng phòng (Manager)',
    badgeClass: 'bg-blue-50 text-blue-700 border-blue-200',
  },
  [ApproverRole.Staff]: {
    label: 'Nhân viên (Staff)',
    badgeClass: 'bg-slate-100 text-slate-700 border-slate-200',
  },
  [ApproverRole.Approver]: {
    label: 'Người phê duyệt (Approver)',
    badgeClass: 'bg-indigo-50 text-indigo-700 border-indigo-200',
  },
};

export const ApprovalDecision = {
  Pending: 0,
  Approved: 1,
  Rejected: 2,
} as const;

export type ApprovalDecision = (typeof ApprovalDecision)[keyof typeof ApprovalDecision];

export interface WorkflowStepDto {
  id: string;
  workflowDefinitionId: string;
  stepOrder: number;
  approverRole: ApproverRole;
  approverRoleName?: string;
  isRequired: boolean;
}

export interface WorkflowDefinitionDto {
  id: string;
  name: string;
  conditionExpression?: string | null;
  version: number;
  isActive: boolean;
  createdAt: string;
  steps: WorkflowStepDto[];
}

export interface CreateWorkflowStepRequest {
  stepOrder: number;
  approverRole: ApproverRole;
  isRequired: boolean;
}

export interface CreateWorkflowDefinitionRequest {
  name: string;
  conditionExpression?: string | null;
  isActive: boolean;
  steps: CreateWorkflowStepRequest[];
}

export interface CreateWorkflowVersionRequest {
  conditionExpression?: string | null;
  steps: CreateWorkflowStepRequest[];
}

export interface UpdateWorkflowDefinitionRequest {
  conditionExpression?: string | null;
  isActive?: boolean;
}

export interface PendingApprovalItemDto {
  approvalStepId: string;
  contractId: string;
  workflowDefinitionId: string;
  workflowName: string;
  stepOrder: number;
  approverRole: ApproverRole;
  approverRoleName?: string;
  approverId: string;
  createdAt: string;
}

export interface ProcessApprovalDecisionRequest {
  approvalStepId: string;
  approverId: string;
  decision: ApprovalDecision;
  comment?: string | null;
}

export interface ApprovalStepDetailDto {
  id: string;
  contractId: string;
  workflowDefinitionId: string;
  stepOrder: number;
  approverRole: ApproverRole;
  approverRoleName?: string;
  approverId: string;
  decision: ApprovalDecision;
  decisionName: string;
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
  currentPendingStepOrder: number | null;
  totalSteps: number;
  approvedStepsCount: number;
  steps: ApprovalStepDetailDto[];
}

export interface EvaluateConditionResponse {
  isValidSyntax: boolean;
  errorMessage?: string | null;
  isSatisfied: boolean;
}

export const SignerType = {
  InternalUser: 0,
  PartnerRepresentative: 1,
} as const;

export type SignerType = (typeof SignerType)[keyof typeof SignerType];

export const SignatureMethod = {
  Mock: 0,
  Otp: 1,
  DigitalCa: 2,
} as const;

export type SignatureMethod = (typeof SignatureMethod)[keyof typeof SignatureMethod];

export interface CreateSignatureRequest {
  contractId: string;
  signerType: SignerType;
  internalSignerId?: string | null;
  partnerSignerId?: string | null;
  signerName: string;
  signatureMethod: SignatureMethod;
  otpCode?: string | null;
}

export interface SignatureDto {
  id: string;
  contractId: string;
  signerType: SignerType;
  signerTypeName: string;
  internalSignerId?: string | null;
  partnerSignerId?: string | null;
  signerNameSnapshot: string;
  signatureMethod: SignatureMethod;
  signatureMethodName: string;
  signedAt: string;
  signatureHash: string;
}

export interface ContractSignatureStatusDto {
  contractId: string;
  hasInternalSignature: boolean;
  hasPartnerSignature: boolean;
  isFullySigned: boolean;
  totalRequiredParties: number;
  signedPartiesCount: number;
  signatures: SignatureDto[];
}
