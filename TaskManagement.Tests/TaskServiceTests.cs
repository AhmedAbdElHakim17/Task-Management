using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Options;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Constants;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Tests;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ITaskQueue> _taskQueueMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<TaskService>> _loggerMock = new();
    private readonly RedisOptions _options = new() { ExpiryMinutes = 10 };
    private readonly TaskService _service;
    private readonly Guid _currentUserId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();

    public TaskServiceTests()
    {
        var optionsMock = new Mock<IOptions<RedisOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        _currentUserMock.Setup(c => c.UserId).Returns(_currentUserId);
        _service = new TaskService(
            _taskRepoMock.Object,
            _userRepoMock.Object,
            _currentUserMock.Object,
            _taskQueueMock.Object,
            _cacheMock.Object,
            _loggerMock.Object,
            optionsMock.Object);
    }

    // ─── GetAllTasksAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllTasksAsync_RegularUser_ReturnsOwnTasksOnly()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var ownTask = CreateTask("Own", _currentUserId);
        _taskRepoMock.Setup(r => r.GetByUserIdAsync(_currentUserId))
            .ReturnsAsync(new List<TaskItem> { ownTask });

        var result = await _service.GetAllTasksAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("Own", result.Data[0].Title);
    }

    [Fact]
    public async Task GetAllTasksAsync_Admin_ReturnsAllNonAdminTasks()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var regularUserTask = CreateTask("Regular", _otherUserId);
        var adminTask = CreateTask("AdminTask", _adminUserId);
        _taskRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TaskItem> { regularUserTask, adminTask });
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>
            {
                new() { Id = _currentUserId, Role = Role.Admin },
                new() { Id = _adminUserId, Role = Role.Admin }
            });

        var result = await _service.GetAllTasksAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("Regular", result.Data[0].Title);
    }

    [Fact]
    public async Task GetAllTasksAsync_Admin_WhenAllTasksOwnedByAdmins_ReturnsEmpty()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var adminTask = CreateTask("AdminTask", _adminUserId);
        _taskRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<TaskItem> { adminTask });
        _userRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User>
            {
                new() { Id = _currentUserId, Role = Role.Admin },
                new() { Id = _adminUserId, Role = Role.Admin }
            });

        var result = await _service.GetAllTasksAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    // ─── GetTaskByIdAsync (Admin) ───────────────────────────────────────

    [Fact]
    public async Task GetTaskByIdAsync_AdminGetsRegularUserTask_ReturnsTask()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("RegularTask", _otherUserId, taskId);
        SetupGetTaskNoCache(taskId, task);
        _userRepoMock.Setup(r => r.GetByIdAsync(_otherUserId))
            .ReturnsAsync(new User { Id = _otherUserId, Role = Role.User });

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.True(result.IsSuccess);
        Assert.Equal("RegularTask", result.Data.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_AdminGetsAnotherAdminTask_ReturnsForbidden()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("AdminTask", _adminUserId, taskId);
        SetupGetTaskNoCache(taskId, task);
        _userRepoMock.Setup(r => r.GetByIdAsync(_adminUserId))
            .ReturnsAsync(new User { Id = _adminUserId, Role = Role.Admin });

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.ACCESS_DENIED, result.MsgCode);
    }

    [Fact]
    public async Task GetTaskByIdAsync_AdminGetsOwnTask_ReturnsTask()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("OwnTask", _currentUserId, taskId);
        SetupGetTaskNoCache(taskId, task);

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.True(result.IsSuccess);
        Assert.Equal("OwnTask", result.Data.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_AdminCacheHitOwnTask_ReturnsCached()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var cached = new TaskDto
        {
            Id = taskId,
            Title = "Cached",
            UserId = _currentUserId
        };
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}"))
            .ReturnsAsync(cached);

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cached", result.Data.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_AdminCacheHitOtherAdmin_GoesToDbAndReturnsForbidden()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var cached = new TaskDto
        {
            Id = taskId,
            Title = "CachedAdminTask",
            UserId = _adminUserId
        };
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}"))
            .ReturnsAsync(cached);

        var task = CreateTask("AdminTask", _adminUserId, taskId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _userRepoMock.Setup(r => r.GetByIdAsync(_adminUserId))
            .ReturnsAsync(new User { Id = _adminUserId, Role = Role.Admin });

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.ACCESS_DENIED, result.MsgCode);
    }

    [Fact]
    public async Task GetTaskByIdAsync_AdminCacheHitRegularUser_GoesToDbAndReturnsTask()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var cached = new TaskDto
        {
            Id = taskId,
            Title = "CachedRegularTask",
            UserId = _otherUserId
        };
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}"))
            .ReturnsAsync(cached);

        var task = CreateTask("RegularTask", _otherUserId, taskId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _userRepoMock.Setup(r => r.GetByIdAsync(_otherUserId))
            .ReturnsAsync(new User { Id = _otherUserId, Role = Role.User });

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.True(result.IsSuccess);
        Assert.Equal("RegularTask", result.Data.Title);
    }

    // ─── GetTaskByIdAsync (Regular User) ────────────────────────────────

    [Fact]
    public async Task GetTaskByIdAsync_RegularUserGetsOwnTask_ReturnsTask()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("OwnTask", _currentUserId, taskId);
        SetupGetTaskNoCache(taskId, task);

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.True(result.IsSuccess);
        Assert.Equal("OwnTask", result.Data.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_RegularUserGetsOthersTask_ReturnsForbidden()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("OthersTask", _otherUserId, taskId);
        SetupGetTaskNoCache(taskId, task);

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.ACCESS_DENIED, result.MsgCode);
    }

    [Fact]
    public async Task GetTaskByIdAsync_RegularUserCacheHitOwnTask_ReturnsCached()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        var cached = new TaskDto
        {
            Id = taskId,
            Title = "CachedOwn",
            UserId = _currentUserId
        };
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}"))
            .ReturnsAsync(cached);

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.True(result.IsSuccess);
        Assert.Equal("CachedOwn", result.Data.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_RegularUserCacheHitOthersTask_ReturnsForbidden()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        var cached = new TaskDto
        {
            Id = taskId,
            Title = "CachedOthers",
            UserId = _otherUserId
        };
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}"))
            .ReturnsAsync(cached);

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.ACCESS_DENIED, result.MsgCode);
    }

    [Fact]
    public async Task GetTaskByIdAsync_TaskNotFound_ReturnsNotFound()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}"))
            .ReturnsAsync((TaskDto?)null);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId))
            .ReturnsAsync((TaskItem?)null);

        var result = await _service.GetTaskByIdAsync(taskId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.TASK_NOT_FOUND, result.MsgCode);
    }

    // ─── UpdateTaskStatusAsync (Admin) ──────────────────────────────────

    [Fact]
    public async Task UpdateTaskStatusAsync_AdminUpdatesRegularUserTask_ReturnsNoContent()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("Task", _otherUserId, taskId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _userRepoMock.Setup(r => r.GetByIdAsync(_otherUserId))
            .ReturnsAsync(new User { Id = _otherUserId, Role = Role.User });

        var result = await _service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequest { NewStatus = TaskItemStatus.Done });

        Assert.True(result.IsSuccess);
        Assert.Equal(API_STATUS_CODES.NO_CONTENT, result.StatusCode);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_AdminUpdatesOtherAdminTask_ReturnsForbidden()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("AdminTask", _adminUserId, taskId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
        _userRepoMock.Setup(r => r.GetByIdAsync(_adminUserId))
            .ReturnsAsync(new User { Id = _adminUserId, Role = Role.Admin });

        var result = await _service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequest { NewStatus = TaskItemStatus.Done });

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.ACCESS_DENIED, result.MsgCode);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_AdminUpdatesOwnTask_ReturnsNoContent()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.Admin.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("OwnTask", _currentUserId, taskId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        var result = await _service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequest { NewStatus = TaskItemStatus.Done });

        Assert.True(result.IsSuccess);
        Assert.Equal(API_STATUS_CODES.NO_CONTENT, result.StatusCode);
    }

    // ─── UpdateTaskStatusAsync (Regular User) ───────────────────────────

    [Fact]
    public async Task UpdateTaskStatusAsync_RegularUserUpdatesOwnTask_ReturnsNoContent()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("OwnTask", _currentUserId, taskId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        var result = await _service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequest { NewStatus = TaskItemStatus.Done });

        Assert.True(result.IsSuccess);
        Assert.Equal(API_STATUS_CODES.NO_CONTENT, result.StatusCode);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_RegularUserUpdatesOthersTask_ReturnsForbidden()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        var task = CreateTask("OthersTask", _otherUserId, taskId);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);

        var result = await _service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequest { NewStatus = TaskItemStatus.Done });

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.ACCESS_DENIED, result.MsgCode);
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_TaskNotFound_ReturnsNotFound()
    {
        _currentUserMock.Setup(c => c.Role).Returns(Role.User.ToString());
        var taskId = Guid.NewGuid();
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId))
            .ReturnsAsync((TaskItem?)null);

        var result = await _service.UpdateTaskStatusAsync(taskId,
            new UpdateTaskStatusRequest { NewStatus = TaskItemStatus.Done });

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.TASK_NOT_FOUND, result.MsgCode);
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private void SetupGetTaskNoCache(Guid taskId, TaskItem task)
    {
        _cacheMock.Setup(c => c.GetAsync<TaskDto>($"task:{taskId}"))
            .ReturnsAsync((TaskDto?)null);
        _taskRepoMock.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(task);
    }

    private static TaskItem CreateTask(string title, Guid userId)
        => CreateTask(title, userId, Guid.NewGuid());

    private static TaskItem CreateTask(string title, Guid userId, Guid taskId)
        => new()
        {
            Id = taskId,
            Title = title,
            Description = "",
            Status = TaskItemStatus.Pending,
            Priority = TaskItemPriority.Medium,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
}
