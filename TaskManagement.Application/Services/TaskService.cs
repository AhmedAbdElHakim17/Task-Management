using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Mappings;
using TaskManagement.Application.Options;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Results;
using TaskManagement.Domain.Constants;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Services;

public class TaskService(
    ITaskRepository taskRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    ITaskQueue taskQueue,
    ICacheService cacheService,
    ILogger<TaskService> logger,
    IOptions<RedisOptions> options) : ITaskService
{
    private bool IsAdmin => currentUserService.Role == Role.Admin.ToString();

    public async Task<OperationResult<TaskDto>> CreateTaskAsync(CreateTaskRequest request)
    {
        try
        {
            var userId = currentUserService.UserId;
            var today = DateTime.Now.Date;
            var taskExists = await taskRepository.ExistsAsync(userId, request.Title, today);
            if (taskExists)
            {
                logger.LogWarning("Task creation failed: Duplicate title for user {UserId} on {Date}", userId, today);
                return OperationResult<TaskDto>.Failure(CONST_MESSAGE_CODES.DUPLICATE_TASK_TITLE, API_STATUS_CODES.BAD_REQUEST);
            }

            var task = TaskItem.Create(request.Title, request.Description, request.Priority, userId);
            await taskRepository.AddAsync(task);
            taskQueue.Enqueue(task.Id);

            logger.LogInformation("Task created successfully: {TaskId} for user {UserId}", task.Id, userId);
            return OperationResult<TaskDto>.Success(EntityMapper.ToDto(task), API_STATUS_CODES.OK);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating task");
            return OperationResult<TaskDto>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult<List<TaskDto>>> GetAllTasksAsync()
    {
        try
        {
            var userId = currentUserService.UserId;

            List<TaskItem> tasks;
            if (IsAdmin)
            {
                var adminUserIds = (await userRepository.GetAllAsync())
                    .Where(u => u.Role == Role.Admin)
                    .Select(u => u.Id)
                    .ToHashSet();

                tasks = await taskRepository.GetAllAsync();
                tasks = tasks.Where(t => !adminUserIds.Contains(t.UserId)).ToList();
            }
            else
            {
                tasks = await taskRepository.GetByUserIdAsync(userId);
            }

            var result = tasks.OrderByDescending(t => t.Priority)
                              .ThenBy(t => t.CreatedAt)
                              .Select(EntityMapper.ToDto)
                              .ToList();

            logger.LogInformation("Retrieved {TaskCount} tasks for user {UserId}", result.Count, userId);
            return OperationResult<List<TaskDto>>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving all tasks");
            return OperationResult<List<TaskDto>>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult<TaskDto>> GetTaskByIdAsync(Guid taskId)
    {
        try
        {
            var userId = currentUserService.UserId;
            var cacheKey = $"task:{taskId}";
            var cached = await cacheService.GetAsync<TaskDto>(cacheKey);

            if (cached is not null)
            {
                if (cached.UserId == userId)
                {
                    logger.LogInformation("[CACHE HIT] Task {TaskId} served from Redis", taskId);
                    return OperationResult<TaskDto>.Success(cached);
                }

                if (!IsAdmin)
                {
                    logger.LogWarning("User {UserId} does not have access to cached task {TaskId}", userId, taskId);
                    return OperationResult<TaskDto>.Failure(CONST_MESSAGE_CODES.ACCESS_DENIED, API_STATUS_CODES.FORBIDDEN);
                }
            }

            logger.LogInformation("[CACHE MISS] Task {TaskId} not in Redis — fetching from database", taskId);

            var task = await taskRepository.GetByIdAsync(taskId);
            if (task is null)
            {
                logger.LogWarning("Task {TaskId} not found", taskId);
                return OperationResult<TaskDto>.Failure(CONST_MESSAGE_CODES.TASK_NOT_FOUND, API_STATUS_CODES.NOT_FOUND);
            }

            if (task.UserId != userId)
            {
                if (!IsAdmin)
                {
                    logger.LogWarning("User {UserId} does not have access to task {TaskId}", userId, taskId);
                    return OperationResult<TaskDto>.Failure(CONST_MESSAGE_CODES.ACCESS_DENIED, API_STATUS_CODES.FORBIDDEN);
                }

                var owner = await userRepository.GetByIdAsync(task.UserId);
                if (owner?.Role == Role.Admin)
                {
                    logger.LogWarning("Admin {UserId} does not have access to another admin's task {TaskId}", userId, taskId);
                    return OperationResult<TaskDto>.Failure(CONST_MESSAGE_CODES.ACCESS_DENIED, API_STATUS_CODES.FORBIDDEN);
                }
            }

            var taskDto = EntityMapper.ToDto(task);
            await cacheService.SetAsync(cacheKey, taskDto, TimeSpan.FromMinutes(options.Value.ExpiryMinutes));

            logger.LogInformation("[CACHE SET] Task {TaskId} stored in Redis (TTL {Minutes}m)", taskId, options.Value.ExpiryMinutes);
            return OperationResult<TaskDto>.Success(taskDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving task {TaskId}", taskId);
            return OperationResult<TaskDto>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusRequest request)
    {
        try
        {
            var task = await taskRepository.GetByIdAsync(taskId);
            if (task is null)
            {
                logger.LogWarning("Task update failed: Task {TaskId} not found", taskId);
                return OperationResult.Failure(CONST_MESSAGE_CODES.TASK_NOT_FOUND, API_STATUS_CODES.NOT_FOUND);
            }

            if (task.UserId != currentUserService.UserId)
            {
                if (!IsAdmin)
                {
                    logger.LogWarning("Task update failed: User {UserId} does not have access to task {TaskId}",
                        currentUserService.UserId, taskId);
                    return OperationResult.Failure(CONST_MESSAGE_CODES.ACCESS_DENIED, API_STATUS_CODES.FORBIDDEN);
                }

                var owner = await userRepository.GetByIdAsync(task.UserId);
                if (owner?.Role == Role.Admin)
                {
                    logger.LogWarning("Admin {UserId} cannot update another admin's task {TaskId}",
                        currentUserService.UserId, taskId);
                    return OperationResult.Failure(CONST_MESSAGE_CODES.ACCESS_DENIED, API_STATUS_CODES.FORBIDDEN);
                }
            }

            task.Status = request.NewStatus;
            await taskRepository.UpdateAsync(task);
            await cacheService.RemoveAsync($"task:{taskId}");

            logger.LogInformation("Task {TaskId} status updated to {Status}", taskId, request.NewStatus);
            return OperationResult.Success(API_STATUS_CODES.NO_CONTENT);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating task {TaskId} status", taskId);
            return OperationResult.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }
}
