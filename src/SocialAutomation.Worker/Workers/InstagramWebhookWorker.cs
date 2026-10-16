// src/SocialAutomation.Worker/Workers/InstagramWebhookWorker.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocialAutomation.Application.Instagram.Services;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using StackExchange.Redis;
using System.Text.Json;

namespace SocialAutomation.Worker.Workers;

public class InstagramWebhookWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InstagramWebhookWorker> _logger;

    public InstagramWebhookWorker(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<InstagramWebhookWorker> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Instagram Webhook Worker iniciado escutando a fila Redis.");
        var db = _redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var item = await db.ListLeftPopAsync("queue:instagram-webhook");

                if (item.IsNullOrEmpty)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                var payloadString = item.ToString();
                using var doc = JsonDocument.Parse(payloadString);
                var root = doc.RootElement;
                var eventId = root.GetProperty("Id").GetGuid();
                var innerPayload = root.GetProperty("Payload").GetString();

                using var scope = _scopeFactory.CreateScope();
                var webhookRepo = scope.ServiceProvider.GetRequiredService<IInstagramWebhookEventRepository>();
                var processor = scope.ServiceProvider.GetRequiredService<InstagramWebhookProcessor>();

                await webhookRepo.UpdateStatusAsync(eventId, WebhookEventStatus.Processing, null, stoppingToken);

                // Executa pipeline automatizado: CRM + Inbox + IA + Instagram
                if (!string.IsNullOrEmpty(innerPayload))
                {
                    await processor.ProcessPayloadAsync(innerPayload, stoppingToken);
                }

                await webhookRepo.UpdateStatusAsync(eventId, WebhookEventStatus.Processed, null, stoppingToken);
                _logger.LogInformation("Evento do Webhook {EventId} processado com sucesso pelo pipeline de IA.", eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar item do Webhook na fila Redis.");
            }
        }
    }
}