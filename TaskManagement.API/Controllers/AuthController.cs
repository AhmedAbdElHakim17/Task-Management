using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Services;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Handles user authentication: registration, login, and profile retrieval.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Auth")]
public class AuthController(IAuthService authService, IUserService userService) : ControllerBase
{

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <remarks>
    /// Creates a new user with the <c>User</c> role. The email must be unique across the system.
    ///
    /// **Sample request:**
    /// ```json
    /// {
    ///   "name": "Jane Doe",
    ///   "email": "jane@example.com",
    ///   "password": "P@ssw0rd!"
    /// }
    /// ```
    /// </remarks>
    /// <response code="201">User created successfully. Returns the new user profile.</response>
    /// <response code="400">Validation failed or email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Authenticate a user and obtain a JWT bearer token.
    /// </summary>
    /// <remarks>
    /// Validates credentials and returns a signed JWT token valid for the configured duration.
    /// Use the returned token in the <c>Authorization: Bearer &lt;token&gt;</c> header for all
    /// protected endpoints.
    ///
    /// **Sample request:**
    /// ```json
    /// {
    ///   "email": "jane@example.com",
    ///   "password": "P@ssw0rd!"
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">Login successful. Returns the JWT token and user profile.</response>
    /// <response code="400">Invalid credentials (wrong password).</response>
    /// <response code="404">No account found for the supplied email address.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Refresh an expired JWT access token using a valid refresh token.
    /// </summary>
    /// <remarks>
    /// Accepts the expired access token and the refresh token. Returns a new access token
    /// and a new refresh token (token rotation). The old refresh token is revoked.
    ///
    /// **Sample request:**
    /// ```json
    /// {
    ///   "accessToken": "eyJhbGciOi...",
    ///   "refreshToken": "aB3dE5..."
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">Tokens refreshed successfully. Returns new access and refresh tokens.</response>
    /// <response code="400">Invalid or expired tokens provided.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshTokenAsync(request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Get the currently authenticated user's profile.
    /// </summary>
    /// <remarks>
    /// Reads the user identity from the JWT bearer token supplied in the request header
    /// and returns the corresponding user profile.
    ///
    /// **Requires:** `Authorization: Bearer &lt;token&gt;`
    /// </remarks>
    /// <response code="200">Returns the authenticated user's profile.</response>
    /// <response code="401">No valid JWT token was provided.</response>
    /// <response code="404">The user referenced by the token no longer exists.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me()
    {
        var result = await userService.GetCurrentUserAsync();
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }
}
