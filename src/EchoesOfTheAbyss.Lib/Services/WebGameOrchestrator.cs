using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using EchoesOfTheAbyss.Lib.Configuration;
using EchoesOfTheAbyss.Lib.Enums;
using EchoesOfTheAbyss.Lib.Models;
using OpenAI;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Services;

public class WebGameOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ChatClient _chatClient;
    private readonly Func<string, Task> _send;
    private readonly Channel<string> _playerInputChannel = Channel.CreateUnbounded<string>();
    private readonly List<ChatMessage> _chatHistory = [];
    private WorldContext _worldContext = new();
    private readonly ImaginationPipeline _imaginationPipeline;
    private readonly ILogger _logger;
    private string _sessionId = string.Empty;
    private int _round = 0;

    public WebGameOrchestrator(LlmConfig config, Func<string, Task> send, ILogger? logger = null)
    {
        _chatClient = new ChatClient(
            config.Model,
            new ApiKeyCredential("aoeu"),
            new OpenAIClientOptions { Endpoint = new Uri($"{config.Host}:{config.Port}/v1") });
        _send = send;
        _logger = logger ?? new SessionLogger();
        _imaginationPipeline = new ImaginationPipeline(_chatClient);

        // Initial setup - these stay in history as system context
        _chatHistory.Add(new SystemChatMessage(Prompts.Narrator));
        _chatHistory.Add(new UserChatMessage(Prompts.Exposition));
    }

    public async Task SetDifficulty(DifficultyLevel difficulty, CancellationToken ct = default)
    {
        _worldContext.Difficulty = difficulty;
        await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);
    }

    public async Task SetNarrationVerbosity(VerbosityLevel verbosity, CancellationToken ct = default)
    {
        _worldContext.NarrationVerbosity = verbosity;
        await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);
    }

    public void EnqueuePlayerInput(string text) =>
        _playerInputChannel.Writer.TryWrite(text);

    public async Task RunAsync(CancellationToken ct = default)
    {
        _sessionId = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        await _logger.InitializeAsync(_sessionId);

        var lastPlayerInput = string.Empty;

        while (!ct.IsCancellationRequested)
        {
            var messages = new List<ChatMessage>();
            messages.Add(new SystemChatMessage($"Current World Context:\n{_worldContext}"));
            messages.AddRange(_chatHistory);

            var narration = _chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct);
            var message = await StreamNarrationAsync(narration, ct);

            _chatHistory.Add(new AssistantChatMessage(message.Speech));

            await LogNarrationAsync(message.Thoughts, message.Speech, _sessionId, _round);

            var eval = await _imaginationPipeline.EvaluateAsync(lastPlayerInput, message.Speech, _worldContext);
            await SendJsonAsync(new { Type = "imagination_starting", Eval = eval }, ct);

            var (context, _) = await _imaginationPipeline.RunAsync(lastPlayerInput, message.Speech, _worldContext);
            _worldContext = context;

            await LogWorldStateAsync(_worldContext, _sessionId, _round);

            await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);

            if (_worldContext.Player.Stats.CurrentHealth <= 0)
            {
                var summary = await GenerateStorySummaryAsync(ct);
                await SendJsonAsync(new { Type = "game_over", Summary = summary }, ct);
                
                // Wait for restart
                var input = await _playerInputChannel.Reader.ReadAsync(ct);
                if (input.Trim().Equals("restart", StringComparison.OrdinalIgnoreCase))
                {
                    await RestartGameAsync(ct);
                    continue;
                }
                break; // Or handle other inputs
            }

            var playerInput = await _playerInputChannel.Reader.ReadAsync(ct);
            _chatHistory.Add(new UserChatMessage(playerInput));
            lastPlayerInput = playerInput;

            _round++;
        }
        
        await _logger.CloseSessionAsync();
    }

    private async Task<string> GenerateStorySummaryAsync(CancellationToken ct)
    {
        var summaryPrompt = "Provide a brief, abridged version of the player's story so far, highlighting their journey and their ultimate end. Keep it to 1-2 paragraphs.";
        var messages = new List<ChatMessage>();
        messages.AddRange(_chatHistory);
        messages.Add(new UserChatMessage(summaryPrompt));
        
        var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: ct);
        return response.Value.Content[0].Text;
    }

    private async Task RestartGameAsync(CancellationToken ct)
    {
        _chatHistory.Clear();
        _worldContext = new WorldContext();
        _round = 0;
        _chatHistory.Add(new SystemChatMessage(Prompts.Narrator));
        _chatHistory.Add(new UserChatMessage(Prompts.Exposition));
        
        _sessionId = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss") + "_restart";
        await _logger.InitializeAsync(_sessionId);

        await SendJsonAsync(new { Type = "restart_confirmed" }, ct);
        await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);
    }

    private async Task<Message> StreamNarrationAsync(
        AsyncCollectionResult<StreamingChatCompletionUpdate> narration,
        CancellationToken ct)
    {
        var currentThought = string.Empty;
        var thoughts = new List<string>();
        var output = string.Empty;
        var isThinking = false;

        await foreach (var update in narration.WithCancellation(ct))
        {
            if (update.ContentUpdate.Count == 0) continue;

            var text = update.ContentUpdate[0].Text;

            if (text.Contains("<think>")) { isThinking = true; continue; }
            if (text.Contains("</think>")) { isThinking = false; continue; }

            if (isThinking)
            {
                if (text.Contains('\n'))
                {
                    if (currentThought.Length > 0)
                    {
                        thoughts.Add(currentThought + text.Replace("\n", ""));
                        currentThought = string.Empty;
                    }
                }
                else
                {
                    currentThought += text;
                }
            }
            else
            {
                output += text.Replace("\n", "").Replace("\"", "");
            }
        }

        var id = Guid.NewGuid().ToString();
        await SendJsonAsync(new { Type = "narrator_complete", Id = id, Speech = output, Thoughts = thoughts.ToArray() }, ct);

        return new Message
        {
            Messager = MessagerEnum.Narrator,
            Thoughts = thoughts,
            Speech = output
        };
    }

    private Task SendJsonAsync(object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return _send(json);
    }

    private async Task LogNarrationAsync(List<string> thoughts, string speech, string sessionId, int round)
    {
        var entry = new NarrationLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            Thoughts = string.Join("\n", thoughts),
            Speech = speech
        };
        
        await _logger.LogNarrationAsync(entry);
    }

    private async Task LogWorldStateAsync(WorldContext context, string sessionId, int round)
    {
        var entry = WorldStateLogEntry.FromWorldContext(context, sessionId, round);
        await _logger.LogWorldStateAsync(entry);
    }

}
