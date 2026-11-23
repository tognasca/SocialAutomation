
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

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
                    if (!await contentRepo.TryClaimPublicationAsync(item.Id, item.TenantId, stoppingToken))
                        continue;

                    try
                    {
                        var accounts = scope.ServiceProvider.GetRequiredService<IInstagramAccountRepository>();
                        var client = scope.ServiceProvider.GetRequiredService<IInstagramClient>();
                        var account = item.InstagramAccountId is Guid accountId
                            ? await accounts.GetByIdAsync(accountId, item.TenantId, stoppingToken)
                            : null;
                        if (account?.Status != InstagramAccountStatus.Active)
                            throw new InvalidOperationException("Selecione uma conta ativa do Instagram.");
                        if (item.Type is not ("Post" or "Story" or "Reel") || item.MediaUrls.Length != 1 ||
                            !Uri.TryCreate(item.MediaUrls[0], UriKind.Absolute, out var media) ||
                            media.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(media.UserInfo))
                            throw new InvalidOperationException("Tipo ou URL de mídia inválidos para publicação.");

                        var hashtags = string.Join(' ', item.Hashtags.Where(tag => !string.IsNullOrWhiteSpace(tag))
                            .Select(tag => "#" + tag.Trim().TrimStart('#')));
                        var caption = string.Join("\n\n", new[] { item.Caption, item.Cta, hashtags }
                            .Where(part => !string.IsNullOrWhiteSpace(part)));
                        if (string.IsNullOrEmpty(account.UserAccessToken) || account.TokenExpiresAt <= DateTime.UtcNow)
                            throw new InvalidOperationException("Reconecte a conta para publicar.");

                        var publicationId = await client.PublishAsync(account.InstagramBusinessId, account.UserAccessToken,
                            item.Type, media.AbsoluteUri, caption, stoppingToken);
                        await contentRepo.UpdateStatusAsync(item.Id, item.TenantId, "Publicado", publicationId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Falha ao publicar o conteúdo {Id}: {ErrorType}", item.Id, ex.GetType().Name);
                        var reason = ex is InvalidOperationException ? ex.Message : "Falha de comunicação com o Instagram.";
                        await contentRepo.UpdateStatusAsync(item.Id, item.TenantId, "Erro",
                            errorMessage: $"{reason} Confira o Instagram antes de tentar novamente.");
                    }
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