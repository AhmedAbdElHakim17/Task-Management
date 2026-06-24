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

public class AuthService(IUserRepository userRepository,
                         IJwtService jwtService,
                         IRefreshTokenRepository refreshTokenRepository,
                         IOptions<RefreshTokenOptions> refreshTokenOptions,
                         ILogger<AuthService> logger) : IAuthService
{
    public async Task<OperationResult<UserDto>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var existingUser = await userRepository.GetByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                logger.LogWarning("Registration failed: Email {Email} is already registered", request.Email);
                return OperationResult<UserDto>.Failure(CONST_MESSAGE_CODES.EMAIL_ALREADY_REGISTERED, API_STATUS_CODES.BAD_REQUEST);
            }

            var user = User.Create(request.Name, request.Email, BCrypt.Net.BCrypt.HashPassword(request.Password), Role.User);
            await userRepository.AddAsync(user);

            logger.LogInformation("User registered successfully: {UserId} with email {Email}", user.Id, user.Email);
            return OperationResult<UserDto>.Success(EntityMapper.ToDto(user), API_STATUS_CODES.CREATED);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during registration for email {Email}", request.Email);
            return OperationResult<UserDto>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult<AuthResultDto>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await userRepository.GetByEmailAsync(request.Email);
            if (user is null || user.IsDeleted)
            {
                logger.LogWarning("Login failed: User with email {Email} not found", request.Email);
                return OperationResult<AuthResultDto>.Failure(CONST_MESSAGE_CODES.USER_NOT_FOUND, API_STATUS_CODES.NOT_FOUND);
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                logger.LogWarning("Login failed: Invalid password for email {Email}", request.Email);
                return OperationResult<AuthResultDto>.Failure(CONST_MESSAGE_CODES.INVALID_CREDENTIALS, API_STATUS_CODES.BAD_REQUEST);
            }

            var tokenResponse = jwtService.GenerateToken(user);

            await refreshTokenRepository.RevokeAllForUserAsync(user.Id);

            var refreshToken = RefreshToken.Create(jwtService.GenerateRefreshToken(), user.Id, refreshTokenOptions.Value.ExpiryDays);

            await refreshTokenRepository.SaveAsync(refreshToken);

            var authResult = AuthResultDto.Create(tokenResponse, EntityMapper.ToDto(user), refreshToken.Token);

            logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return OperationResult<AuthResultDto>.Success(authResult);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during login for email {Email}", request.Email);
            return OperationResult<AuthResultDto>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }

    public async Task<OperationResult<AuthResultDto>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var principal = jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal is null)
            {
                logger.LogWarning("Refresh token failed: Invalid expired access token");
                return OperationResult<AuthResultDto>.Failure(CONST_MESSAGE_CODES.INVALID_ACCESS_TOKEN, API_STATUS_CODES.BAD_REQUEST);
            }

            var savedToken = await refreshTokenRepository.GetAsync(request.RefreshToken);
            if (savedToken is null || savedToken.IsRevoked || savedToken.ExpiryDate <= DateTime.Now)
            {
                logger.LogWarning("Refresh token failed: Invalid or expired refresh token");
                return OperationResult<AuthResultDto>.Failure(CONST_MESSAGE_CODES.INVALID_REFRESH_TOKEN, API_STATUS_CODES.BAD_REQUEST);
            }

            var tokenResponse = jwtService.GenerateToken(principal.Claims);
            var newRefreshToken = RefreshToken.Create(jwtService.GenerateRefreshToken(), savedToken.UserId, refreshTokenOptions.Value.ExpiryDays);

            await refreshTokenRepository.RevokeAllForUserAsync(savedToken.UserId);
            await refreshTokenRepository.SaveAsync(newRefreshToken);

            logger.LogInformation("Tokens refreshed for user {UserId}", savedToken.UserId);

            return OperationResult<AuthResultDto>.Success(AuthResultDto.Create(tokenResponse, new UserDto { Id = savedToken.UserId }, newRefreshToken.Token));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during token refresh");
            return OperationResult<AuthResultDto>.Failure(CONST_MESSAGE_CODES.OPERATION_FAILED, API_STATUS_CODES.INTERNAL_SERVER_ERROR);
        }
    }
}
