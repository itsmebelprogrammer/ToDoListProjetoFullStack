using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ToDoListProjeto.Api.Services;
using Xunit;

namespace ToDoListProjeto.Tests;

public class AIServiceTests
{
    private static AIService CreateService(HttpMessageHandler handler, string? apiKey = "fake-key")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["Gemini:ApiKey"] = apiKey })
            .Build();

        return new AIService(new HttpClient(handler), config, new Mock<ILogger<AIService>>().Object);
    }

    private static Mock<HttpMessageHandler> HandlerReturning(HttpResponseMessage response)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return mock;
    }

    private static HttpResponseMessage GeminiOkWith(string text) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    candidates = new[] { new { content = new { parts = new[] { new { text } } } } }
                }),
                Encoding.UTF8, "application/json")
        };

    // ─── Sucesso ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ParseTaskFromPrompt_WhenGeminiRespondsWithValidJson_ReturnsTaskModel()
    {
        var json = """{"title":"Reunião urgente","description":"Marcar sala","priority":"Alta","status":"Pendente"}""";
        var service = CreateService(HandlerReturning(GeminiOkWith(json)).Object);

        var result = await service.ParseTaskFromPrompt("Marcar reunião urgente");

        Assert.NotNull(result);
        Assert.Equal("Reunião urgente", result!.Title);
        Assert.Equal("Alta", result.Priority);
        Assert.Equal("Pendente", result.Status);
    }

    [Fact]
    public async Task ParseTaskFromPrompt_WhenGeminiWrapsJsonInMarkdown_StillParsesCorrectly()
    {
        var text = "```json\n{\"title\":\"Tarefa\",\"description\":\"desc\",\"priority\":\"Média\",\"status\":\"Pendente\"}\n```";
        var service = CreateService(HandlerReturning(GeminiOkWith(text)).Object);

        var result = await service.ParseTaskFromPrompt("criar tarefa");

        Assert.NotNull(result);
        Assert.Equal("Tarefa", result!.Title);
    }

    // ─── Erros HTTP ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]        // chave inválida ou ausente
    [InlineData(HttpStatusCode.BadRequest)]          // payload malformado
    [InlineData(HttpStatusCode.TooManyRequests)]     // cota da API esgotada
    [InlineData(HttpStatusCode.InternalServerError)] // erro interno do Gemini
    [InlineData(HttpStatusCode.ServiceUnavailable)]  // Gemini fora do ar
    public async Task ParseTaskFromPrompt_WhenGeminiReturnsHttpError_ReturnsNull(HttpStatusCode statusCode)
    {
        var handler = HandlerReturning(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("{\"error\":\"falha\"}")
        });
        var service = CreateService(handler.Object);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    // ─── Chave de API ausente / inválida ──────────────────────────────────────

    [Fact]
    public async Task ParseTaskFromPrompt_WhenApiKeyIsNull_ReturnsNull()
    {
        var handler = HandlerReturning(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"code\":401,\"message\":\"API key not valid\"}}")
        });
        var service = CreateService(handler.Object, apiKey: null);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    // ─── Resposta vazia / sem conteúdo ────────────────────────────────────────

    [Fact]
    public async Task ParseTaskFromPrompt_WhenCandidatesListIsEmpty_ReturnsNull()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { candidates = Array.Empty<object>() }),
                Encoding.UTF8, "application/json")
        };
        var service = CreateService(HandlerReturning(response).Object);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseTaskFromPrompt_WhenTextIsEmpty_ReturnsNull()
    {
        var service = CreateService(HandlerReturning(GeminiOkWith("")).Object);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseTaskFromPrompt_WhenTextIsWhitespaceOnly_ReturnsNull()
    {
        var service = CreateService(HandlerReturning(GeminiOkWith("   \n  ")).Object);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    // ─── JSON inválido na resposta ────────────────────────────────────────────

    [Fact]
    public async Task ParseTaskFromPrompt_WhenResponseTextHasNoBraces_ReturnsNull()
    {
        var service = CreateService(HandlerReturning(GeminiOkWith("Desculpe, não entendi a solicitação.")).Object);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseTaskFromPrompt_WhenJsonIsMalformed_ReturnsNull()
    {
        var service = CreateService(HandlerReturning(GeminiOkWith("{title: sem aspas, isto e invalido}")).Object);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseTaskFromPrompt_WhenJsonHasOnlyOpenBrace_ReturnsNull()
    {
        var service = CreateService(HandlerReturning(GeminiOkWith("resposta estranha {sem fechar")).Object);

        var result = await service.ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    // ─── Falha de rede ────────────────────────────────────────────────────────

    [Fact]
    public async Task ParseTaskFromPrompt_WhenNetworkIsUnavailable_ReturnsNull()
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Sem conexão com a internet"));

        var result = await CreateService(mock.Object).ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseTaskFromPrompt_WhenRequestTimesOut_ReturnsNull()
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var result = await CreateService(mock.Object).ParseTaskFromPrompt("qualquer coisa");

        Assert.Null(result);
    }
}
