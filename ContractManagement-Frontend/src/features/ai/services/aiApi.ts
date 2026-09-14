import { apiPost } from '../../../api/client';
import type {
  ExtractContractRequest,
  ExtractedContractInfoDto,
  SummarizeContractRequest,
  ContractSummaryDto,
  AnalyzeContractRiskRequest,
  ContractRiskAnalysisDto,
} from '../types/ai.types';

export const aiApi = {
  extractInfo: (data: ExtractContractRequest): Promise<ExtractedContractInfoDto> => {
    return apiPost<ExtractedContractInfoDto>('/api/ai/contracts/extract', data);
  },

  summarize: (data: SummarizeContractRequest): Promise<ContractSummaryDto> => {
    return apiPost<ContractSummaryDto>('/api/ai/contracts/summarize', data);
  },

  analyzeRisk: (data: AnalyzeContractRiskRequest): Promise<ContractRiskAnalysisDto> => {
    return apiPost<ContractRiskAnalysisDto>('/api/ai/contracts/analyze-risk', data);
  },
};
