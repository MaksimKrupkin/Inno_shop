using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}