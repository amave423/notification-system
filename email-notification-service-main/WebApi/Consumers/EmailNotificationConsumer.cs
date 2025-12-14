using System.Diagnostics;
using Api.Models;
using Domain.Contracts;
using Domain.Models;
using MassTransit;
using Prometheus;

namespace EmailNotificationService.Consumers;

public class EmailNotificationConsumer(
    IEmailSender emailSender,
    INotificationRepository repo,
    ILogger<EmailNotificationConsumer> logger)
    : IConsumer<NotificationMessage>
{
    private static readonly Counter SentEmails = Metrics.CreateCounter("email_sent_total", "Total emails sent");

    private static readonly Histogram SendLatency =
        Metrics.CreateHistogram("email_send_duration_seconds", "Email send duration");

    private static readonly Counter FailedEmails = Metrics.CreateCounter("email_failed_total", "Total failed emails");

    public async Task Consume(ConsumeContext<NotificationMessage> context)
    {
        var message = context.Message;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (message.RetryCount > 0)
            {
                await repo.UpdateStatusAsync(message.NotificationId, NotificationStatus.Retrying, message.RetryCount);
                logger.LogInformation("Retrying email {Id} (attempt {RetryCount})", message.NotificationId,
                    message.RetryCount);
            }

            await emailSender.SendAsync(message, context.CancellationToken);

            await repo.UpdateStatusAsync(message.NotificationId, NotificationStatus.Sent, message.RetryCount);
            SentEmails.Inc();
            logger.LogInformation("Email processed successfully: {Id} to {Recipient}", message.NotificationId,
                message.Recipient);
        }
        catch (Exception ex)
        {
            var retryCount = message.RetryCount + 1;
            var errorMsg = ex.Message;

            if (retryCount >= message.MaxRetryCount)
            {
                await repo.UpdateStatusAsync(message.NotificationId, NotificationStatus.Failed, retryCount, errorMsg);
                FailedEmails.Inc();
                logger.LogError(ex, "Email failed permanently: {Id} to {Recipient}", message.NotificationId,
                    message.Recipient);
            }
            else
            {
                await context.Publish(new NotificationMessage
                {
                    NotificationId = message.NotificationId,
                    Recipient = message.Recipient,
                    Subject = message.Subject,
                    Message = message.Message,
                    Attachments = message.Attachments,
                    RetryCount = retryCount,
                    MaxRetryCount = message.MaxRetryCount,
                });
                await repo.UpdateStatusAsync(message.NotificationId, NotificationStatus.Retrying, retryCount,
                    errorMsg);
                logger.LogWarning(ex, "Email retrying: {Id} (attempt {RetryCount})", message.NotificationId,
                    retryCount);
            }
        }
        finally
        {
            stopwatch.Stop();
            SendLatency.Observe(stopwatch.Elapsed.TotalSeconds);
        }
    }
}