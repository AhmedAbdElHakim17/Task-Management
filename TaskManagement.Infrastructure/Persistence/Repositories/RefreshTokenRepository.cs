using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetAsync(string token)
        => await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);

    public async Task SaveAsync(RefreshToken refreshToken)
    {
        await context.RefreshTokens.AddAsync(refreshToken);
        await context.SaveChangesAsync();
    }

    public async Task RevokeAsync(RefreshToken refreshToken)
    {
        refreshToken.IsRevoked = true;
        await context.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var activeTokens = await context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();

        foreach (var token in activeTokens)
            token.IsRevoked = true;

        await context.SaveChangesAsync();
    }
}
