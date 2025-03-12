using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class UserDto
{
	[Required]
    public required string Name { get; set; }
	[Required]
    public required string Email { get; set; }
	[Required]
    public required string Password { get; set; }
}