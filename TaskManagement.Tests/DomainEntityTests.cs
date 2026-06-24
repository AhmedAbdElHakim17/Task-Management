using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests;

public class DomainEntityTests
{
    [Fact]
    public void RefreshToken_Create_SetsPropertiesCorrectly()
    {
        var token = "test-token-value";
        var userId = Guid.NewGuid();
        var expiryDays = 7;

        var refreshToken = RefreshToken.Create(token, userId, expiryDays);

        Assert.Equal(0, refreshToken.Id);
        Assert.Equal(token, refreshToken.Token);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.False(refreshToken.IsRevoked);
        Assert.True(refreshToken.ExpiryDate > DateTime.Now.AddDays(6));
        Assert.True(refreshToken.ExpiryDate <= DateTime.Now.AddDays(7));
        Assert.True(refreshToken.CreatedAt > DateTime.Now.AddMinutes(-1));
    }

    [Fact]
    public void TaskItem_Create_SetsPropertiesCorrectly()
    {
        var title = "Test Task";
        var description = "Test Description";
        var userId = Guid.NewGuid();

        var task = TaskItem.Create(title, description, TaskItemPriority.High, userId);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(title, task.Title);
        Assert.Equal(description, task.Description);
        Assert.Equal(TaskItemPriority.High, task.Priority);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(userId, task.UserId);
        Assert.True(task.CreatedAt > DateTime.Now.AddMinutes(-1));
    }

    [Fact]
    public void TaskItem_Create_DefaultStatusIsPending()
    {
        var task = TaskItem.Create("Title", "Desc", TaskItemPriority.Low, Guid.NewGuid());

        Assert.Equal(TaskItemStatus.Pending, task.Status);
    }
}
