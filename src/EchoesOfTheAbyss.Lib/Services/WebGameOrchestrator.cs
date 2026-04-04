using System.ClientModel;
using System.Text.Json;
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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ChatClient _chatClient;
    private readonly Func<string, Task> _send;
    private readonly Channel<string> _playerInputChannel = Channel.CreateUnbounded<string>();
    private readonly List<ChatMessage> _chatHistory = [];
    private WorldContext _worldContext = new();
    private readonly ImaginationPipeline _imaginationPipeline;

    public WebGameOrchestrator(LlmConfig config, Func<string, Task> send)
    {
        _chatClient = new ChatClient(
            config.Model,
            new ApiKeyCredential("aoeu"),
            new OpenAIClientOptions { Endpoint = new Uri($"{config.Host}:{config.Port}/v1") });
        _send = send;
        _imaginationPipeline = new ImaginationPipeline(_chatClient);

        _chatHistory.Add(new SystemChatMessage(Prompts.Narrator));
        _chatHistory.Add(new UserChatMessage(Prompts.Exposition));
    }

    public void SetDifficulty(int difficulty)
    {
        _worldContext.Difficulty = Math.Clamp(difficulty, 0, 100);
    }

    public void EnqueuePlayerInput(string text) =>
        _playerInputChannel.Writer.TryWrite(text);

    public async Task RunAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var messages = new List<ChatMessage>(_chatHistory);
            messages.Insert(0, new SystemChatMessage($"Current World Context:\n{_worldContext}"));
            
            var narration = _chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct);
            var message = await StreamNarrationAsync(narration, ct);

            _chatHistory.Add(new AssistantChatMessage(message.Speech));

            _worldContext = await _imaginationPipeline.RunAsync(message.Speech, _worldContext);

            await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);

            var playerInput = await _playerInputChannel.Reader.ReadAsync(ct);
            _chatHistory.Add(new UserChatMessage(playerInput));
        }
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
}
