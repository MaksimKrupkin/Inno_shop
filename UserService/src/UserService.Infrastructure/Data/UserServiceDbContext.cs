using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Data;

public class UserServiceDbContext : DbContext
{
    public UserServiceDbContext() {} 
    public UserServiceDbContext(DbContextOptions<UserServiceDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasQueryFilter(u => u.IsActive); // Soft Delete
    }
}