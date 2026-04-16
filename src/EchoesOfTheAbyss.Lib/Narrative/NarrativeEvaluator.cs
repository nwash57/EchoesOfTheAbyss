using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Shared;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Narrative;

public class NarrativeEvaluator : INarrativeEvaluator
{
    private readonly IChatService _chatService;

    public NarrativeEvaluator(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<NarrativeEvaluation> EvaluateAsync(string playerInput, string narration, WorldContext current, string? plotObjective = null)
    {
        var plotLine = plotObjective is not null
            ? $"\nCurrent plot objective: \"{plotObjective}\""
            : "";

        var history = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.NarrativeEvaluator),
            new UserChatMessage($"""
                Player action: "{playerInput}"
                Narrator response: "{narration}"
                Current player condition: {current.Player.Stats.Condition}
                Current player armor: {current.Player.Stats.BaseArmor}{plotLine}
                """)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "narrative_evaluation",
                jsonSchema: BinaryData.FromString(NarrativeEvaluation.JsonSchema),
                jsonSchemaIsStrict: false)
        };

        var json = await _chatService.CompleteChatAsync(history, options);
        return json.FromJson<NarrativeEvaluation>();
    }
}
