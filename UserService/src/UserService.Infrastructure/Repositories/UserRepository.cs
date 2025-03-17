using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserServiceDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        UserServiceDbContext context,
        ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {Id}", id);
            throw;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower() && !u.IsDeleted);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Where(u => !u.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    
        if (user != null)
        {
            user.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RestoreAsync(Guid id)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user != null)
        {
            user.IsDeleted = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ConfirmEmailAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            user.EmailConfirmed = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdatePasswordAsync(Guid userId, string newPasswordHash)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            user.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync();
        }
    }

	 public async Task UpdateAsync(User user)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id && !u.IsDeleted)
            ?? throw new KeyNotFoundException($"User with ID {user.Id} not found");

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.PasswordHash = user.PasswordHash;
        existingUser.Role = user.Role;
        existingUser.IsActive = user.IsActive;
        existingUser.EmailConfirmed = user.EmailConfirmed;
        existingUser.ConfirmationToken = user.ConfirmationToken;
        existingUser.PasswordResetToken = user.PasswordResetToken;
        existingUser.PasswordResetExpires = user.PasswordResetExpires;

        await _context.SaveChangesAsync();
    }

	public async Task<User?> GetByPasswordResetTokenAsync(string token)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => 
                u.PasswordResetToken == token && 
                !u.IsDeleted
            );
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users
            .AnyAsync(u => u.Id == id && !u.IsDeleted);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
    
public async Task<User?> GetByConfirmationTokenAsync(string token)
{
    return await _context.Users
        .FirstOrDefaultAsync(u => 
            u.ConfirmationToken == token && 
            !u.IsDeleted && 
            !u.EmailConfirmed
        );
}
}