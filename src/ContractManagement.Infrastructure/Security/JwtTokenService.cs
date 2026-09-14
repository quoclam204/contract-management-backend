using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ContractManagement.Application.Identity.Interfaces;
using ContractManagement.Domain.Identity.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ContractManagement.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "ContractManagementSuperSecretKey2026!@#$%^&*()_+";
        var issuer = _configuration["Jwt:Issuer"] ?? "ContractManagement";
        var audience = _configuration["Jwt:Audience"] ?? "ContractManagementApp";
        var expirationMinutes = int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var exp) ? exp : 480;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role_id", ((byte)user.Role).ToString()),
        };

        if (user.DepartmentId.HasValue)
        {
            claims.Add(new Claim("department_id", user.DepartmentId.Value.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
