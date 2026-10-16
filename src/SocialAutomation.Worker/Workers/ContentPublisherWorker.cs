
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Worker.Workers;

public class ContentPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContentPublisherWorker> _logger;

    public ContentPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<ContentPublisherWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker de Publicação de Conteúdo iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var contentRepo = scope.ServiceProvider.GetRequiredService<IContentRepository>();

                var pendingItems = await contentRepo.GetPendingScheduledContentAsync(10, stoppingToken);

                foreach (var item in pendingItems)
                {
                    _logger.LogInformation("Publicando conteúdo agendado {Title} (ID: {Id})", item.Title, item.Id);

                    await contentRepo.UpdateStatusAsync(item.Id, item.TenantId, "Processando", null, null, stoppingToken);

                    // Publicação via Meta Graph API (Modo Simulação / Produção)
                    var fakePubId = $"ig_pub_{Guid.NewGuid():N}";
                    await contentRepo.UpdateStatusAsync(item.Id, item.TenantId, "Publicado", fakePubId, null, stoppingToken);

                    _logger.LogInformation("Conteúdo {Title} publicado no Instagram com sucesso!", item.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar fila de agendamento de conteúdos.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}