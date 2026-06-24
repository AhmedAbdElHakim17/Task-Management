using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Requests;

public class CreateUserByAdminRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public Role Role { get; set; }
}
