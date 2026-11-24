using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Domain.Interfaces.Repositories;

namespace SocialAutomation.Api.Controllers;

[AllowAnonymous]
[Route("api/propostas")]
public sealed class PublicProposalsController(ICampaignRepository campaigns) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ViewProposal([FromQuery] Guid codigo, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'";
        var proposal = await campaigns.GetPublicProposalAsync(codigo, ct);
        if (proposal is null)
            return NotFound("Proposta indisponível ou expirada.");

        var encoder = HtmlEncoder.Default;
        var price = proposal.Price.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        var expires = TimeZoneInfo.ConvertTimeFromUtc(proposal.ExpiresAt,
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).ToString("dd/MM/yyyy HH:mm");
        var action = proposal.AcceptedAt.HasValue
            ? """<p role="status"><strong>Seu aceite comercial foi registrado.</strong> A empresa dará continuidade ao atendimento.</p>"""
            : $"""<form method="post" action="/api/propostas"><input type="hidden" name="codigo" value="{codigo:D}"><button type="submit">Registrar meu aceite</button></form>""";
        return Content($$"""
            <!doctype html>
            <html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Proposta comercial</title>
            <style>
              body { font: 1rem/1.6 system-ui,sans-serif; background: #f3f5f8; color: #172033; margin: 0; }
              main { max-width: 38rem; margin: 8vh auto; padding: 2rem; background: white; border-radius: 1rem; }
              h1 { line-height: 1.2; } .terms { white-space: pre-wrap; overflow-wrap: anywhere; }
              button { background: #2444a3; color: white; border: 0; border-radius: .5rem; padding: .8rem 1.3rem; font: inherit; }
            </style>
            <main><p>{{encoder.Encode(proposal.CompanyName)}}</p><h1>{{encoder.Encode(proposal.PlanName)}}</h1>
            <p><strong>{{encoder.Encode(price)}}</strong></p><h2>Condições comerciais</h2>
            <p class="terms">{{encoder.Encode(proposal.Terms)}}</p><p>Válida até {{expires}} (Brasília).</p>{{action}}</main></html>
            """, "text/html; charset=utf-8");
    }

    [HttpPost]
    public async Task<IActionResult> Accept([FromForm] Guid codigo, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        if (!await campaigns.AcceptProposalAsync(codigo, ct))
            return NotFound("Proposta indisponível ou expirada.");

        return RedirectToAction(nameof(ViewProposal), new { codigo });
    }
}
