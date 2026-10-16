using SocialAutomation.Application.Common;
using SocialAutomation.Application.Content.DTOs;
using SocialAutomation.Domain.Interfaces.Services;
using SocialAutomation.Domain.Models.AI;
using SocialAutomation.Infrastructure.AI;
using System.Text.Json;

namespace SocialAutomation.Application.Content.Services;

public class ContentGeneratorService
{
    private readonly IAIProviderFactory _aiProviderFactory;

    public ContentGeneratorService(IAIProviderFactory aiProviderFactory)
    {
        _aiProviderFactory = aiProviderFactory;
    }

    public async Task<Result<GeneratedAiContentResponse>> GenerateAsync(GenerateAiContentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
            return Result.Fail<GeneratedAiContentResponse>("O tema é obrigatório.");

        var provider = _aiProviderFactory.GetProvider("openai");


        var prompt = $$"""
                            Você é um especialista em Marketing Digital e Instagram.
                            Gere uma publicação completa para o Instagram no formato: {{request.ContentType}}.
                            Tema: {{request.Topic}}
                            Produto/Serviço: {{request.ProductOrService ?? "Geral"}}
                            Tom: {{request.Tone}}
                            Público-alvo: {{request.TargetAudience}}

                            REGRAS IMPORTANTES:
                            - Responda SOMENTE com JSON válido.
                            - NÃO use Markdown.
                            - NÃO utilize ```json ou ``` na resposta.
                            - NÃO adicione explicações antes ou depois do JSON.
                            - Todos os campos devem existir.
                            - "hashtags" deve ser sempre um array de strings.
                            - "reelScenes" deve ser sempre um array.
                            - Para conteúdos que não sejam Reel, "reelScenes" deve ser um array vazio.
                            - O JSON deve estar corretamente formatado e pronto para desserialização.
                            - Não inclua comentários dentro do JSON.

                            Formato obrigatório:

                            {
                              "title": "Título atraente para o post/reel",
                              "caption": "Legenda engajadora dividida em parágrafos curtos com emojis",
                              "cta": "Chamada para ação clara, por exemplo: Comente QUERO",
                              "hashtags": [
                                "#tag1",
                                "#tag2",
                                "#tag3"
                              ],
                              "visualSuggestion": "Descrição detalhada da imagem, carrossel ou vídeo a ser criado",
                              "reelScenes": [
                                {
                                  "sceneOrder": 1,
                                  "scriptText": "Texto falado ou exibido na tela",
                                  "visualDescription": "O que aparece visualmente nesta cena",
                                  "durationSeconds": 5
                                }
                              ]
                            }
                            """;


        var completion = await provider.CompleteAsync(new AiCompletionRequest
        {
            Model = "gpt-4o-mini",
            Temperature = 0.7m,
            MaxTokens = 1200,
            Messages = new List<AiChatMessage>
            {
                new() { Role = "system", Content = "Você gera conteúdo comercial estruturado em JSON para redes sociais." },
                new() { Role = "user", Content = prompt }
            }
        }, ct);

        if (!completion.Success || string.IsNullOrWhiteSpace(completion.Content))
            return Result.Fail<GeneratedAiContentResponse>(completion.Error ?? "Falha ao gerar conteúdo com a IA.");

        try
        {
            var jsonStart = completion.Content.IndexOf('{');
            var jsonEnd = completion.Content.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = completion.Content[jsonStart..(jsonEnd + 1)];
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var result = new GeneratedAiContentResponse
                {
                    Title = root.GetProperty("title").GetString() ?? request.Topic,
                    Caption = root.GetProperty("caption").GetString() ?? "",
                    Cta = root.GetProperty("cta").GetString() ?? "",
                    VisualSuggestion = root.GetProperty("visualSuggestion").GetString() ?? "",
                    Hashtags = root.TryGetProperty("hashtags", out var tags) && tags.ValueKind == JsonValueKind.Array
                        ? tags.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
                        : new List<string>()
                };

                if (root.TryGetProperty("reelScenes", out var scenes) && scenes.ValueKind == JsonValueKind.Array)
                {
                    result.ReelScenes = scenes.EnumerateArray().Select(s => new ReelSceneDto
                    {
                        SceneOrder = s.GetProperty("sceneOrder").GetInt32(),
                        ScriptText = s.GetProperty("scriptText").GetString() ?? "",
                        VisualDescription = s.TryGetProperty("visualDescription", out var vd) ? vd.GetString() : null,
                        DurationSeconds = s.TryGetProperty("durationSeconds", out var ds) ? ds.GetInt32() : 5
                    }).ToList();
                }

                return Result.Ok(result);
            }
        }
        catch { }

        return Result.Ok(new GeneratedAiContentResponse
        {
            Title = request.Topic,
            Caption = completion.Content,
            Cta = "Comente para saber mais!",
            Hashtags = new List<string> { "#instagram", "#marketing" }
        });
    }
}