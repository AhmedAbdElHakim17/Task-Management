using TaskManagement.Domain.Entities;

namespace TaskManagement.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetAsync(string token);
    Task SaveAsync(RefreshToken refreshToken);
    Task RevokeAsync(RefreshToken refreshToken);
    Task RevokeAllForUserAsync(Guid userId);
}
