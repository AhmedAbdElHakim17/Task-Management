using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Options;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Identity;

public class JwtService(IOptions<JwtOptions> options) : IJwtService
{
    private JwtOptions Jwt => options.Value;
    private SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(Jwt.Key));
    private SigningCredentials Credentials => new(Key, SecurityAlgorithms.HmacSha256);

    public AuthResponseDto GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = CreateToken(claims);
        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = token.ValidTo.ToLocalTime()
        };
    }

    public AuthResponseDto GenerateToken(IEnumerable<Claim> claims)
    {
        var token = CreateToken(claims);
        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = token.ValidTo.ToLocalTime()
        };
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = Jwt.Audience,
            ValidIssuer = Jwt.Issuer,
            IssuerSigningKey = Key,
            ValidateLifetime = false
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private JwtSecurityToken CreateToken(IEnumerable<Claim> claims)
        => new(Jwt.Issuer, Jwt.Audience, claims, expires: DateTime.Now.AddMinutes(Jwt.ExpiryMinutes), signingCredentials: Credentials);
}
