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
    await _userRepository.UpdateAsync(user); // SaveChanges уже вызывается внутри UpdateAsync
}
 public Task ForgotPasswordAsync(string email)
{
    throw new NotImplementedException();
}

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        throw new NotImplementedException();
    }

public string GenerateJwtToken(User user)
{
    var claims = new[]
    {
        // Используем стандартные ClaimTypes
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
    // Исправленный URL с указанием локального адреса
    var confirmationLink = $"http://192.168.0.105:5000/api/auth/confirm-email?token={user.ConfirmationToken}";
    
    await _emailService.SendEmailAsync(
        user.Email,
        "Подтвердите ваш email",
        $"Пожалуйста, подтвердите ваш email, перейдя по <a href='{confirmationLink}'>ссылке</a>");
}

    private static string GenerateSecureToken() => 
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}

public class AuthException : Exception
{
    public AuthException(string message) : base(message) { }
}