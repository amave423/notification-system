namespace SmsSenderService.Configuration;

public class RabbitMqSettings
{
    public string Host { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SmsQueue { get; set; } = "sms.notifications";
    public string RetryQueue { get; set; } = "sms.notifications.retry";
    public int MaxRetries { get; set; } = 3;
}

public class SmsSettings
{
    public bool SimulateDelay { get; set; } = true;
    public int DelayMilliseconds { get; set; } = 1000;
    public double SuccessRate { get; set; } = 0.95;
    public int MaxMessageLength { get; set; } = 1600;
    public string DefaultSender { get; set; } = "SERVICE";
}