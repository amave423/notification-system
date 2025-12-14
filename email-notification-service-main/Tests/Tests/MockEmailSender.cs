using Api.Models;
using Domain.Contracts;

namespace Tests.Tests;

public class MockEmailSender : IEmailSender
{
    private int _timesToFail;

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (_timesToFail > 0)
        {
            _timesToFail -= 1;
            throw new InvalidOperationException("Simulated email sending failure");
        }

        return Task.CompletedTask;
    }

    public void FailMock(int timesToFail)
    {
        _timesToFail = timesToFail;
    }
}