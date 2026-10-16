using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialAutomation.Api.Filters;
using SocialAutomation.Application.AI.Dtos;
using SocialAutomation.Application.AI.Services;

namespace SocialAutomation.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly AiAgentService _agentService;
    private readonly KnowledgeService _knowledgeService;
    private readonly AiConversationService _conversationService;

    public AiController(
        AiAgentService agentService,
        KnowledgeService knowledgeService,
        AiConversationService conversationService)
    {
        _agentService = agentService;
        _knowledgeService = knowledgeService;
        _conversationService = conversationService;
    }

    [HttpGet("agents")]
    [RequirePermission("ai.manage")]
    public async Task<IActionResult> GetAgents(CancellationToken ct)
    {
        var result = await _agentService.GetAllAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("agents")]
    [RequirePermission("ai.manage")]
    public async Task<IActionResult> CreateAgent([FromBody] UpsertAiAgentRequest request, CancellationToken ct)
    {
        var result = await _agentService.CreateAsync(request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });
        return Ok(new { success = true, data = new { id = result.Data } });
    }

    [HttpPut("agents/{id:guid}")]
    [RequirePermission("ai.manage")]
    public async Task<IActionResult> UpdateAgent(Guid id, [FromBody] UpsertAiAgentRequest request, CancellationToken ct)
    {
        var result = await _agentService.UpdateAsync(id, request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });
        return Ok(new { success = true });
    }

    [HttpGet("knowledge/documents")]
    [RequirePermission("ai.manage")]
    public async Task<IActionResult> GetDocuments(CancellationToken ct)
    {
        var result = await _knowledgeService.GetDocumentsAsync(ct);
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("knowledge/documents")]
    [RequirePermission("ai.manage")]
    public async Task<IActionResult> CreateDocument([FromBody] CreateKnowledgeDocumentRequest request, CancellationToken ct)
    {
        var result = await _knowledgeService.CreateDocumentAsync(request, ct);
        if (!result.Success) return BadRequest(new { success = false, error = result.Error });
        return Ok(new { success = true, data = new { id = result.Data } });
    }

    [HttpPost("process-message")]
    [RequirePermission("conversations.reply")]
    public async Task<IActionResult> ProcessMessage([FromBody] ProcessAiMessageRequest request, CancellationToken ct)
    {
        // Tenant vem do JWT via middleware
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            return Unauthorized();

        var result = await _conversationService.ProcessIncomingMessageAsync(
            tenantId, request.ConversationId, request.IncomingMessage, ct);

        if (!result.Success) return BadRequest(new { success = false, error = result.Error });
        return Ok(new { success = true, data = new { reply = result.Data } });
    }
}