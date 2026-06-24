namespace TaskManagement.Application.DTOs
{
    public record AuthResponseDto
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
