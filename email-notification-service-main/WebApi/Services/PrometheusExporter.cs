using Prometheus;

namespace EmailNotificationService.Services;

public class PrometheusExporter : BackgroundService
{
    private MetricServer? _metricServer;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _metricServer = new MetricServer("localhost", 1234, "metrics/");
        _metricServer.Start();

        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested) await Task.Delay(1000, stoppingToken);
            _metricServer?.Stop();
        }, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _metricServer?.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _metricServer?.Stop();
        base.Dispose();
    }
}