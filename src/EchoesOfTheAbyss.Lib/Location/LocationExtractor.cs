using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Shared;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Location;

public class LocationExtractor : ILocationExtractor
{
    private readonly IChatService _chatService;

    public LocationExtractor(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<Location> ExtractAsync(List<ChatMessage> history, WorldContext current, NarrativeEvaluation eval)
    {
        if (!eval.UpdateLocation)
        {
            history.Add(new AssistantChatMessage(current.CurrentLocation.ToJson()));
            return current.CurrentLocation;
        }

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "location_context",
                jsonSchema: BinaryData.FromString(Location.JsonSchema),
                jsonSchemaIsStrict: false)
        };

        var locationJson = await _chatService.CompleteChatAsync(history, options);
        history.Add(new AssistantChatMessage(locationJson));
        return locationJson.FromJson<Location>();
    }
}
