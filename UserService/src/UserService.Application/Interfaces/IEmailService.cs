namespace UserService.Application.Interfaces;

public interface IEmailService
{
    Task SendTestEmailAsync(string email); // Метод из интерфейса
    Task SendEmailAsync(string to, string subject, string body); // Дополнительный метод
}