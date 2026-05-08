using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToDoListProjeto.Api.Models;

namespace ToDoListProjeto.Api.Services;

public class AIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;
    private readonly string? _apiKey;

    public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"];
    }

    public async Task<TaskCreateModel?> ParseTaskFromPrompt(string userPrompt)
    {
        var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";

        var prompt = $@"
            Você é um assistente especialista em analisar solicitações e extrair informações para tarefas.
            Sua resposta deve ser APENAS o objeto JSON, começando com '{{' e terminando com '}}', sem markdown ou texto extra.

            O JSON deve ter os campos: 'title', 'description', 'priority' ('Baixa', 'Média', 'Alta'), e 'status' ('Pendente', 'Em Andamento', 'Concluída').

            --- REGRAS DETALHADAS DE INTERPRETAÇÃO ---

            **PARA O CAMPO 'STATUS':**
            - Use 'Em Andamento' para verbos como: 'terminar', 'finalizar', 'continuar', 'revisar'.
            - Use 'Pendente' para verbos como: 'começar', 'criar', 'agendar', 'comprar', 'ligar para'.
            - Se for ambíguo, o padrão é 'Pendente'.

            **PARA O CAMPO 'PRIORITY':**
            - Use 'Alta' para palavras de urgência como: 'urgente', 'crítico', 'importante', 'pra ontem', 'não esquecer'.
            - Para tarefas pessoais ou de trabalho sem urgência explícita, use 'Média'. Exemplos: 'pagar conta', 'consertar algo', 'comprar remédio'.
            - Use 'Baixa' para tarefas de baixa urgência ou rotineiras como: 'quando der', 'sem pressa', 'ver depois', 'escovar os dentes', 'comprar pão'.
            - Se for ambíguo, o padrão é 'Média'.

            --- EXEMPLOS ---
            - Usuário: 'terminar projeto to do' -> Status: 'Em Andamento', Prioridade: 'Média'
            - Usuário: 'enviar email para o fornecedor sobre a nota fiscal, é urgente' -> Status: 'Pendente', Prioridade: 'Alta'
            - Usuário: 'escovar os dentes' -> Status: 'Pendente', Prioridade: 'Baixa'
            - Usuário: 'Comprar remédio para as gatas' -> Status: 'Pendente', Prioridade: 'Média'
            - Usuário: 'revisar o documento de design quando tiver um tempo' -> Status: 'Em Andamento', Prioridade: 'Baixa'

            --- SOLICITAÇÃO DO USUÁRIO ---
            '{userPrompt}'";

        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(apiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini retornou erro {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var rawText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                _logger.LogWarning("Gemini retornou resposta vazia para o prompt.");
                return null;
            }

            var startIndex = rawText.IndexOf('{');
            var endIndex = rawText.LastIndexOf('}');

            if (startIndex == -1 || endIndex == -1)
            {
                _logger.LogWarning("Gemini não retornou JSON válido. Resposta: {Raw}", rawText);
                return null;
            }

            var jsonText = rawText.Substring(startIndex, endIndex - startIndex + 1);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<TaskCreateModel>(jsonText, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao chamar a API Gemini.");
            return null;
        }
    }
}

public class GeminiResponse { public List<GeminiCandidate>? Candidates { get; set; } }
public class GeminiCandidate { public GeminiContent? Content { get; set; } }
public class GeminiContent { public List<GeminiPart>? Parts { get; set; } }
public class GeminiPart { public string? Text { get; set; } }
