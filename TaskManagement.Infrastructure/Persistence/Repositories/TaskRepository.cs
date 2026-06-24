using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TaskRepository(AppDbContext context) : ITaskRepository
{
    public async Task<TaskItem?> GetByIdAsync(Guid id)
        => await context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<TaskItem>> GetByUserIdAsync(Guid userId)
        => await context.Tasks.Where(t => t.UserId == userId).ToListAsync();

    public async Task<List<TaskItem>> GetAllAsync()
        => await context.Tasks.ToListAsync();

    public async Task<bool> ExistsAsync(Guid userId, string title, DateTime date)
        => await context.Tasks.AnyAsync(t => t.UserId == userId && t.Title == title && t.CreatedAt.Date == date.Date);

    public async Task AddAsync(TaskItem task)
    {
        await context.Tasks.AddAsync(task);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TaskItem task)
    {
        context.Tasks.Update(task);
        await context.SaveChangesAsync();
    }
}
