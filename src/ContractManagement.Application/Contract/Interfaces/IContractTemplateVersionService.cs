using ContractManagement.Application.Contract.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContractManagement.Application.Contract.Interfaces;

/// <summary>
/// Interface dịch vụ quản lý Contract Template Version
/// </summary>
public interface IContractTemplateVersionService
{
    /// <summary>
    /// Lấy danh sách tất cả Contract Template Versions
    /// </summary>
    Task<List<ContractTemplateVersionDto>> GetAllAsync();

    /// <summary>
    /// Lấy Contract Template Version theo Id
    /// </summary>
    Task<ContractTemplateVersionDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Tạo mới Contract Template Version
    /// </summary>
    Task<ContractTemplateVersionDto> CreateAsync(ContractTemplateVersionDto dto);

    /// <summary>
    /// Cập nhật Contract Template Version
    /// </summary>
    Task<ContractTemplateVersionDto?> UpdateAsync(Guid id, ContractTemplateVersionDto dto);

    /// <summary>
    /// Xóa Contract Template Version
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
