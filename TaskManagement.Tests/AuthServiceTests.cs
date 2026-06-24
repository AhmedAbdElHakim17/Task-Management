using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Options;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Results;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Constants;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.Tests;

public class AuthServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repoMock = new();
    private readonly Mock<IJwtService> _jwtMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly RefreshTokenOptions _options = new() { ExpiryDays = 7 };
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var optionsMock = new Mock<IOptions<RefreshTokenOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        _service = new AuthService(
            _userRepoMock.Object, _jwtMock.Object, _repoMock.Object, optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidAccessToken_ReturnsFailure()
    {
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns((System.Security.Claims.ClaimsPrincipal?)null);

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = "bad-token",
            RefreshToken = "some-refresh-token"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.INVALID_ACCESS_TOKEN, result.MsgCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedRefreshToken_ReturnsFailure()
    {
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());

        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        var revokedUserId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(new RefreshToken
        {
            Id = 1,
            Token = "revoked",
            UserId = revokedUserId,
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            IsRevoked = true,
            CreatedAt = DateTime.UtcNow
        });

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = "valid-token",
            RefreshToken = "revoked-refresh-token"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.INVALID_REFRESH_TOKEN, result.MsgCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredRefreshToken_ReturnsFailure()
    {
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());

        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        var expiredUserId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAsync(It.IsAny<string>())).ReturnsAsync(new RefreshToken
        {
            Id = 3,
            Token = "expired",
            UserId = expiredUserId,
            ExpiryDate = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = "valid-token",
            RefreshToken = "expired-refresh-token"
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(CONST_MESSAGE_CODES.INVALID_REFRESH_TOKEN, result.MsgCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidTokens_ReturnsSuccessWithNewTokens()
    {
        var userId = Guid.NewGuid();
        var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        var savedToken = new RefreshToken
        {
            Id = 6,
            Token = "valid-refresh-token",
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        _jwtMock.Setup(j => j.GenerateToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns(new AuthResponseDto { Token = "new-access-token", ExpiresAt = DateTime.Now.AddMinutes(30) });
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
        _repoMock.Setup(r => r.GetAsync("valid-refresh-token")).ReturnsAsync(savedToken);

        var result = await _service.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = "expired-access-token",
            RefreshToken = "valid-refresh-token"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("new-access-token", result.Data.Token);
        Assert.Equal("new-access-token", result.Data.AccessToken);
        Assert.Equal("new-refresh-token", result.Data.RefreshToken);
        Assert.True(result.Data.ExpiresAt > DateTime.Now);
    }
}
