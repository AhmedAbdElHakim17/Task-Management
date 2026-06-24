using TaskManagement.Application.DTOs;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Results;

namespace TaskManagement.Application.Services;

public interface ITaskService
{
    Task<OperationResult<TaskDto>> CreateTaskAsync(CreateTaskRequest request);
    Task<OperationResult<List<TaskDto>>> GetAllTasksAsync();
    Task<OperationResult<TaskDto>> GetTaskByIdAsync(Guid taskId);
    Task<OperationResult> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusRequest request);
}
