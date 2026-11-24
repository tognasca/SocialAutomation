using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SocialAutomation.Domain.Models.AI;
using SocialAutomation.Infrastructure.AI;

namespace SocialAutomation.IntegrationTests.Api;

public class OllamaProviderTests
{
    [Fact]
    public async Task GeneratesStructuredReplyOnConfiguredHostWithoutOpenAiCredentials()
    {
        using var client = new HttpClient(new StubHandler(async (request, ct) =>
        {
            Assert.Equal("http://ollama.test:11434/api/chat", request.RequestUri!.AbsoluteUri);
            Assert.Null(request.Headers.Authorization);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            Assert.Equal("qwen2.5:3b", json.RootElement.GetProperty("model").GetString());
            Assert.Equal("json", json.RootElement.GetProperty("format").GetString());
            Assert.False(json.RootElement.GetProperty("stream").GetBoolean());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"message":{"content":"{\"reply\":\"Olá\"}"},"prompt_eval_count":21,"eval_count":8}""")
            };
        }));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:Ollama:BaseUrl"] = "http://ollama.test:11434"
        }).Build();
        var provider = new OllamaProvider(client, configuration, NullLogger<OllamaProvider>.Instance);

        var result = await provider.CompleteAsync(new AiCompletionRequest { Model = "qwen2.5:3b", ExpectJson = true });

        Assert.True(result.Success);
        Assert.Equal("""{"reply":"Olá"}""", result.Content);
        Assert.Equal(21, result.InputTokens);
        Assert.Equal(8, result.OutputTokens);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
