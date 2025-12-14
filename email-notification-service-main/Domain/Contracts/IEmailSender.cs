using Api.Models;

namespace Domain.Contracts;

public interface IEmailSender
{
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}