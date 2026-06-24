using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email)
        => await context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<List<User>> GetAllAsync()
        => await context.Users.ToListAsync();

    public async Task AddAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return;

        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return;

        user.IsDeleted = true;
        await context.SaveChangesAsync();
    }
}
