using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UserService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MailKit.Security;
using MailKit.Net.Smtp;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IEmailService emailService,
        ILogger<TestController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("send-test-email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendTestEmail(
        [FromQuery]
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        string email)
    {
        try
        {
            _logger.LogInformation("Попытка отправки тестового письма на {Email}", email);
            await _emailService.SendTestEmailAsync(email);
            _logger.LogInformation("Письмо успешно отправлено на {Email}", email);
            return Ok(new { Message = "Письмо отправлено" });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации: {Message}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "Ошибка аутентификации SMTP");
            return StatusCode(503, new { Error = "Ошибка аутентификации на почтовом сервере" });
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(ex, "Ошибка SMTP команды: {StatusCode}", ex.StatusCode);
            return StatusCode(503, new { 
                Error = "Ошибка почтового сервера",
                Details = ex.Message 
            });
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(ex, "Ошибка протокола SMTP");
            return StatusCode(503, new { Error = "Ошибка протокола связи с почтовым сервером" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка: {Message}", ex.Message);
            return StatusCode(500, new { 
                Error = "Внутренняя ошибка сервера",
                Details = ex.Message
            });
        }
    }
}
