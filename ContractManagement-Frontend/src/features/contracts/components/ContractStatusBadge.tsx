import type { FC } from 'react';
import type { ContractStatus } from '../types/contract.types';
import { CONTRACT_STATUS_MAP } from '../types/contract.types';

interface ContractStatusBadgeProps {
  status: ContractStatus;
  className?: string;
}

export const ContractStatusBadge: FC<ContractStatusBadgeProps> = ({ status, className = '' }) => {
  const statusInfo = CONTRACT_STATUS_MAP[status] ?? {
    label: `Trạng thái (${status})`,
    description: 'Không xác định',
    badgeClass: 'bg-gray-100 text-gray-700 border border-gray-300',
    dotColor: 'bg-gray-400',
  };

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold tracking-wide ${statusInfo.badgeClass} ${className}`}
      title={statusInfo.description}
    >
      <span className={`w-1.5 h-1.5 rounded-full ${statusInfo.dotColor} animate-pulse`} />
      {statusInfo.label}
    </span>
  );
};
