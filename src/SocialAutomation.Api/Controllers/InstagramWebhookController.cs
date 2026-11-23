using SocialAutomation.Infrastructure.Instagram;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/webhooks/instagram")]
public class InstagramWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<InstagramWebhookController> _logger;
    private readonly IInstagramWebhookEventRepository _webhookEventRepository;
    private readonly IInstagramAccountRepository _instagramAccountRepository;

    public InstagramWebhookController(
        IConfiguration configuration,
        ILogger<InstagramWebhookController> logger,
        IInstagramWebhookEventRepository webhookEventRepository,
        IInstagramAccountRepository instagramAccountRepository)
    {
        _configuration = configuration;
        _logger = logger;
        _webhookEventRepository = webhookEventRepository;
        _instagramAccountRepository = instagramAccountRepository;
    }

    // ==========================================================
    // GET - VERIFICACAO DO WEBHOOK META
    // ==========================================================

    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var configuredToken =
            _configuration["Instagram:WebhookVerifyToken"];

        if (mode == "subscribe" &&
            !string.IsNullOrWhiteSpace(configuredToken) &&
            !string.IsNullOrWhiteSpace(challenge) &&
            verifyToken is not null &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(verifyToken), Encoding.UTF8.GetBytes(configuredToken)))
        {
            _logger.LogInformation(
                "Instagram webhook verification successful.");

            return Content(challenge, "text/plain");
        }

        _logger.LogWarning(
            "Instagram webhook verification rejected.");

        return StatusCode(403);
    }

    // ==========================================================
    // POST - RECEBIMENTO DE EVENTOS INSTAGRAM
    // ==========================================================

    [HttpPost]
    [RequestSizeLimit(1024 * 1024)]
    public async Task<IActionResult> HandleWebhook(
        CancellationToken ct)
    {
        // ------------------------------------------------------
        // 1. LER OS BYTES ORIGINAIS DA REQUISICAO
        // ------------------------------------------------------

        // ------------------------------------------------------
        // 2. OBTER CABECALHO E SECRET
        // ------------------------------------------------------

        var signatureHeader =
            Request.Headers["X-Hub-Signature-256"].ToString();

        var appSecret =
            _configuration["Instagram:AppSecret"];

        if (string.IsNullOrWhiteSpace(appSecret))
        {
            _logger.LogError(
                "Instagram webhook rejected: AppSecret not configured.");

            return StatusCode(
                500,
                "Instagram webhook configuration error.");
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            _logger.LogWarning(
                "Instagram webhook rejected: Missing X-Hub-Signature-256.");

            return Unauthorized(
                "Assinatura do webhook ausente.");
        }

        byte[] payloadBytes;

        using (var buffer = new MemoryStream())
        {
            await Request.Body.CopyToAsync(buffer, ct);
            payloadBytes = buffer.ToArray();
        }

        if (!InstagramWebhookValidator.ValidateSignature(payloadBytes, signatureHeader, appSecret))
        {
            _logger.LogWarning("Instagram webhook rejected: invalid X-Hub-Signature-256.");
            return Unauthorized("Assinatura do webhook inválida.");
        }

        _logger.LogInformation(
            "Instagram webhook signature validated successfully.");

        // ------------------------------------------------------
        // 5. DESSERIALIZAR JSON
        // ------------------------------------------------------

        var rawPayload =
            Encoding.UTF8.GetString(payloadBytes);

        try
        {
            using var doc =
                JsonDocument.Parse(rawPayload);

            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return BadRequest("Payload JSON inválido.");

            var objectType =
                root.TryGetProperty("object", out var objEl) && objEl.ValueKind == JsonValueKind.String
                    ? objEl.GetString() ?? "instagram"
                    : "instagram";

            if (!root.TryGetProperty(
                    "entry",
                    out var entries) ||
                entries.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning(
                    "Instagram webhook received without entry array.");

                return Ok(new
                {
                    status = "EVENT_RECEIVED",
                    entries = 0
                });
            }

            var queuedCount = 0;
            var duplicateCount = 0;

            // --------------------------------------------------
            // 6. PROCESSAR ENTRADAS
            // --------------------------------------------------

            foreach (var entry in entries.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();

                if (entry.ValueKind != JsonValueKind.Object)
                {
                    _logger.LogWarning(
                        "Instagram webhook entry ignored: invalid format.");

                    continue;
                }

                var entryId =
                    entry.TryGetProperty("id", out var idEl)
                        ? idEl.ToString()
                        : string.Empty;

                var entryPayload =
                    entry.GetRawText();

                // --------------------------------------------------
                // 7. LOCALIZAR CONTA INSTAGRAM
                // --------------------------------------------------

                var account =
                    await _instagramAccountRepository
                        .GetByBusinessIdAsync(entryId, ct)
                    ?? await _instagramAccountRepository
                        .GetByPageIdAsync(entryId, ct);

                if (account == null)
                {
                    _logger.LogWarning(
                        "Instagram webhook received for an unlinked account. " +
                        "EntryId={EntryId}",
                        entryId);
                }

                // --------------------------------------------------
                // 8. GERAR IDENTIFICADOR DETERMINISTICO
                // --------------------------------------------------

                var eventHash =
                    Convert.ToHexString(
                        SHA256.HashData(
                            Encoding.UTF8.GetBytes(entryPayload)));

                var eventId =
                    $"{entryId}:{eventHash}";

                var webhookEvent = new InstagramWebhookEvent
                {
                    Id = Guid.NewGuid(),
                    TenantId = account?.TenantId,
                    InstagramAccountId = account?.Id,
                    EventId = eventId,
                    ObjectType = objectType,
                    EntryId = entryId,
                    EventType = "meta.entry",
                    Payload = entryPayload,
                    Status = WebhookEventStatus.Received,
                    CreatedAt = DateTime.UtcNow
                };

                if (!await _webhookEventRepository.TryCreateAsync(webhookEvent, ct))
                {
                    duplicateCount++;
                    continue;
                }

                queuedCount++;

                _logger.LogInformation(
                    "Instagram webhook queued successfully. " +
                    "EventId={EventId}, EntryId={EntryId}",
                    eventId,
                    entryId);
            }

            // --------------------------------------------------
            // 12. RESPONDER A META
            // --------------------------------------------------

            return Ok(new
            {
                status = "EVENT_RECEIVED",
                queued = queuedCount,
                duplicates = duplicateCount
            });
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Instagram webhook rejected: Invalid JSON.");

            return BadRequest(
                "Payload JSON invalido.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing incoming Instagram webhook.");

            return StatusCode(
                500,
                new
                {
                    status = "EVENT_PROCESSING_ERROR"
                });
        }
    }

}
