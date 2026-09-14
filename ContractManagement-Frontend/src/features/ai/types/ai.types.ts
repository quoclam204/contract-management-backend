export type RiskLevel = 0 | 1 | 2;

export const RiskLevelLabels: Record<RiskLevel, string> = {
  0: 'Thấp',
  1: 'Trung bình',
  2: 'Cao',
};

export const RiskLevelColors: Record<RiskLevel, string> = {
  0: 'bg-green-100 text-green-800 border-green-200',
  1: 'bg-yellow-100 text-yellow-800 border-yellow-200',
  2: 'bg-red-100 text-red-800 border-red-200',
};

export interface ExtractContractRequest {
  contractId: string;
  contractContent?: string;
}

export interface ExtractedContractInfoDto {
  contractNumber?: string;
  title?: string;
  partner?: string;
  contractType?: string;
  value?: number;
  signedDate?: string;
  effectiveDate?: string;
  expiryDate?: string;
  status?: string;
}

export interface SummarizeContractRequest {
  contractId: string;
  contractContent: string;
}

export interface ContractSummaryDto {
  contractId: string;
  summary: string;
  keyPoints: string[];
}

export interface ContractRiskDto {
  title: string;
  description: string;
  level: RiskLevel;
  levelName: string;
}

export interface AnalyzeContractRiskRequest {
  contractId: string;
  contractContent: string;
}

export interface ContractRiskAnalysisDto {
  contractId: string;
  risks: ContractRiskDto[];
}
