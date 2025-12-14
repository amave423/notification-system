using MassTransit;
using SmsSenderService.Models;
using SmsSenderService.Services;

namespace SmsSenderService.Consumers;

public class SmsNotificationConsumer : IConsumer<SmsMessage>
{
    private readonly ISmsSenderService _smsSender;
    private readonly ILogger<SmsNotificationConsumer> _logger;

    public SmsNotificationConsumer(
        ISmsSenderService smsSender,
        ILogger<SmsNotificationConsumer> logger)
    {
        _smsSender = smsSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SmsMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation("Processing SMS {MessageId} for {PhoneNumber}",
            message.Id, message.PhoneNumber);

        try
        {
            var result = await _smsSender.SendAsync(message);

            if (result.Status == "Sent")
            {
                _logger.LogInformation("SMS {MessageId} delivered successfully", message.Id);
            }
            else
            {
                _logger.LogError("Failed to send SMS {MessageId}: {Error}",
                    message.Id, result.Error);

                if (result.Error?.Contains("timeout") == true ||
                    result.Error?.Contains("network") == true)
                {
                    throw new Exception($"Retryable error: {result.Error}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SMS {MessageId}", message.Id);
            throw;
        }
    }
}