using UserService.Domain.Entities;
using UserService.Application.DTOs;

namespace UserService.Application.Interfaces;

public interface IAuthService
{
    Task<string> LoginAsync(LoginDto loginDto);
    Task<User> RegisterAsync(UserDto userDto);
    Task ConfirmEmailAsync(string token);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    string GenerateJwtToken(User user);
}