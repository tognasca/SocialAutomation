using SocialAutomation.Domain.Interfaces.Services;

namespace SocialAutomation.Infrastructure.AI;

public interface IAIProviderFactory
{
    IAIProvider GetProvider(string? providerName = null);
}

public class AIProviderFactory : IAIProviderFactory
{
    private readonly IEnumerable<IAIProvider> _providers;

    public AIProviderFactory(IEnumerable<IAIProvider> providers)
    {
        _providers = providers;
    }

    public IAIProvider GetProvider(string? providerName = null)
    {
        var name = string.IsNullOrWhiteSpace(providerName) ? "openai" : providerName.Trim().ToLowerInvariant();
        return _providers.FirstOrDefault(p => p.ProviderName.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? _providers.First();
    }
}