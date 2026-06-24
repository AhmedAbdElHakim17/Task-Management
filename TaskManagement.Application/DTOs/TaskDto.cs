using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs;

public record TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TaskItemStatus Status { get; set; }
    public TaskItemPriority Priority { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static TaskDto Create(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            UserId = task.UserId,
            CreatedAt = task.CreatedAt
        };
    }
}
