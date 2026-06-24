using System.Security.Claims;
using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IJwtService
{
    AuthResponseDto GenerateToken(User user);
    AuthResponseDto GenerateToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
