namespace SmsSenderService.Models;

public class SmsResponse
{
    public Guid MessageId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public DateTime SentAt { get; set; }
    public string? Error { get; set; }
}