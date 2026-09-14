export interface DashboardSummaryDto {
  totalContracts: number;
  totalValue: number;
  activeContractsCount: number;
  activeContractsValue: number;
  expiringContractsCount: number;
  expiringContractsValue: number;
  pendingApprovalCount: number;
  pendingApprovalValue: number;
  draftCount: number;
  signedCount: number;
  terminatedCount: number;
}

export interface ContractStatusSummaryDto {
  status: number;
  statusName: string;
  count: number;
  totalValue: number;
}

export interface DepartmentContractSummaryDto {
  departmentId?: string | null;
  departmentName: string;
  count: number;
  totalValue: number;
}

export interface PartnerContractSummaryDto {
  partnerId: string;
  partnerName: string;
  count: number;
  totalValue: number;
}

export interface MonthlyContractSummaryDto {
  year: number;
  month: number;
  count: number;
  totalValue: number;
}
