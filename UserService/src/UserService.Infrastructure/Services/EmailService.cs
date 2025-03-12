using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using UserService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace UserService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _username;
    private readonly string _password;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration config,
        ILogger<EmailService> logger)
    {
        _smtpServer = config["EmailSettings:SmtpServer"]!;
        _smtpPort = int.Parse(config["EmailSettings:SmtpPort"]!);
        _username = config["EmailSettings:Username"]!;
        _password = config["EmailSettings:Password"]!;
        _logger = logger;
    }

    public async Task SendTestEmailAsync(string email)
    {
        await SendEmailAsync(email, "Тестовое письмо", "Это тестовое письмо.");
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentNullException(nameof(to));

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("InnoShop", _username));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();

            // Явное указание SecureSocketOptions для порта 465
            await client.ConnectAsync(
                _smtpServer,
                _smtpPort,
                SecureSocketOptions.StartTls // Для порта 587
            );
            
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Письмо отправлено на {Email}", to);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogError(ex, "Ошибка аутентификации в SMTP");
            throw new ApplicationException("Неверные учетные данные", ex);
        }
        catch (SmtpCommandException ex)
        {
            _logger.LogError(ex, "Ошибка SMTP команды: {Status}", ex.StatusCode);
            throw new ApplicationException("Ошибка SMTP", ex);
        }
        catch (SmtpProtocolException ex)
        {
            _logger.LogError(ex, "Ошибка протокола SMTP");
            throw new ApplicationException("Ошибка протокола", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Общая ошибка отправки");
            throw new ApplicationException("Ошибка отправки письма", ex);
        }
    }
}