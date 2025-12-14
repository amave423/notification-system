namespace SmsSenderService.Models;

public class SmsMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Sender { get; set; } = "SERVICE";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsFlash { get; set; } = false;
    public string? CallbackData { get; set; }
}