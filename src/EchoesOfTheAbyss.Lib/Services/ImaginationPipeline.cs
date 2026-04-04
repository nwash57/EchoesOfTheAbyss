using EchoesOfTheAbyss.Lib.Configuration;
using EchoesOfTheAbyss.Lib.Extensions;
using EchoesOfTheAbyss.Lib.Models;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Services;

public class ImaginationPipeline
{
    private readonly ChatClient _chatClient;

    public ImaginationPipeline(ChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<WorldContext> RunAsync(string narration, WorldContext current)
    {
        var history = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.ImaginationSystem),
            new UserChatMessage($"""
                Latest narration: "{narration}"
                Previous world state: {current}

                Extract the current location.
                """)
        };

        var locationJson = await CompleteAsync(history, Location.JsonSchema, "location_context");
        var location = locationJson.FromJson<Location>();
        history.Add(new AssistantChatMessage(locationJson));

        history.Add(new UserChatMessage("Now extract the player's demographics and stats. Ensure they are consistent with the latest narration. If the narration implies health loss or gain, reflect it in the currentHealth (max 100, min 0). At the start of the game, currentHealth should be between 70 and 100 based on the initial exposition."));
        var playerJson = await CompleteAsync(history, Player.JsonSchema, "player_context");
        var player = playerJson.FromJson<Player>();
        history.Add(new AssistantChatMessage(playerJson));

        history.Add(new UserChatMessage("""
            Analyze the narration and extract the player's current equipment.
            If the narration mentions grabbing, picking up, or using an item (e.g., a "rusted dagger"), ensure it's placed into a hand slot.
            Maintain existing equipment that is still in possession.
            """));
        var equipmentJson = await CompleteAsync(history, Equipment.JsonSchema, "equipment_context");
        var equipment = equipmentJson.FromJson<Equipment>();

        return new WorldContext
        {
            Player = player,
            CurrentLocation = location,
            Equipment = equipment
        };
    }

    private async Task<string> CompleteAsync(List<ChatMessage> history, string jsonSchema, string schemaName)
    {
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: schemaName,
                jsonSchema: BinaryData.FromString(jsonSchema),
                jsonSchemaIsStrict: false)
        };
        var completion = await _chatClient.CompleteChatAsync(history, options);
        return completion.Value.Content[0].Text;
    }
}
