import { apiGet, apiPost, apiPut, apiDelete } from '../../../api/client';
import type {
  PartnerDto,
  PagedResult,
  CreatePartnerRequest,
  UpdatePartnerRequest,
} from '../types/partner.types';

const PARTNER_BASE_URL = '/api/v1/partners';

export async function getPartners(params?: {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
}): Promise<PagedResult<PartnerDto>> {
  return apiGet<PagedResult<PartnerDto>>(PARTNER_BASE_URL, {
    PageNumber: params?.pageNumber ?? 1,
    PageSize: params?.pageSize ?? 50,
    SearchTerm: params?.searchTerm ?? '',
  });
}

export async function getPartnerById(id: string): Promise<PartnerDto> {
  return apiGet<PartnerDto>(`${PARTNER_BASE_URL}/${id}`);
}

export async function createPartner(data: CreatePartnerRequest): Promise<PartnerDto> {
  return apiPost<PartnerDto>(PARTNER_BASE_URL, data);
}

export async function updatePartner(id: string, data: UpdatePartnerRequest): Promise<PartnerDto> {
  return apiPut<PartnerDto>(`${PARTNER_BASE_URL}/${id}`, data);
}

export async function deletePartner(id: string): Promise<void> {
  return apiDelete<void>(`${PARTNER_BASE_URL}/${id}`);
}
