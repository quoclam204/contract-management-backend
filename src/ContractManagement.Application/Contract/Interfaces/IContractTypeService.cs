using ContractManagement.Application.Contract.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContractManagement.Application.Contract.Interfaces;

/// <summary>
/// Interface dịch vụ quản lý Contract Type
/// </summary>
public interface IContractTypeService
{
    /// <summary>
    /// Lấy danh sách tất cả Contract Types
    /// </summary>
    Task<List<ContractTypeDto>> GetAllAsync();

    /// <summary>
    /// Lấy Contract Type theo Id
    /// </summary>
    Task<ContractTypeDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Tạo mới Contract Type
    /// </summary>
    Task<ContractTypeDto> CreateAsync(ContractTypeDto dto);

    /// <summary>
    /// Cập nhật Contract Type
    /// </>
    Task<ContractTypeDto?> UpdateAsync(Guid id, ContractTypeDto dto);

    /// <summary>
    /// Xóa Contract Type
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
