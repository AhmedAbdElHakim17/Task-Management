namespace TaskManagement.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }

    public static RefreshToken Create(string token, Guid userId, int expiryDays)
        => new()
        {
            Token = token,
            UserId = userId,
            ExpiryDate = DateTime.Now.AddDays(expiryDays),
            IsRevoked = false,
            CreatedAt = DateTime.Now
        };
}
