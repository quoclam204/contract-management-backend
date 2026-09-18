using System.Security.Claims;
using ContractManagement.Domain.Identity.Entities;

namespace ContractManagement.Application.Identity.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    string GenerateRefreshToken(User user);
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = false);
}
