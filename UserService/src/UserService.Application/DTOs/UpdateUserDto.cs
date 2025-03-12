namespace UserService.Application.DTOs;

public class UpdateUserDto
{
    public required  string? Name { get; set; }
    public required  string? Email { get; set; }
    public required  string? NewPassword { get; set; }
}