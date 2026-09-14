export interface PartnerDto {
  id: string;
  name: string;
  taxCode?: string | null;
  representative?: string | null;
  contactEmail?: string | null;
  address?: string | null;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreatePartnerRequest {
  name: string;
  taxCode: string;
  representative: string;
  contactEmail: string;
  address: string;
}

export interface UpdatePartnerRequest {
  id: string;
  name: string;
  taxCode: string;
  representative: string;
  contactEmail: string;
  address: string;
}
