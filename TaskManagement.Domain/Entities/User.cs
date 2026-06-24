using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public static User Create(string name, string email, string password, Role role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = password,
            Role = role,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
    }
}
