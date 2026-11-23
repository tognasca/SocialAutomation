using SocialAutomation.Application.Instagram.Services;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Worker.Workers;

public class InstagramWebhookWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<InstagramWebhookWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IInstagramWebhookEventRepository>();
                var item = await repository.ClaimNextAsync(stoppingToken);
                if (item is not null)
                {
                    try
                    {
                        var processor = scope.ServiceProvider.GetRequiredService<InstagramWebhookProcessor>();
                        if (item.TenantId is not Guid tenantId || item.InstagramAccountId is not Guid accountId)
                        {
                            await repository.UpdateStatusAsync(item.Id, WebhookEventStatus.Ignored,
                                "Conta do Instagram não vinculada ao receber o evento.", stoppingToken);
                            continue;
                        }

                        await processor.ProcessPayloadAsync(item.Payload, tenantId, accountId, stoppingToken);
                        await repository.UpdateStatusAsync(item.Id, WebhookEventStatus.Processed, ct: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Falha no processamento do webhook {Id}.", item.Id);
                        await repository.UpdateStatusAsync(item.Id, WebhookEventStatus.Failed,
                            "Falha no processamento. Verifique o inbox e o Instagram antes de reprocessar.");
                    }

                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao consultar eventos do Instagram.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
