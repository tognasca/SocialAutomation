using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SocialAutomation.Domain.Entities;
using SocialAutomation.Domain.Interfaces.Repositories;
using SocialAutomation.Infrastructure.Instagram;

namespace SocialAutomation.IntegrationTests.Api;

public sealed class InstagramGraphTests
{
    [Fact]
    public void SignatureIsVerifiedOverOriginalUtf8Bytes()
    {
        var body = Encoding.UTF8.GetBytes("{ \"message\": \"olá\" }");
        const string secret = "test-secret";
        var signature = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body));

        Assert.True(InstagramWebhookValidator.ValidateSignature(body, signature, secret));
        Assert.False(InstagramWebhookValidator.ValidateSignature(Encoding.UTF8.GetBytes("{\"message\":\"olá\"}"), signature, secret));
        Assert.False(InstagramWebhookValidator.ValidateSignature(body, signature, "different-test-secret"));
    }

    [Theory]
    [InlineData("Post", "image_url", null)]
    [InlineData("Reel", "video_url", "REELS")]
    [InlineData("Story", "image_url", "STORIES")]
    public async Task PublicationReturnsIdOnlyAfterMetaConfirms(string type, string field, string? mediaType)
    {
        var graph = new GraphHandler(
            """{"id":"container"}""", """{"status_code":"FINISHED"}""", """{"id":"published-media"}""");
        var client = CreateClient(graph);

        var id = await client.PublishAsync("account", "test-token", type, "https://example.test/example", "Legenda", default);

        Assert.Equal("published-media", id);
        Assert.Equal(3, graph.Requests.Count);
        Assert.Equal("/v21.0/account/media", graph.Requests[0].Path);
        Assert.Contains(field + "=", graph.Requests[0].Body);
        if (mediaType is not null)
            Assert.Contains("media_type=" + mediaType, graph.Requests[0].Body);
        Assert.Equal(type != "Story", graph.Requests[0].Body.Contains("caption=Legenda"));
        Assert.Equal("/v21.0/container", graph.Requests[1].Path);
        Assert.Equal("/v21.0/account/media_publish", graph.Requests[2].Path);
        Assert.Equal("creation_id=container", graph.Requests[2].Body);
    }

    [Fact]
    public async Task FailedMediaContainerDoesNotPublish()
    {
        var graph = new GraphHandler("""{"id":"container"}""", """{"status_code":"ERROR"}""");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateClient(graph)
            .PublishAsync("account", "test-token", "Post", "https://example.test/photo.jpg", "Legenda", default));

        Assert.Equal(2, graph.Requests.Count);
    }

    [Fact]
    public async Task HashtagSearchUsesHashtagIdToLoadTopMedia()
    {
        var graph = new GraphHandler("""{"data":[{"id":"tag-id"}]}""",
            """{"data":[{"id":"media-id","caption":"Exemplo"}]}""");

        var results = await CreateClient(graph).SearchAsync("account", "test-token", "cotia", true, default);

        Assert.Equal("media-id", results!.Value[0].GetProperty("id").GetString());
        Assert.Equal("/v21.0/ig_hashtag_search", graph.Requests[0].Path);
        Assert.Equal("/v21.0/tag-id/top_media", graph.Requests[1].Path);
    }

    [Fact]
    public async Task ProfessionalProfileSearchUsesBusinessDiscovery()
    {
        var graph = new GraphHandler("""{"business_discovery":{"username":"empresa","followers_count":10}}""");

        var result = await CreateClient(graph).SearchAsync("account", "test-token", "empresa", false, default);

        Assert.Equal(10, result!.Value.GetProperty("followers_count").GetInt32());
        Assert.Contains("business_discovery.username(empresa)", Uri.UnescapeDataString(graph.Requests[0].Query));
    }

    [Fact]
    public async Task DirectMessageUsesThePageAndTheScopedRecipient()
    {
        var graph = new GraphHandler("""{"message_id":"dm-id","recipient_id":"scoped-id"}""");
        var result = await CreateClient(graph).SendTextMessageAsync("page-id", "test-token", "scoped-id", "Olá");

        Assert.Equal("dm-id", result?.MessageId);
        Assert.Equal("/v21.0/page-id/messages", graph.Requests[0].Path);
        Assert.Empty(graph.Requests[0].Query);
        using var json = System.Text.Json.JsonDocument.Parse(graph.Requests[0].Body);
        Assert.Equal("scoped-id", json.RootElement.GetProperty("recipient").GetProperty("id").GetString());
        Assert.Equal("RESPONSE", json.RootElement.GetProperty("messaging_type").GetString());
    }

    private static InstagramClient CreateClient(GraphHandler graph) => new(new HttpClient(graph),
        new ConfigurationBuilder().Build(), NullLogger<InstagramClient>.Instance, new IntegrationLogs());

    private sealed class GraphHandler(params string[] responses) : HttpMessageHandler
    {
        public List<(string Path, string Query, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            var uri = request.RequestUri!;
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((uri.AbsolutePath, uri.Query, body));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responses[Requests.Count - 1]) };
        }
    }

    private sealed class IntegrationLogs : IIntegrationLogRepository
    {
        public Task CreateAsync(IntegrationLog log, CancellationToken ct = default) => Task.CompletedTask;
    }
}
