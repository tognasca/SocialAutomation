using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialAutomation.Domain.Interfaces.Services;
using SocialAutomation.Domain.Models.AI;

namespace SocialAutomation.Infrastructure.AI;

public sealed class OllamaProvider(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OllamaProvider> logger) : IAIProvider
{
    public string ProviderName => "ollama";

    public async Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
    {
        var baseUrl = configuration["AI:Ollama:BaseUrl"] ?? "http://127.0.0.1:11434";
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var address) ||
            address.Scheme is not ("http" or "https"))
            return Failure("O endereço do Ollama não está configurado corretamente.");

        try
        {
            using var response = await httpClient.PostAsJsonAsync(new Uri(address, "api/chat"), new
            {
                model = request.Model,
                messages = request.Messages.Select(message => new { role = message.Role, content = message.Content }).ToArray(),
                stream = false,
                format = request.ExpectJson ? "json" : null,
                options = new { temperature = request.Temperature, num_predict = request.MaxTokens }
            }, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Ollama recusou a geração para {Model}. HTTP {StatusCode}.", request.Model, (int)response.StatusCode);
                return Failure(response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? "Modelo não disponível no Ollama. Instale o modelo selecionado e tente novamente."
                    : "O Ollama recusou a geração da resposta.");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            var content = root.GetProperty("message").GetProperty("content").GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(content))
                return Failure("O Ollama retornou uma resposta vazia.");

            return new AiCompletionResponse
            {
                Success = true,
                Content = content,
                InputTokens = root.TryGetProperty("prompt_eval_count", out var input) ? input.GetInt32() : 0,
                OutputTokens = root.TryGetProperty("eval_count", out var output) ? output.GetInt32() : 0
            };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("O tempo limite de geração no Ollama foi atingido.");
            return Failure("O Ollama excedeu o tempo limite de resposta.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            logger.LogWarning("Falha ao consultar Ollama: {ErrorType}.", ex.GetType().Name);
            return Failure("Não foi possível obter uma resposta do Ollama.");
        }
    }

    private static AiCompletionResponse Failure(string error) => new() { Error = error };
}
