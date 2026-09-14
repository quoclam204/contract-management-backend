import { apiGet } from '../../../api/client';
import type {
  DashboardSummaryDto,
  ContractStatusSummaryDto,
  DepartmentContractSummaryDto,
  PartnerContractSummaryDto,
  MonthlyContractSummaryDto,
} from '../types/dashboard.types';

export const dashboardApi = {
  getSummary: (userId?: string): Promise<DashboardSummaryDto> => {
    const query = userId ? `?userId=${userId}` : '';
    return apiGet<DashboardSummaryDto>(`/api/dashboard/stats${query}`);
  },

  getByStatus: (userId?: string): Promise<ContractStatusSummaryDto[]> => {
    const query = userId ? `?userId=${userId}` : '';
    return apiGet<ContractStatusSummaryDto[]>(`/api/dashboard/status-distribution${query}`);
  },

  getByDepartment: (userId?: string): Promise<DepartmentContractSummaryDto[]> => {
    const query = userId ? `?userId=${userId}` : '';
    return apiGet<DepartmentContractSummaryDto[]>(`/api/dashboard/value-by-type${query}`);
  },

  getByPartner: (top: number = 5, userId?: string): Promise<PartnerContractSummaryDto[]> => {
    const params = new URLSearchParams();
    params.append('top', top.toString());
    if (userId) params.append('userId', userId);
    return apiGet<PartnerContractSummaryDto[]>(`/api/dashboard/by-partner?${params.toString()}`);
  },

  getByTime: (userId?: string): Promise<MonthlyContractSummaryDto[]> => {
    const query = userId ? `?userId=${userId}` : '';
    return apiGet<MonthlyContractSummaryDto[]>(`/api/dashboard/contract-trends${query}`);
  },
};
