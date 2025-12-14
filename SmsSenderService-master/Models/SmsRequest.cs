namespace SmsSenderService.Models;

public class SmsRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Sender { get; set; }
    public bool IsFlash { get; set; } = false;
    public string? CallbackData { get; set; }
}