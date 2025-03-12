namespace UserService.Application.DTOs;

public class ResetPasswordDto
{
    public required  string Email { get; set; }
    public required  string Token { get; set; }
    public required  string NewPassword { get; set; }
}