using Microsoft.Extensions.Logging;
using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.Instagram.DTOs;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Instagram.Services;

public class InstagramAccountService
{
    private readonly IInstagramClient _instagramClient;
    private readonly IInstagramAccountRepository _instagramAccountRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<InstagramAccountService> _logger;

    public InstagramAccountService(
        IInstagramClient instagramClient,
        IInstagramAccountRepository instagramAccountRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider,
        ILogger<InstagramAccountService> logger)
    {
        _instagramClient = instagramClient;
        _instagramAccountRepository = instagramAccountRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<InstagramOAuthUrlResponse>> GenerateOAuthUrlAsync(string redirectUri, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var state = $"{tenantId}:{Guid.NewGuid():N}";

        await _cacheService.SetAsync($"oauth_state:{state}", tenantId.ToString(), TimeSpan.FromMinutes(10), ct);

        var url = _instagramClient.GetAuthorizationUrl(state, redirectUri);
        return Result.Ok(new InstagramOAuthUrlResponse { AuthorizationUrl = url, State = state });
    }

    public async Task<Result<List<InstagramAccountResponse>>> HandleOAuthCallbackAsync(
        ConnectInstagramCallbackRequest request, CancellationToken ct = default)
    {
        var savedTenantId = await _cacheService.GetAsync<string>($"oauth_state:{request.State}", ct);
        if (string.IsNullOrEmpty(savedTenantId) || Guid.Parse(savedTenantId) != _tenantContext.TenantId)
        {
            return Result.Fail<List<InstagramAccountResponse>>("Sessão ou estado OAuth inválido ou expirado.");
        }
        await _cacheService.RemoveAsync($"oauth_state:{request.State}", ct);

        var tenant = await _tenantRepository.GetByIdAsync(_tenantContext.TenantId, ct);
        if (tenant == null)
            return Result.Fail<List<InstagramAccountResponse>>("Tenant não encontrado.");

        var currentCount = await _instagramAccountRepository.CountByTenantAsync(_tenantContext.TenantId, ct);
        if (currentCount >= tenant.MaxInstagramAccounts)
        {
            return Result.Fail<List<InstagramAccountResponse>>($"Limite de {tenant.MaxInstagramAccounts} conta(s) do Instagram atingido para o seu plano.");
        }

        var tokenResponse = await _instagramClient.ExchangeCodeForTokenAsync(request.Code, request.RedirectUri, ct);
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            return Result.Fail<List<InstagramAccountResponse>>("Falha ao obter token de acesso com a Meta.");
        }

        var longLivedTokenResponse = await _instagramClient.GetLongLivedUserTokenAsync(tokenResponse.AccessToken, ct);
        var userAccessToken = longLivedTokenResponse?.AccessToken ?? tokenResponse.AccessToken;

        var pages = await _instagramClient.GetUserPagesWithInstagramAccountsAsync(userAccessToken, ct);
        var connectedAccounts = new List<InstagramAccountResponse>();

        foreach (var page in pages)
        {
            if (page.InstagramBusinessAccount == null)
            {
                _logger.LogInformation("Page {PageName} ({PageId}) does not have an Instagram Business account connected.", page.Name, page.Id);
                continue;
            }

            var igAccount = page.InstagramBusinessAccount;
            var subscribed = await _instagramClient.SubscribeAppToPageWebhooksAsync(page.Id, page.AccessToken, ct);

            var account = new InstagramAccount
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                InstagramBusinessId = igAccount.Id,
                PageId = page.Id,
                PageName = page.Name,
                Username = igAccount.Username ?? igAccount.Id,
                Name = igAccount.Name ?? page.Name,
                ProfilePictureUrl = igAccount.ProfilePictureUrl,
                AccessToken = page.AccessToken,
                TokenExpiresAt = _dateTimeProvider.UtcNow.AddDays(60),
                Status = InstagramAccountStatus.Active,
                WebhookSubscribed = subscribed,
                Scopes = new[] { 
                "instagram_business_basic","instagram_business_content_publish","instagram_business_manage_comments","instagram_business_manage_messages" },
                CreatedAt = _dateTimeProvider.UtcNow,
                UpdatedAt = _dateTimeProvider.UtcNow
            };

            await _instagramAccountRepository.CreateOrUpdateAsync(account, ct);

            connectedAccounts.Add(new InstagramAccountResponse
            {
                Id = account.Id,
                InstagramBusinessId = account.InstagramBusinessId,
                PageName = account.PageName,
                Username = account.Username,
                Name = account.Name,
                ProfilePictureUrl = account.ProfilePictureUrl,
                Status = account.Status.ToString(),
                WebhookSubscribed = account.WebhookSubscribed,
                TokenExpiresAt = account.TokenExpiresAt,
                CreatedAt = account.CreatedAt
            });
        }

        if (connectedAccounts.Count == 0)
        {
            return Result.Fail<List<InstagramAccountResponse>>("Nenhuma conta do Instagram Profissional/Comercial foi encontrada vinculada às suas Páginas do Facebook.");
        }

        return Result.Ok(connectedAccounts);
    }

    public async Task<Result<IEnumerable<InstagramAccountResponse>>> GetAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await _instagramAccountRepository.GetAllByTenantAsync(_tenantContext.TenantId, ct);
        var response = accounts.Select(a => new InstagramAccountResponse
        {
            Id = a.Id,
            InstagramBusinessId = a.InstagramBusinessId,
            PageName = a.PageName,
            Username = a.Username,
            Name = a.Name,
            ProfilePictureUrl = a.ProfilePictureUrl,
            Status = a.Status.ToString(),
            WebhookSubscribed = a.WebhookSubscribed,
            TokenExpiresAt = a.TokenExpiresAt,
            CreatedAt = a.CreatedAt
        });

        return Result.Ok(response);
    }

    public async Task<Result> DisconnectAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _instagramAccountRepository.GetByIdAsync(id, _tenantContext.TenantId, ct);
        if (account == null)
            return Result.Fail("Conta do Instagram não encontrada.");

        await _instagramAccountRepository.UpdateStatusAsync(id, _tenantContext.TenantId, InstagramAccountStatus.Disconnected, ct);
        return Result.Ok();
    }
}