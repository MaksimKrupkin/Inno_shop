using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Application.Interfaces;
using UserService.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace UserService.Application.Services;

public class UserService : IUserService 
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UserDto> _userValidator;
    private readonly ILogger<UserService> _logger;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;

    public UserService(
        IUserRepository userRepository,
        IValidator<UserDto> userValidator,
        ILogger<UserService> logger,
        IAuthService authService,
        IEmailService emailService)
    {
        _userRepository = userRepository;
        _userValidator = userValidator;
        _logger = logger;
        _authService = authService;
        _emailService = emailService;
    }

    public async Task<User> RegisterUserAsync(UserDto userDto)
    {
        await ValidateUserAsync(userDto);
        
        if (await _userRepository.GetByEmailAsync(userDto.Email) != null)
            throw new ArgumentException("Email already exists");

        var user = new User
        {
            Name = userDto.Name,
            Email = userDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
            Role = "User",
            IsActive = true,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            ConfirmationToken = Guid.NewGuid().ToString()
        };

        var createdUser = await _userRepository.AddAsync(user);
        
        await _emailService.SendEmailAsync(
            user.Email,
            "Confirm your email",
            $"<a href='http://localhost:5000/api/auth/confirm-email?token={user.ConfirmationToken}'>Confirm your email</a>");

        return createdUser;
    }

    public async Task<User> GetUserByIdAsync(Guid id)
    {
        User? user = await _userRepository.GetByIdAsync(id);
        return user ?? throw new KeyNotFoundException("User not found");
    }

    public async Task SoftDeleteUserAsync(Guid id)
    {
        await _userRepository.SoftDeleteAsync(id);
    }

    public async Task RestoreUserAsync(Guid id)
    {
        await _userRepository.RestoreAsync(id);
    }

public async Task UpdateUserAsync(Guid id, UpdateUserDto updateDto)
{
    var user = await GetUserByIdAsync(id);
    
    user.Name = updateDto.Name ?? user.Name;
    user.Email = updateDto.Email ?? user.Email;

    if (!string.IsNullOrEmpty(updateDto.NewPassword))
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.NewPassword);
    }

    await _userRepository.UpdateAsync(user); // SaveChangesAsync вызывается внутри UpdateAsync
    // Удалите эту строку: await _userRepository.SaveChangesAsync();
}

    public async Task ConfirmEmailAsync(Guid userId)
    {
        await _userRepository.ConfirmEmailAsync(userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto passwordDto)
    {
        if (passwordDto.NewPassword != passwordDto.ConfirmPassword)
            throw new ArgumentException("Passwords do not match");

        var user = await GetUserByIdAsync(userId);
        
        if (!BCrypt.Net.BCrypt.Verify(passwordDto.OldPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid password");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordDto.NewPassword);
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        
        if (user == null) 
        {
            _logger.LogWarning("User with email {Email} not found", email);
            return null;
        }
        
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for user {Email}", email);
            return null;
        }

        return user;
    }

    public async Task<string> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.EmailConfirmed)
            throw new InvalidOperationException("Email not confirmed");

        return _authService.GenerateJwtToken(user);
    }

    private async Task ValidateUserAsync(UserDto userDto)
    {
        var validationResult = await _userValidator.ValidateAsync(userDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }
	public async Task<User?> GetUserByConfirmationToken(string token)
	{
    	return await _userRepository.GetByConfirmationTokenAsync(token); // Правильное имя метода
	}
}