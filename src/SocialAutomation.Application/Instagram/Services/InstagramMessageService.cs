using SocialAutomation.Application.Common;
using SocialAutomation.Application.Common.Interfaces;
using SocialAutomation.Application.Instagram.DTOs;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Application.Instagram.Services;

public class InstagramMessageService
{
    private readonly IInstagramClient _instagramClient;
    private readonly IInstagramAccountRepository _instagramAccountRepository;
    private readonly ITenantContext _tenantContext;

    public InstagramMessageService(
        IInstagramClient instagramClient,
        IInstagramAccountRepository instagramAccountRepository,
        ITenantContext tenantContext)
    {
        _instagramClient = instagramClient;
        _instagramAccountRepository = instagramAccountRepository;
        _tenantContext = tenantContext;
    }

    public async Task<Result<SendInstagramMessageResponse>> SendDirectMessageAsync(
        SendInstagramMessageRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return Result.Fail<SendInstagramMessageResponse>("A mensagem não pode ser vazia.");

        var account = await _instagramAccountRepository.GetByIdAsync(request.InstagramAccountId, _tenantContext.TenantId, ct);
        if (account == null || account.Status != InstagramAccountStatus.Active)
            return Result.Fail<SendInstagramMessageResponse>("Conta do Instagram não encontrada ou inativa.");

        var response = await _instagramClient.SendTextMessageAsync(
            account.PageId,
            account.AccessToken,
            request.RecipientInstagramScopedId,
            request.Text.Trim(),
            ct);

        if (response == null || string.IsNullOrEmpty(response.MessageId))
            return Result.Fail<SendInstagramMessageResponse>("Falha ao enviar mensagem pelo Instagram. Verifique a janela de 24h ou permissões.");

        return Result.Ok(new SendInstagramMessageResponse
        {
            MessageId = response.MessageId,
            RecipientId = response.RecipientId
        });
    }
}