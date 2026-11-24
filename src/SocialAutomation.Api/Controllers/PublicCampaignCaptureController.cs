using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("PublicCampaignCapture")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/public/campaigns/{id:guid}")]
public sealed class PublicCampaignCaptureController(ICampaignRepository campaigns) : ControllerBase
{
    [HttpPost("visit")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Visit(Guid id, VisitRequest request, CancellationToken ct)
    {
        if (request.Visitor == Guid.Empty)
            return BadRequest();
        var channel = request.Channel?.Trim().ToLowerInvariant() ?? "";
        if (!Regex.IsMatch(channel, @"\A[a-z0-9][a-z0-9_-]{0,79}\z"))
            channel = "direto";
        var capture = await campaigns.VisitCaptureAsync(id, request.Visitor, channel, ct);
        return capture is null ? NotFound() : Ok(capture);
    }

    [HttpPost("diagnosis")]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> Diagnose(Guid id, DiagnosisRequest request, CancellationToken ct)
    {
        if (request.Visitor == Guid.Empty || request.Answers is not { Length: >= 1 and <= 5 } ||
            request.Answers.Any(answer => string.IsNullOrWhiteSpace(answer) || answer.Length > 300))
            return BadRequest(new { error = "Responda a todas as perguntas com até 300 caracteres." });

        PublicCampaignContact? contact = null;
        if (request.Consent)
        {
            var name = request.Name?.Trim() ?? "";
            var email = request.Email?.Trim().ToLowerInvariant();
            var rawPhone = request.Phone?.Trim() ?? "";
            var phone = Regex.Replace(rawPhone, "[^0-9]", "");
            if (string.IsNullOrEmpty(email))
                email = null;
            if (name.Length is < 1 or > 255 ||
                (email is not null && (email.Length > 255 || !new EmailAddressAttribute().IsValid(email))) ||
                (rawPhone.Length > 0 && (rawPhone.Length > 30 || !Regex.IsMatch(rawPhone, @"\A[+0-9() .-]+\z") ||
                    phone.Length is < 10 or > 15)) || (email is null && phone.Length == 0))
                return BadRequest(new { error = "Informe seu nome e um e-mail ou WhatsApp válido." });
            contact = new PublicCampaignContact(name, email, phone.Length == 0 ? null : phone);
        }
        else if (!string.IsNullOrWhiteSpace(request.Email) || !string.IsNullOrWhiteSpace(request.Phone) ||
            !string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Autorize o retorno para compartilhar seu contato." });

        var data = await campaigns.DiagnoseCaptureAsync(id, request.Visitor,
            request.Answers.Select(answer => answer.Trim()).ToArray(), contact, ct);
        return data is null ? Conflict(new { error = "A campanha mudou ou está indisponível. Atualize a página." }) : Ok(data);
    }

    [HttpPost("book")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Book(Guid id, BookRequest request, CancellationToken ct)
    {
        if (request.Visitor == Guid.Empty || request.SlotId < 1)
            return BadRequest();
        var slot = await campaigns.BookCaptureAsync(id, request.Visitor, request.SlotId, ct);
        return slot is null ? Conflict(new { error = "Horário indisponível, já existe uma reserva ou falta confirmar seu contato." }) : Ok(slot);
    }

    [HttpPost("proposal")]
    [RequestSizeLimit(4096)]
    public async Task<IActionResult> Proposal(Guid id, VisitorRequest request, CancellationToken ct)
    {
        if (request.Visitor == Guid.Empty)
            return BadRequest();
        var message = await campaigns.ProposeCaptureAsync(id, request.Visitor, ct);
        if (message is null)
            return NotFound(new { error = "Proposta automática indisponível para esta campanha." });
        var lastSpace = message.LastIndexOf(' ');
        if (lastSpace >= 0 && Uri.TryCreate(message[(lastSpace + 1)..], UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https")
            return Ok(new { message = message[..lastSpace], url = uri.AbsoluteUri });
        return Ok(new { message });
    }

    public sealed record VisitRequest(Guid Visitor, string? Channel);
    public sealed record VisitorRequest(Guid Visitor);
    public sealed record DiagnosisRequest(Guid Visitor, string[] Answers, bool Consent, string? Name, string? Email, string? Phone);
    public sealed record BookRequest(Guid Visitor, long SlotId);
}
