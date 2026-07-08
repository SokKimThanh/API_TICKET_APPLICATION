namespace API_TICKET_APPLICATION.Models
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public UserResponseDto User { get; set; } = null!;
    }
}
