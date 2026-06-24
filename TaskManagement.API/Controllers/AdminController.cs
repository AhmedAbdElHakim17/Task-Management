using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Services;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Admin-only user management operations. All endpoints require an <c>Admin</c> role JWT token.
/// </summary>
[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Tags("Admin")]
public class AdminController(IUserService userService) : ControllerBase
{

    /// <summary>
    /// Create a new user (admin operation).
    /// </summary>
    /// <remarks>
    /// Allows an administrator to create a new user with any role, including <c>Admin</c>.
    /// The email must be unique. Unlike self-registration, the role can be explicitly set.
    ///
    /// **Sample request:**
    /// ```json
    /// {
    ///   "name": "John Smith",
    ///   "email": "john@example.com",
    ///   "password": "P@ssw0rd!",
    ///   "role": "User"
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">User created successfully. Returns the newly created user profile.</response>
    /// <response code="400">Validation failed or email already registered.</response>
    /// <response code="401">No valid JWT token was provided.</response>
    /// <response code="403">The authenticated user does not have the Admin role.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserByAdminRequest request)
    {
        var result = await userService.CreateUserByAdminAsync(request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Soft-delete a user by ID (admin operation).
    /// </summary>
    /// <remarks>
    /// Performs a **soft delete** — the user record is marked as deleted and excluded from
    /// future queries, but is not physically removed from the database.
    /// A soft-deleted user cannot log in.
    /// </remarks>
    /// <param name="id">The ID of the user to delete.</param>
    /// <response code="204">User successfully soft-deleted.</response>
    /// <response code="401">No valid JWT token was provided.</response>
    /// <response code="403">The authenticated user does not have the Admin role.</response>
    /// <response code="404">No user found with the given ID.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await userService.DeleteUserAsync(id);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode);
    }

    /// <summary>
    /// Retrieve all registered users (admin operation).
    /// </summary>
    /// <remarks>
    /// Returns a flat list of all active (non-deleted) users in the system.
    /// Soft-deleted users are automatically excluded by the global query filter.
    /// </remarks>
    /// <response code="200">Returns the list of all active users.</response>
    /// <response code="401">No valid JWT token was provided.</response>
    /// <response code="403">The authenticated user does not have the Admin role.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await userService.GetAllUsersAsync();
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }
}
