using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class ResetPasswordDto
{
    [Required]
    public required string Token { get; set; }

    [Required]
    [MinLength(8)]
    public required string NewPassword { get; set; }

    [Required]
    [Compare("NewPassword")]
    public required string ConfirmPassword { get; set; }
}