using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialAutomation.Domain.Interfaces.Services;
using SocialAutomation.Domain.Models.AI;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SocialAutomation.Infrastructure.AI;

public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIProvider> _logger;

    public string ProviderName => "openai";

    public OpenAIProvider(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken ct = default)
    {
        var apiKey = _configuration["AI:ApiKey"] ?? _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiCompletionResponse
            {
                Success = false,
                Error = "AI:ApiKey não configurada no appsettings."
            };
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var payload = new
            {
                model = request.Model,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(httpRequest, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI error {Status}: {Body}", response.StatusCode, body);
                return new AiCompletionResponse
                {
                    Success = false,
                    Error = body,
                    RawJson = body
                };
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            var inputTokens = root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("prompt_tokens", out var pt)
                ? pt.GetInt32() : 0;
            var outputTokens = usage.ValueKind != JsonValueKind.Undefined && usage.TryGetProperty("completion_tokens", out var ctTokens)
                ? ctTokens.GetInt32() : 0;

            return new AiCompletionResponse
            {
                Success = true,
                Content = content.Trim(),
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                RawJson = body
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Falha ao chamar OpenAI");
            return new AiCompletionResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}