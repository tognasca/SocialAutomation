using SocialAutomation.Domain.Models.AI;

namespace SocialAutomation.Domain.Interfaces.Services;

public interface IAIProvider
{
    string ProviderName { get; }
    Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default);
}