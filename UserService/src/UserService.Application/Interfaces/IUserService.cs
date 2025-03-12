using UserService.Application.DTOs;
using UserService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserService.Application.Interfaces;

public interface IUserService
{
    Task<User> RegisterUserAsync(UserDto userDto);
    Task<User> GetUserByIdAsync(Guid id);
    Task UpdateUserAsync(Guid id, UpdateUserDto updateDto);
    Task SoftDeleteUserAsync(Guid id);
    Task RestoreUserAsync(Guid id);
    Task ConfirmEmailAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto passwordDto);
    Task<string> LoginAsync(LoginDto loginDto);
    Task<IEnumerable<User>> GetAllUsersAsync();
	Task<User?> AuthenticateAsync(string email, string password);
	Task<User?> GetUserByConfirmationToken(string token);
}