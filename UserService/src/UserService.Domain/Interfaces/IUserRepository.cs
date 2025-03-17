using UserService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserService.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> AddAsync(User user);
    Task SoftDeleteAsync(Guid id);
    Task RestoreAsync(Guid id);
    Task UpdateAsync(User user);
    Task ConfirmEmailAsync(Guid userId);
    Task UpdatePasswordAsync(Guid userId, string newPasswordHash);
    Task<bool> ExistsAsync(Guid id);
    Task SaveChangesAsync();
	Task<User?> GetByPasswordResetTokenAsync(string token);
    Task<User?> GetByConfirmationTokenAsync(string token); // Один метод вместо двух
}