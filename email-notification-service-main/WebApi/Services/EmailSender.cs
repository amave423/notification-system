using Api.Models;
using Domain.Contracts;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Polly;
using Polly.Retry;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace EmailNotificationService.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<EmailSender> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailSender(IConfiguration config, ILogger<EmailSender> logger, IHttpClientFactory httpFactory)
    {
        _config = config;
        _logger = logger;
        _httpFactory = httpFactory;

        _retryPolicy = Policy
            .Handle<SmtpCommandException>()
            .Or<AuthenticationException>()
            .Or<ProtocolException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timespan, retryCount, context) =>
                {
                    var recipient = context.TryGetValue("recipient", out var rec)
                        ? rec?.ToString() ?? "unknown"
                        : "unknown";
                    _logger.LogWarning("Retry {RetryCount} after {TimeSpan} for email to {Recipient}", retryCount,
                        timespan, recipient);
                });
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var contextData = new Dictionary<string, object> { ["recipient"] = message.Recipient };

        await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(MailboxAddress.Parse(_config["Smtp:From"] ?? "noreply@example.com"));
            mimeMessage.To.Add(MailboxAddress.Parse(message.Recipient));
            mimeMessage.Subject = message.Subject;

            var builder = new BodyBuilder { HtmlBody = message.Message };

            if (message.Attachments?.Count != 0)
            {
                var httpClient = _httpFactory.CreateClient();
                foreach (var att in message.Attachments)
                {
                    byte[] bytes;
                    if (att.Url.StartsWith("http"))
                    {
                        bytes = await httpClient.GetByteArrayAsync(att.Url, ct);
                    }
                    else if (att.Url.StartsWith("data:"))
                    {
                        var base64 = att.Url.Split(',')[1];
                        bytes = Convert.FromBase64String(base64);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid attachment URL: {Url}", att.Url);
                        continue;
                    }

                    builder.Attachments.Add(att.FileName, bytes,
                        ContentType.Parse(att.ContentType));
                }
            }

            mimeMessage.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"] ?? "25"),
                SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(_config["Smtp:Username"], _config["Smtp:Password"], ct);
            await smtp.SendAsync(mimeMessage, ct);
            await smtp.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent successfully to {Recipient} (ID: {Id})", message.Recipient,
                message.NotificationId);
        }, contextData, cancellationToken);
    }
}