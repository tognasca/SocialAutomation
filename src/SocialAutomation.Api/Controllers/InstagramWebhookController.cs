using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialAutomation.Application.Instagram.Services;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Enums;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Infrastructure.Instagram;
using StackExchange.Redis;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

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
    private readonly IWebHostEnvironment _environment;

    public InstagramWebhookController(
        IConfiguration configuration,
        ILogger<InstagramWebhookController> logger,
        IInstagramWebhookEventRepository webhookEventRepository,
        IInstagramAccountRepository instagramAccountRepository,
        IConnectionMultiplexer redis,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _webhookEventRepository = webhookEventRepository;
        _instagramAccountRepository = instagramAccountRepository;
        _redis = redis;
        _environment = environment;
    }

    /// <summary>
    /// Meta Webhook Verification (hub.challenge)
    /// </summary>
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var configuredVerifyToken = _configuration["Instagram:WebhookVerifyToken"];

        if (mode == "subscribe" && verifyToken == configuredVerifyToken)
        {
            _logger.LogInformation("Meta Webhook verified successfully.");
            return Ok(challenge);
        }

        _logger.LogWarning("Meta Webhook verification failed. Invalid verify token.");
        return Forbid();
    }

    /// <summary>
    /// Ingests Meta Webhook Events (Messages, comments, deliveries)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        // 1. Read Raw Body
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(ct);

        // 2. Validate HMAC Signature
        var signatureHeader = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        var appSecret = _configuration["Instagram:AppSecret"] ?? string.Empty;

        if (string.IsNullOrEmpty(signatureHeader) ||
            !InstagramWebhookValidator.ValidateSignature(rawPayload, signatureHeader, appSecret))
        {
            _logger.LogWarning("Webhook rejected: Invalid X-Hub-Signature-256.");
            return Unauthorized("Assinatura do webhook inválida.");
        }

        // 3. Parse JSON & Extract Events
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            var root = doc.RootElement;
            var objectType = root.TryGetProperty("object", out var objEl) ? objEl.GetString() ?? "instagram" : "instagram";

            if (root.TryGetProperty("entry", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entries.EnumerateArray())
                {
                    var entryId = entry.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

                    // Find connected Instagram account by business id or page id
                    var account = await _instagramAccountRepository.GetByBusinessIdAsync(entryId, ct)
                                  ?? await _instagramAccountRepository.GetByPageIdAsync(entryId, ct);

                    // Generate Unique Event Id for Idempotency
                    var entryPayload = entry.GetRawText();
                    var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(entryPayload))).ToLowerInvariant();
                    var eventId = $"{entryId}:{payloadHash}";

                    if (await _webhookEventRepository.ExistsAsync(eventId, ct))
                    {
                        _logger.LogDebug("Duplicate webhook event skipped: {EventId}", eventId);
                        continue;
                    }

                    var webhookEvent = new InstagramWebhookEvent
                    {
                        Id = Guid.NewGuid(),
                        TenantId = account?.TenantId,
                        InstagramAccountId = account?.Id,
                        EventId = eventId,
                        ObjectType = objectType,
                        EntryId = entryId,
                        EventType = "meta.entry",
                        Payload = entry.GetRawText(),
                        Status = WebhookEventStatus.Received,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _webhookEventRepository.CreateAsync(webhookEvent, ct);

                    // Enqueue to Redis for async Worker processing
                    var db = _redis.GetDatabase();
                    await db.ListRightPushAsync("queue:instagram-webhook", JsonSerializer.Serialize(new
                    {
                        webhookEvent.Id,
                        webhookEvent.TenantId,
                        webhookEvent.InstagramAccountId,
                        webhookEvent.EventId,
                        Payload = entry.GetRawText()
                    }));
                }
            }

            // Always return 200 OK immediately to Meta to avoid retries
            return Ok(new { status = "EVENT_RECEIVED" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming Meta webhook.");
            return Ok(new { status = "EVENT_RECEIVED_WITH_ERROR" });
        }
    }

    /// <summary>
    /// Endpoint de simulação de Webhook (Ambiente Dev) para testar o fluxo DM -> CRM -> Inbox -> IA
    /// </summary>
    [HttpPost("simulate-dm")]
    public async Task<IActionResult> SimulateIncomingDm([FromBody] SimulateDmRequest request, [FromServices] InstagramWebhookProcessor processor, CancellationToken ct)
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        var fakePayload = $"""
        {
          "id": "{{request.InstagramBusinessId}}",
          "time": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
          "messaging": [
            {
              "sender": { "id": "{{request.LeadInstagramScopedId}}" },
              "recipient": { "id": "{{request.InstagramBusinessId}}" },
              "timestamp": {{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}},
              "message": {
                "mid": "mid_simulated_{{Guid.NewGuid():N}}",
                "text": "{{request.MessageText}}"
              }
            }
          ]
        }
        """;

        await processor.ProcessPayloadAsync(fakePayload, ct);
        return Ok(new { success = true, message = "Webhook simulado processado com sucesso pelo pipeline de IA." });
    }

    public record SimulateDmRequest(string InstagramBusinessId, string LeadInstagramScopedId, string MessageText);
}