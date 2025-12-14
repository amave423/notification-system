namespace Api.Models;

public class NotificationMessage
{
    public Guid NotificationId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<AttachmentDto>? Attachments { get; set; }
    public int RetryCount { get; set; } = 0;
    public int MaxRetryCount { get; set; }

    public class AttachmentDto
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
    }
}