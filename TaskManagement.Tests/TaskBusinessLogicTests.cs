using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests;

public class TaskBusinessLogicTests
{
    [Fact]
    public void SortTasks_PriorityDescendingThenCreatedAtAscending()
    {
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        var tasks = new List<TaskItem>
        {
            CreateTask("Medium old",    TaskItemPriority.Medium, now.AddHours(-2), userId),
            CreateTask("High new",      TaskItemPriority.High,   now.AddHours(-1), userId),
            CreateTask("Low newest",    TaskItemPriority.Low,    now,              userId),
            CreateTask("High old",      TaskItemPriority.High,   now.AddHours(-3), userId),
            CreateTask("Medium newer",  TaskItemPriority.Medium, now.AddHours(-1), userId),
        };

        var sorted = tasks
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToList();

        Assert.Equal("High old", sorted[0].Title);
        Assert.Equal("High new", sorted[1].Title);
        Assert.Equal("Medium old", sorted[2].Title);
        Assert.Equal("Medium newer", sorted[3].Title);
        Assert.Equal("Low newest", sorted[4].Title);
    }

    [Fact]
    public void TaskPriority_EnumOrder_LowThenMediumThenHigh()
    {
        Assert.True((int)TaskItemPriority.Low < (int)TaskItemPriority.Medium);
        Assert.True((int)TaskItemPriority.Medium < (int)TaskItemPriority.High);
    }

    private static TaskItem CreateTask(string title, TaskItemPriority priority, DateTime createdAt, Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "",
            Status = TaskItemStatus.Pending,
            Priority = priority,
            UserId = userId,
            CreatedAt = createdAt
        };
}
