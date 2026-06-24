namespace TaskManagement.Application.Options;

public class RedisOptions
{
    public string ConnectionString { get; set; }
    public int ExpiryMinutes { get; set; } = 10;
}
