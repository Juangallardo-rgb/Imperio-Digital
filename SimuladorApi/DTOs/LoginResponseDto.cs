namespace SimuladorApi.DTOs
{
    public class LoginResponseDto
    {
        public string Message { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public bool MustChangePassword { get; set; }
    }
}
