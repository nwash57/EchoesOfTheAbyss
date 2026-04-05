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

    public async Task<WorldContext> RunAsync(string playerInput, string narration, WorldContext current)
    {
        var eval = await EvaluateAsync(playerInput, narration, current);

        var history = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.ImaginationSystem),
            new UserChatMessage($"""
                Latest narration: "{narration}"
                Previous world state: {current}

                Extract the current location.
                """)
        };

        // Location
        Location location;
        if (eval.UpdateLocation)
        {
            var locationJson = await CompleteAsync(history, Location.JsonSchema, "location_context");
            location = locationJson.FromJson<Location>();
            history.Add(new AssistantChatMessage(locationJson));
        }
        else
        {
            location = current.CurrentLocation;
            history.Add(new AssistantChatMessage(current.CurrentLocation.ToJson()));
        }

        // Player
        Player player;
        if (eval.UpdatePlayer)
        {
            var healthConstraint = eval.HealthDelta != 0
                ? $" The net health change this turn is exactly {eval.HealthDelta:+#;-#;0} HP; set currentHealth to exactly {Math.Clamp(current.Player.Stats.CurrentHealth + eval.HealthDelta, 0, current.Player.Stats.MaxHealth)}."
                : " No health change occurred this turn; carry current health forward unchanged.";

            history.Add(new UserChatMessage(
                $"Now extract the player's demographics and stats. Ensure they are consistent with the latest narration.{healthConstraint} At the start of the game, currentHealth should be between 70 and 100 based on the initial exposition."));
            var playerJson = await CompleteAsync(history, Player.JsonSchema, "player_context");
            player = playerJson.FromJson<Player>();
            history.Add(new AssistantChatMessage(playerJson));
        }
        else
        {
            player = current.Player;
            history.Add(new AssistantChatMessage(current.Player.ToJson()));
        }

        // Equipment
        Equipment equipment;
        if (eval.UpdateEquipment)
        {
            history.Add(new UserChatMessage("""
                Analyze the narration and extract the player's current equipment.
                If the narration mentions grabbing, picking up, or using an item (e.g., a "rusted dagger"), ensure it's placed into a hand slot.
                Maintain existing equipment that is still in possession.
                """));
            var equipmentJson = await CompleteAsync(history, Equipment.JsonSchema, "equipment_context");
            equipment = equipmentJson.FromJson<Equipment>();
        }
        else
        {
            equipment = current.Equipment;
        }

        return new WorldContext
        {
            Difficulty = current.Difficulty,
            NarrationVerbosity = current.NarrationVerbosity,
            Player = player,
            CurrentLocation = location,
            Equipment = equipment
        };
    }

    private async Task<NarrativeEvaluation> EvaluateAsync(string playerInput, string narration, WorldContext current)
    {
        var history = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.NarrativeEvaluator),
            new UserChatMessage($"""
                Player action: "{playerInput}"
                Narrator response: "{narration}"
                Current player health: {current.Player.Stats.CurrentHealth}/{current.Player.Stats.MaxHealth}
                Current player armor: {current.Player.Stats.BaseArmor}
                """)
        };
        var json = await CompleteAsync(history, NarrativeEvaluation.JsonSchema, "narrative_evaluation");
        return json.FromJson<NarrativeEvaluation>();
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
