
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialAutomation.Application.Instagram.Services;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Infrastructure.Instagram;
using StackExchange.Redis;
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
    private readonly IConnectionMultiplexer _redis;

    public InstagramWebhookController(
        IConfiguration configuration,
        ILogger<InstagramWebhookController> logger,
        IInstagramWebhookEventRepository webhookEventRepository,
        IInstagramAccountRepository instagramAccountRepository,
        IConnectionMultiplexer redis)
    {
        _configuration = configuration;
        _logger = logger;
        _webhookEventRepository = webhookEventRepository;
        _instagramAccountRepository = instagramAccountRepository;
        _redis = redis;
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
            verifyToken == configuredToken)
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
    public async Task<IActionResult> HandleWebhook(
        CancellationToken ct)
    {
        // ------------------------------------------------------
        // 1. LER OS BYTES ORIGINAIS DA REQUISICAO
        // ------------------------------------------------------

        byte[] payloadBytes;

        using (var buffer = new MemoryStream())
        {
            await Request.Body.CopyToAsync(buffer, ct);
            payloadBytes = buffer.ToArray();
        }

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

        // ------------------------------------------------------
        // 3. VALIDAR FORMATO DA ASSINATURA
        // ------------------------------------------------------

        const string prefix = "sha256=";

        signatureHeader = signatureHeader.Trim();

        if (!signatureHeader.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Instagram webhook rejected: Invalid signature prefix.");

            return Unauthorized(
                "Formato da assinatura invalido.");
        }

        var signatureHex =
            signatureHeader[prefix.Length..];

        if (signatureHex.Length != 64)
        {
            _logger.LogWarning(
                "Instagram webhook rejected: Invalid signature length.");

            return Unauthorized(
                "Tamanho da assinatura invalido.");
        }

        byte[] receivedHash;

        try
        {
            receivedHash =
                Convert.FromHexString(signatureHex);
        }
        catch (FormatException)
        {
            _logger.LogWarning(
                "Instagram webhook rejected: Malformed signature.");

            return Unauthorized(
                "Assinatura malformada.");
        }

        // ------------------------------------------------------
        // 4. VALIDAR HMAC-SHA256
        // ------------------------------------------------------

        var expectedHash = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(appSecret),
            payloadBytes);

        var validSignature =
            CryptographicOperations.FixedTimeEquals(
                expectedHash,
                receivedHash);

        if (!validSignature)
        {
            _logger.LogWarning(
                "Instagram webhook rejected: HMAC mismatch. " +
                "PayloadBytes={PayloadBytes}, SignatureBytes={SignatureBytes}",
                payloadBytes.Length,
                receivedHash.Length);

            return Unauthorized(
                "Assinatura do webhook invalida.");
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

            var objectType =
                root.TryGetProperty("object", out var objEl)
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

                // --------------------------------------------------
                // 9. VERIFICAR DUPLICIDADE
                // --------------------------------------------------

                if (await _webhookEventRepository
                    .ExistsAsync(eventId, ct))
                {
                    _logger.LogInformation(
                        "Duplicate Instagram webhook skipped. " +
                        "EventId={EventId}",
                        eventId);

                    duplicateCount++;
                    continue;
                }

                // --------------------------------------------------
                // 10. PERSISTIR EVENTO
                // --------------------------------------------------

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

                await _webhookEventRepository
                    .CreateAsync(webhookEvent, ct);

                // --------------------------------------------------
                // 11. ENVIAR EVENTO PARA REDIS
                // --------------------------------------------------

                var redisDb =
                    _redis.GetDatabase();

                var queueMessage =
                    JsonSerializer.Serialize(new
                    {
                        webhookEvent.Id,
                        webhookEvent.TenantId,
                        webhookEvent.InstagramAccountId,
                        webhookEvent.EventId,
                        Payload = entryPayload
                    });

                await redisDb.ListRightPushAsync(
                    "queue:instagram-webhook",
                    queueMessage);

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

            // Nao confirmar recebimento quando a persistencia
            // ou o envio para Redis falhar.
            // A Meta podera tentar entregar novamente.
            return StatusCode(
                500,
                new
                {
                    status = "EVENT_PROCESSING_ERROR"
                });
        }
    }

    // ==========================================================
    // POST - SIMULACAO DE DM
    // ==========================================================

    [HttpPost("simulate-dm")]
    public async Task<IActionResult> SimulateIncomingDm(
        [FromBody] SimulateDmRequest request,
        [FromServices] InstagramWebhookProcessor processor,
        CancellationToken ct)
    {
        // Bloqueia simulacao em producao.
        // Endpoint de desenvolvimento nao deve ficar publico.
        if (!HttpContext.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment())
        {
            return NotFound();
        }

        var fakePayload = JsonSerializer.Serialize(new
        {
            id = request.InstagramBusinessId,
            time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            messaging = new[]
            {
                new
                {
                    sender = new
                    {
                        id = request.LeadInstagramScopedId
                    },
                    recipient = new
                    {
                        id = request.InstagramBusinessId
                    },
                    timestamp =
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    message = new
                    {
                        mid = $"mid_simulated_{Guid.NewGuid():N}",
                        text = request.MessageText
                    }
                }
            }
        });

        await processor.ProcessPayloadAsync(
            fakePayload,
            ct);

        return Ok(new
        {
            success = true,
            message =
                "Webhook simulado processado com sucesso."
        });
    }

    // ==========================================================
    // DTO
    // ==========================================================

    public record SimulateDmRequest(
        string InstagramBusinessId,
        string LeadInstagramScopedId,
        string MessageText);
}
