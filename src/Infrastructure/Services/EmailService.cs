using Application.Abstractions.Emailing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly SmtpSettings _smtpSettings;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailService(IOptions<SmtpSettings> options, ILogger<EmailService> logger)
    {
        _logger = logger;
        _smtpSettings = options.Value ?? throw new ArgumentNullException(nameof(options));

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) => _logger.LogWarning(exception, "Retry {RetryAttempt} for email sending after {Delay}s", retryCount, timeSpan.TotalSeconds));
    }

    public async Task SendEmailAsync(
        IEnumerable<string> recipients,
        string subject,
        string textBody,
        string? htmlBody = null)
    {
        using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
            EnableSsl = _smtpSettings.EnableSsl
        };

        foreach (string recipient in recipients)
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.From),
                Subject = subject,
                Body = textBody,
                IsBodyHtml = false
            };

            mailMessage.To.Add(recipient);

            // Use alternate views for proper MIME support
            var plainView = AlternateView.CreateAlternateViewFromString(textBody, null, MediaTypeNames.Text.Plain);
            mailMessage.AlternateViews.Add(plainView);

            if (!string.IsNullOrWhiteSpace(htmlBody))
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
                mailMessage.AlternateViews.Add(htmlView);
            }

            try
            {
                await _retryPolicy.ExecuteAsync(() => smtpClient.SendMailAsync(mailMessage));
                _logger.LogInformation("Email sent to {Recipient}", recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient} after retries", recipient);
            }
        }
    }
}
