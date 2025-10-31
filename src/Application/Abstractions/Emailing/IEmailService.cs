namespace Application.Abstractions.Emailing;

public interface IEmailService
{
    Task SendEmailAsync(IEnumerable<string> recipients, string subject, string textBody, string? htmlBody = null);
}
