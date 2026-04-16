using System.ClientModel;
using System.Runtime.CompilerServices;
using OpenAI;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Llm;

public class OpenAiChatService : IChatService
{
    private readonly ChatClient _chatClient;

    public OpenAiChatService(LlmConfig config)
    {
        _chatClient = new ChatClient(
            config.Model,
            new ApiKeyCredential("aoeu"),
            new OpenAIClientOptions { Endpoint = new Uri($"{config.Host}:{config.Port}/v1") });
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> StreamChatAsync(
        IList<ChatMessage> messages, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var result = _chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct);
        await foreach (var update in result.WithCancellation(ct))
        {
            yield return update;
        }
    }

    public async Task<string> CompleteChatAsync(
        IList<ChatMessage> messages, ChatCompletionOptions? options = null, CancellationToken ct = default)
    {
        var response = await _chatClient.CompleteChatAsync(messages, options, ct);
        return response.Value.Content[0].Text;
    }
}
