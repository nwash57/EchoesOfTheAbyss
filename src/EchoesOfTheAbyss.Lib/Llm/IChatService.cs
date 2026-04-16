using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Llm;

public interface IChatService
{
    IAsyncEnumerable<StreamingChatCompletionUpdate> StreamChatAsync(
        IList<ChatMessage> messages, CancellationToken ct = default);

    Task<string> CompleteChatAsync(
        IList<ChatMessage> messages, ChatCompletionOptions? options = null, CancellationToken ct = default);
}
