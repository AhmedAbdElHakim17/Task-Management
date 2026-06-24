using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Requests;
using TaskManagement.Application.Services;

namespace TaskManagement.API.Controllers;

/// <summary>
/// CRUD and status operations for tasks.
/// </summary>
/// <remarks>
/// All endpoints require a valid JWT bearer token.
/// - **Regular users** can only read or modify **their own tasks**.
/// - **Admins** can read or modify any **non-admin user's task**; attempting to access
///   another admin's task returns **403 Forbidden**.
/// </remarks>
[Authorize]
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[Tags("Tasks")]
public class TasksController(ITaskService taskService) : ControllerBase
{

    /// <summary>
    /// Create a new task for the authenticated user.
    /// </summary>
    /// <remarks>
    /// The task is automatically associated with the user extracted from the JWT token.
    /// Initial status is always <c>Pending</c>. Duplicate titles on the same calendar day
    /// are rejected with **400 Bad Request**.
    ///
    /// **Sample request:**
    /// ```json
    /// {
    ///   "title": "Design database schema",
    ///   "description": "ER diagram for the new product module",
    ///   "priority": "High"
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">Task created successfully. Returns the new task details.</response>
    /// <response code="400">Validation failed or duplicate title on the same day.</response>
    /// <response code="401">No valid JWT token was provided.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var result = await taskService.CreateTaskAsync(request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Retrieve all accessible tasks.
    /// </summary>
    /// <remarks>
    /// - **Regular users**: returns their own tasks only.
    /// - **Admins**: returns all tasks except those belonging to other admins.
    /// </remarks>
    /// <response code="200">Returns the list of accessible tasks (may be empty).</response>
    /// <response code="401">No valid JWT token was provided.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllTasks()
    {
        var result = await taskService.GetAllTasksAsync();
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Retrieve a single task by its unique identifier.
    /// </summary>
    /// <remarks>
    /// - **Regular users**: can only access their own tasks.
    /// - **Admins**: can access any non-admin user's task.
    /// The second call to this endpoint is served from **Redis cache**; check the
    /// application logs for a <c>[CACHE HIT]</c> entry to confirm caching is working.
    /// </remarks>
    /// <param name="id">The ID of the task to retrieve.</param>
    /// <response code="200">Returns the task details.</response>
    /// <response code="401">No valid JWT token was provided.</response>
    /// <response code="403">Access denied (different user or another admin's task).</response>
    /// <response code="404">No task found with the given ID.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(Guid id)
    {
        var result = await taskService.GetTaskByIdAsync(id);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode, result.Data);
    }

    /// <summary>
    /// Update the status of a task.
    /// </summary>
    /// <remarks>
    /// Partial update — only the <c>status</c> field is changed.
    /// - **Regular users**: can only update their own tasks.
    /// - **Admins**: can update any non-admin user's task.
    /// The Redis cache entry for this task is invalidated immediately after a successful update.
    ///
    /// **Status values:** <c>Pending</c> | <c>InProgress</c> | <c>Done</c>
    ///
    /// **Sample request:**
    /// ```json
    /// {
    ///   "newStatus": "InProgress"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">The ID of the task to update.</param>
    /// <param name="request">Request body containing the new status value.</param>
    /// <response code="204">Status updated successfully. No body is returned.</response>
    /// <response code="401">No valid JWT token was provided.</response>
    /// <response code="403">Access denied (different user or another admin's task).</response>
    /// <response code="404">No task found with the given ID.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTaskStatus(Guid id, [FromBody] UpdateTaskStatusRequest request)
    {
        var result = await taskService.UpdateTaskStatusAsync(id, request);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { error = result.MsgCode });
        return StatusCode(result.StatusCode);
    }
}
