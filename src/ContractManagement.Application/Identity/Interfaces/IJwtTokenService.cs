using ContractManagement.Domain.Identity.Entities;

namespace ContractManagement.Application.Identity.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
