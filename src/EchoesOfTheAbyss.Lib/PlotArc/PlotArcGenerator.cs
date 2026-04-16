using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Shared;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.PlotArc;

public class PlotArcGenerator : IPlotArcGenerator
{
    private readonly IChatService _chatService;

    public PlotArcGenerator(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<PlotArc> GenerateAsync(CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.PlotArcGenerator),
            new UserChatMessage("Generate a hero's journey plot arc for a new adventure.")
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "plot_arc",
                jsonSchema: BinaryData.FromString(PlotArc.JsonSchema),
                jsonSchemaIsStrict: false)
        };

        var json = await _chatService.CompleteChatAsync(messages, options, ct);
        return json.FromJson<PlotArc>();
    }

    public async Task<PlotArc> RegenerateAsync(PlotArc current, List<string> adventureLog, CancellationToken ct = default)
    {
        var logText = adventureLog.Count > 0
            ? string.Join("\n", adventureLog.TakeLast(8).Select((e, i) => $"  {i + 1}. {e}"))
            : "(no events yet)";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.PlotArcRegenerator),
            new UserChatMessage($"""
                The player has deviated from the planned plot. Regenerate the remaining plot points.

                ORIGINAL ARC:
                NPCs: {current.EstablishedNpcs}
                Locations: {current.EstablishedLocations}
                Items: {current.EstablishedItems}
                Backstory: {current.Backstory}
                Original climax: {current.Climax}

                WHAT HAS ACTUALLY HAPPENED:
                {logText}
                """)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "plot_arc",
                jsonSchema: BinaryData.FromString(PlotArc.JsonSchema),
                jsonSchemaIsStrict: false)
        };

        var json = await _chatService.CompleteChatAsync(messages, options, ct);
        var regenerated = json.FromJson<PlotArc>();

        // Safety net: preserve established facts that the narrator has already referenced
        regenerated.EstablishedNpcs = current.EstablishedNpcs;
        regenerated.EstablishedLocations = current.EstablishedLocations;
        regenerated.EstablishedItems = current.EstablishedItems;
        regenerated.Backstory = current.Backstory;
        regenerated.CurrentPlotPointIndex = 0;

        return regenerated;
    }
}
