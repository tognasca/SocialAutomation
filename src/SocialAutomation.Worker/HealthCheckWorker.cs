// src/SocialAutomation.Worker/Workers/HealthCheckWorker.cs

namespace SocialAutomation.Worker.Workers;

public class HealthCheckWorker : BackgroundService
{
    private readonly ILogger<HealthCheckWorker> _logger;

    public HealthCheckWorker(ILogger<HealthCheckWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Worker heartbeat at: {Time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}