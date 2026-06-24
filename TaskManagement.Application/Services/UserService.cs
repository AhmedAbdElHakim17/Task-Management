using Microsoft.Extensions.Logging;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Mappings;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Results;
using TaskManagement.Domain.Constants;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Application.Services;

public class UserService(IUserRepository userRepository,
                         ICurrentUserService currentUserService,
                         ILogger<UserService> logger) : IUserService
{
    public async Task<OperationResult<UserDto>> GetCurrentUserAsync()
    {
        try
        {
            var userId = currentUserService.UserId;
            var user = await userRepository.GetByIdAsync(userId);

            if (user is null || user.IsDeleted)
            {
                logger.LogWarning("Current user query failed: User {UserId} not found", userId);
                return OperationResult<UserDto>.Failure(CONST_MESSAGE_CODES.USER_NOT_FOUND, API_STATUS_CODES.NOT_FOUND);
            }

            logger.LogInformation("Retrieved current user profile for {UserId}", userId);
            return OperationResult<UserDto>.Success(EntityMapper.ToDto(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving current user");
            return OperationResult<UserDto>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult<List<UserDto>>> GetAllUsersAsync()
    {
        try
        {
            if (currentUserService.Role != Role.Admin.ToString())
            {
                logger.LogWarning("Get all users failed: Non-admin user {UserId} attempted to access all users", currentUserService.UserId);
                return OperationResult<List<UserDto>>.Failure("Only administrators can view all users.", 403);
            }

            var users = await userRepository.GetAllAsync();

            var result = users.Where(u => !u.IsDeleted).Select(EntityMapper.ToDto).ToList();

            logger.LogInformation("Retrieved {UserCount} users by admin {AdminId}", result.Count, currentUserService.UserId);
            return OperationResult<List<UserDto>>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving all users");
            return OperationResult<List<UserDto>>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult<UserDto>> CreateUserByAdminAsync(CreateUserByAdminRequest request)
    {
        try
        {
            if (currentUserService.Role != Role.Admin.ToString())
            {
                logger.LogWarning("User creation failed: Non-admin user {UserId} attempted to create user", currentUserService.UserId);
                return OperationResult<UserDto>.Failure(CONST_MESSAGE_CODES.ONLY_ADMINISTRATORS, API_STATUS_CODES.FORBIDDEN);
            }

            var existingUser = await userRepository.GetByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                logger.LogWarning("User creation failed: Email {Email} is already registered", request.Email);
                return OperationResult<UserDto>.Failure(CONST_MESSAGE_CODES.EMAIL_ALREADY_REGISTERED, API_STATUS_CODES.BAD_REQUEST);
            }

            var user = User.Create(request.Name, request.Email, BCrypt.Net.BCrypt.HashPassword(request.Password), request.Role);
            await userRepository.AddAsync(user);

            logger.LogInformation("User created successfully: {UserId} with email {Email} by admin {AdminId}", user.Id, user.Email, currentUserService.UserId);
            return OperationResult<UserDto>.Success(EntityMapper.ToDto(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating user with email {Email}", request.Email);
            return OperationResult<UserDto>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult> DeleteUserAsync(Guid targetUserId)
    {
        try
        {
            if (currentUserService.Role != Role.Admin.ToString())
            {
                logger.LogWarning("User deletion failed: Non-admin user {UserId} attempted to delete user", currentUserService.UserId);
                return OperationResult.Failure(CONST_MESSAGE_CODES.ONLY_ADMINISTRATORS, API_STATUS_CODES.FORBIDDEN);
            }

            var user = await userRepository.GetByIdAsync(targetUserId);
            if (user is null || user.IsDeleted)
            {
                logger.LogWarning("User deletion failed: User {UserId} not found", targetUserId);
                return OperationResult.Failure(CONST_MESSAGE_CODES.USER_NOT_FOUND, API_STATUS_CODES.NOT_FOUND);
            }

            user.IsDeleted = true;
            await userRepository.UpdateAsync(user);

            logger.LogInformation("User {UserId} deleted by admin {AdminId}", targetUserId, currentUserService.UserId);
            return OperationResult.Success(API_STATUS_CODES.NO_CONTENT);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting user {UserId}", targetUserId);
            return OperationResult.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }
}
