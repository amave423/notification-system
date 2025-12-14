using Microsoft.Extensions.Options;

namespace SmsSenderService.Services

{
    public class SmsSenderService : ISmsSenderService
    {
        private readonly ILogger<SmsSenderService> _logger;
        private readonly Configuration.SmsSettings _settings;
        private readonly Random _random = new();

        public SmsSenderService(
            ILogger<SmsSenderService> logger,
            IOptions<Configuration.SmsSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<Models.SmsResponse> SendAsync(Models.SmsMessage message)
        {
            _logger.LogInformation(
                "Simulating SMS send to {PhoneNumber}",
                message.PhoneNumber);

            if (_settings.SimulateDelay)
            {
                await Task.Delay(_settings.DelayMilliseconds);
            }

            var isSuccess = _random.NextDouble() <= _settings.SuccessRate;

            if (isSuccess)
            {
                _logger.LogInformation("SMS {MessageId} successfully sent", message.Id);

                return new Models.SmsResponse
                {
                    MessageId = message.Id,
                    Status = "Sent",
                    ProviderMessageId = $"SIM_{Guid.NewGuid():N}",
                    SentAt = DateTime.UtcNow
                };
            }
            else
            {
                var error = "Simulated error";

                _logger.LogWarning("Failed to send SMS {MessageId}: {Error}",
                    message.Id, error);

                return new Models.SmsResponse
                {
                    MessageId = message.Id,
                    Status = "Failed",
                    SentAt = DateTime.UtcNow,
                    Error = error
                };
            }
        }
    }
}
