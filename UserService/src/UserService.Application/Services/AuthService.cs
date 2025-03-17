using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using UserService.Domain.Entities;
using UserService.Application.DTOs;
using UserService.Domain.Interfaces;
using UserService.Application.Interfaces;
using UserService.Application.Models;
using FluentValidation;
using System.Text;
using UserService.Application.Exceptions;

namespace UserService.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IValidator<UserDto> _validator;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IEmailService emailService,
        IValidator<UserDto> validator,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _validator = validator;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<string> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            throw new AuthException("Invalid credentials");
        
        if (!user.EmailConfirmed)
            throw new AuthException("Email not confirmed");

        return GenerateJwtToken(user);
    }

    public async Task<User> RegisterAsync(UserDto userDto)
    {
        await _validator.ValidateAndThrowAsync(userDto);
        
        if (await _userRepository.GetByEmailAsync(userDto.Email) != null)
            throw new AuthException("Email already exists");

        var user = new User
        {
            Name = userDto.Name,
            Email = userDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
            ConfirmationToken = GenerateSecureToken(),
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await SendConfirmationEmail(user);
        
        return user;
    }

    public async Task ConfirmEmailAsync(string token)
    {
        var user = await _userRepository.GetByConfirmationTokenAsync(token)
            ?? throw new AuthException("Неверный токен");
        
        user.EmailConfirmed = true;
        user.ConfirmationToken = null;
        await _userRepository.UpdateAsync(user);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return;

        user.PasswordResetToken = GenerateSecureToken();
        user.PasswordResetExpires = DateTime.UtcNow.AddHours(1);
        await _userRepository.UpdateAsync(user);

        var resetLink = $"http://localhost:5000/api/auth/reset-password?token={user.PasswordResetToken}";
        await _emailService.SendEmailAsync(
            user.Email,
            "Password Reset Request",
            $"Reset your password by clicking <a href='{resetLink}'>here</a>.");
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
            throw new AuthException("Passwords do not match");

        var user = await _userRepository.GetByPasswordResetTokenAsync(dto.Token);
        if (user == null || user.PasswordResetExpires < DateTime.UtcNow)
            throw new AuthException("Invalid or expired token");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpires = null;

        await _userRepository.UpdateAsync(user);
    }

    public string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("email_confirmed", user.EmailConfirmed.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task SendConfirmationEmail(User user)
    {
        var confirmationLink = $"http://192.168.0.105:5000/api/auth/confirm-email?token={user.ConfirmationToken}";
        
        await _emailService.SendEmailAsync(
            user.Email,
            "Подтвердите ваш email",
            $"Пожалуйста, подтвердите ваш email, перейдя по <a href='{confirmationLink}'>ссылке</a>");
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}