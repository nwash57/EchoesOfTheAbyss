using OpenAI.Chat;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Shared;

namespace EchoesOfTheAbyss.Lib.Player;

public class PlayerStateUpdater : IPlayerStateUpdater
{
    private readonly IChatService _chatService;
    private readonly Func<double> _randomProvider;

    public PlayerStateUpdater(IChatService chatService, Func<double>? randomProvider = null)
    {
        _chatService = chatService;
        _randomProvider = randomProvider ?? (() => Random.Shared.NextDouble());
    }

    public async Task<Player> UpdateAsync(List<ChatMessage> history, WorldContext current, NarrativeEvaluation eval)
    {
        if (!eval.UpdatePlayer)
        {
            history.Add(new AssistantChatMessage(current.Player.ToJson()));
            return current.Player;
        }

        var targetHealth = Math.Clamp(current.Player.Stats.CurrentHealth + eval.HealthDelta, 0, current.Player.Stats.MaxHealth);
        var secondWindHappened = false;

        if (targetHealth == 0 && !current.Player.Stats.HasUsedSecondWind)
        {
            var chance = GetSecondWindChance(current.Difficulty);
            if (_randomProvider() < chance)
            {
                targetHealth = 15;
                secondWindHappened = true;
            }
        }

        var healthConstraint = eval.HealthDelta != 0
            ? $" The net health change this turn is exactly {eval.HealthDelta:+#;-#;0} HP; set currentHealth to exactly {targetHealth}."
            : $" No health change occurred this turn; carry current health forward unchanged (current: {current.Player.Stats.CurrentHealth}).";

        if (secondWindHappened)
        {
            healthConstraint += " CRITICAL: The player just triggered their 'Second Wind'. Set hasUsedSecondWind to true.";
        }

        history.Add(new UserChatMessage(
            $"Now extract the player's demographics and stats. Ensure they are consistent with the latest narration.{healthConstraint} At the start of the game, currentHealth should be between 70 and 100 based on the initial exposition."));

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "player_context",
                jsonSchema: BinaryData.FromString(Player.JsonSchema),
                jsonSchemaIsStrict: false)
        };

        var playerJson = await _chatService.CompleteChatAsync(history, options);
        var player = playerJson.FromJson<Player>();

        if (secondWindHappened)
        {
            player.Stats.HasUsedSecondWind = true;
            player.Stats.CurrentHealth = Math.Max(player.Stats.CurrentHealth, targetHealth);
        }

        history.Add(new AssistantChatMessage(playerJson));
        return player;
    }

    public static double GetSecondWindChance(DifficultyLevel difficulty) => difficulty switch
    {
        DifficultyLevel.ExtremelyEasy => 0.95,
        DifficultyLevel.Easy          => 0.75,
        DifficultyLevel.Balanced      => 0.50,
        DifficultyLevel.Hard          => 0.25,
        DifficultyLevel.ExtremelyHard => 0.05,
        _                             => 0.50
    };
}
