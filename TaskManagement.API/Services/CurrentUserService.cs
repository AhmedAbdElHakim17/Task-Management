using System.Security.Claims;
using TaskManagement.Domain.Interfaces;

namespace TaskManagement.API.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

            return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
        }
    }

    public string Role =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value
        ?? httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value
        ?? string.Empty;
}
