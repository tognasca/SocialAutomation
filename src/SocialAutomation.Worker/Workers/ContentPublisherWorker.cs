
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

                    // Do not report success until a real Meta Graph API publish flow exists.
                    const string publishError = "Publicação automática indisponível: integração com a Meta Graph API ainda não implementada.";
                    await contentRepo.UpdateStatusAsync(item.Id, item.TenantId, "Falha", null, publishError, stoppingToken);

                    _logger.LogWarning("Conteúdo {Title} não foi publicado: integração com a Meta Graph API pendente.", item.Title);
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