using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using UserService.Domain.Entities;
using System.Security.Claims;
using FluentValidation;
using Microsoft.Extensions.Logging;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Repositories;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository; // Добавить
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            IUserRepository userRepository, // Добавить
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _userRepository = userRepository; // Инициализировать
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            try
            {
                var user = await _userService.RegisterUserAsync(userDto);
                _logger.LogInformation("User {Email} registered successfully", user.Email);
                return Ok(new { user.Id, user.Email });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Validation error: {Message}", ex.Message);
                return BadRequest(new { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Data conflict: {Message}", ex.Message);
                return Conflict(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Email}", userDto.Email);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                if (currentUserId != id && currentUserRole != "Admin")
                    return Forbid();

                var user = await _userService.GetUserByIdAsync(id);
                return Ok(MapUserToDto(user));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User {UserId} not found", id);
                return NotFound(new { Error = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users.Select(MapUserToDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users list");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

[HttpPut("{id}")]
[Authorize]
public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto updateDto)
{
    try
    {
        var currentUserId = GetCurrentUserId();
        var currentUserRole = GetCurrentUserRole();

        // Проверка подтверждения email через claim
        var isEmailConfirmed = User.FindFirst("email_confirmed")?.Value;
        if (isEmailConfirmed?.ToLower() != "true")
        {
            return Unauthorized(new { Error = "Email не подтвержден" });
        }

        // Проверка прав доступа
        if (currentUserId != id && currentUserRole != "Admin")
        {
            return Forbid();
        }

        await _userService.UpdateUserAsync(id, updateDto);
        return NoContent();
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { Error = "Пользователь не найден" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при обновлении пользователя");
        return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
    }
}

        [HttpDelete("{id}")]
		[Authorize(Roles = "User, Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                await _userService.SoftDeleteUserAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User {UserId} not found", id);
                return NotFound(new { Error = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RestoreUser(Guid id)
        {
            try
            {
                await _userService.RestoreUserAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User {UserId} not found", id);
                return NotFound(new { Error = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring user {UserId}", id);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-endpoint")]
        public IActionResult AdminEndpoint()
        {
            return Ok(new { Message = "Admin access granted" });
        }

        [Authorize]
        [HttpGet("user-endpoint")]
        public IActionResult UserEndpoint()
        {
            var userId = GetCurrentUserId();
            return Ok(new 
            { 
                Message = "User access granted",
                UserId = userId 
            });
        }
        
        [HttpPut("{userId}/status")]
        [Authorize(Roles = "Admin")] // Только для администраторов
        public async Task<IActionResult> UpdateUserStatus(
            Guid userId, 
            [FromBody] UpdateUserStatusDto dto) // Используем DTO
        {
            try
            {
                _logger.LogInformation("Updating status for user {UserId}", userId);
        
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return NotFound();
                }

                user.IsActive = dto.IsActive;
                await _userRepository.UpdateAsync(user);
        
                _logger.LogInformation("User {UserId} status updated to {Status}", 
                    userId, dto.IsActive);
            
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user {UserId}", userId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

private Guid GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (Guid.TryParse(userIdClaim, out Guid userId))
    {
        return userId;
    }
    throw new UnauthorizedAccessException("Invalid user ID in token");
}

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value 
                   ?? throw new UnauthorizedAccessException("Role claim missing");
        }

        private static object MapUserToDto(User user)
        {
            return new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.IsActive,
                user.EmailConfirmed,
                CreatedAt = user.CreatedAt.ToString("o")
            };
        }
    }
}