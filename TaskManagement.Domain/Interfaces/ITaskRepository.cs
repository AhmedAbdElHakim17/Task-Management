using TaskManagement.Domain.Entities;

namespace TaskManagement.Domain.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<List<TaskItem>> GetByUserIdAsync(Guid userId);
    Task<List<TaskItem>> GetAllAsync();
    Task<bool> ExistsAsync(Guid userId, string title, DateTime date);
    Task AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
}
