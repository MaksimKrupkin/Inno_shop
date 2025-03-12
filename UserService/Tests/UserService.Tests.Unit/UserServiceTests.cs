using Xunit;
using Moq;
using UserService.Application.Services;
using UserService.Domain.Interfaces;
using UserService.Application.DTOs;
using FluentValidation;
using Microsoft.Extensions.Logging;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;

namespace UserService.Tests.Unit;

public class UserServiceTests
{
    [Fact]
    public async Task RegisterUser_ShouldReturnUser()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        var mockValidator = new Mock<IValidator<UserDto>>();
        var mockLogger = new Mock<ILogger<UserService.Application.Services.UserService>>();
        var mockAuthService = new Mock<IAuthService>();
        var mockEmailService = new Mock<IEmailService>();

        var service = new UserService.Application.Services.UserService(
            mockRepo.Object,
            mockValidator.Object,
            mockLogger.Object,
            mockAuthService.Object,
            mockEmailService.Object
        );

        var userDto = new UserDto 
        { 
            Name = "Test", 
            Email = "test@example.com", 
            Password = "123" 
        };

        mockRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        mockRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(new User 
            { 
                Id = Guid.NewGuid(),
                Name = "Test",
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                Role = "User",
                IsActive = true,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow
            });

        mockValidator.Setup(v => v.ValidateAsync(It.IsAny<UserDto>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await service.RegisterUserAsync(userDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        mockEmailService.Verify(
            s => s.SendEmailAsync(
                "test@example.com", 
                "Confirm your email", 
                It.IsAny<string>()
            ), 
            Times.Once
        );
    }
}