namespace TaskManagement.Application.DTOs;

public record AuthResultDto
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public static AuthResultDto Create(AuthResponseDto responseDto, UserDto user, string refreshToken)
    {
        return new AuthResultDto
        {
            Token = responseDto.Token,
            ExpiresAt = responseDto.ExpiresAt,
            User = user,
            AccessToken = responseDto.Token,
            RefreshToken = refreshToken
        };
    }
}
