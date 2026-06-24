using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskItemStatus Status { get; set; }
    public TaskItemPriority Priority { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static TaskItem Create(string title, string description, TaskItemPriority priority, Guid userId)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Status = TaskItemStatus.Pending,
            Priority = priority,
            UserId = userId,
            CreatedAt = DateTime.Now
        };
    }
}
