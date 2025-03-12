namespace UserService.Application.DTOs;

public class ChangePasswordDto
{
    public required  string OldPassword { get; set; }
    public required  string NewPassword { get; set; }
    public required  string ConfirmPassword { get; set; }
}