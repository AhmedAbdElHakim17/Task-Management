using TaskManagement.Application.DTOs;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Results;

namespace TaskManagement.Application.Services;

public interface IUserService
{
    Task<OperationResult<UserDto>> GetCurrentUserAsync();
    Task<OperationResult<List<UserDto>>> GetAllUsersAsync();
    Task<OperationResult<UserDto>> CreateUserByAdminAsync(CreateUserByAdminRequest request);
    Task<OperationResult> DeleteUserAsync(Guid targetUserId);
}
