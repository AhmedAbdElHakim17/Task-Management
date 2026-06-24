using TaskManagement.Application.DTOs;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Results;

namespace TaskManagement.Application.Services;

public interface IAuthService
{
    Task<OperationResult<UserDto>> RegisterAsync(RegisterRequest request);
    Task<OperationResult<AuthResultDto>> LoginAsync(LoginRequest request);
    Task<OperationResult<AuthResultDto>> RefreshTokenAsync(RefreshTokenRequest request);
}
