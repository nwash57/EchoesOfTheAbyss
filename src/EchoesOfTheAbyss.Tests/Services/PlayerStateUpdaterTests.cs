using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Player;
using EchoesOfTheAbyss.Lib.Shared;
using NSubstitute;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Tests.Services;

public class PlayerStateUpdaterTests
{
    private readonly IChatService _chatService = Substitute.For<IChatService>();

    private WorldContext CreateContext(int health = 80, bool hasUsedSecondWind = false, DifficultyLevel difficulty = DifficultyLevel.Balanced)
    {
        return new WorldContext
        {
            Difficulty = difficulty,
            Player = new Player
            {
                Demographics = new ActorDemographics
                {
                    FirstName = "Aldric",
                    LastName = "Thornwood",
                    Age = 28,
                    Occupation = "Sellsword"
                },
                Stats = new PlayerStats
                {
                    CurrentHealth = health,
                    MaxHealth = 100,
                    BaseArmor = 10,
                    BaseStrength = 15,
                    HasUsedSecondWind = hasUsedSecondWind
                }
            }
        };
    }

    private Player CreatePlayerResponse(int health = 80, bool hasUsedSecondWind = false)
    {
        return new Player
        {
            Demographics = new ActorDemographics
            {
                FirstName = "Aldric",
                LastName = "Thornwood",
                Age = 28,
                Occupation = "Sellsword"
            },
            Stats = new PlayerStats
            {
                CurrentHealth = health,
                MaxHealth = 100,
                BaseArmor = 10,
                BaseStrength = 15,
                HasUsedSecondWind = hasUsedSecondWind
            }
        };
    }

    [Fact]
    public async Task UpdateAsync_ReturnsCurrentPlayer_WhenNoUpdate()
    {
        var updater = new PlayerStateUpdater(_chatService);
        var context = CreateContext();
        var eval = new NarrativeEvaluation { UpdatePlayer = false, HealthDelta = 0 };
        var history = new List<ChatMessage>();

        var result = await updater.UpdateAsync(history, context, eval);

        Assert.Equal(context.Player.Stats.CurrentHealth, result.Stats.CurrentHealth);
        Assert.Equal(context.Player.Demographics.FirstName, result.Demographics.FirstName);
        await _chatService.DidNotReceive().CompleteChatAsync(
            Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_AppliesHealthDelta_WhenUpdating()
    {
        var playerResponse = CreatePlayerResponse(health: 70);
        _chatService.CompleteChatAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(playerResponse.ToJson());

        var updater = new PlayerStateUpdater(_chatService);
        var context = CreateContext(health: 80);
        var eval = new NarrativeEvaluation { UpdatePlayer = true, HealthDelta = -10 };
        var history = new List<ChatMessage>();

        var result = await updater.UpdateAsync(history, context, eval);

        Assert.Equal(70, result.Stats.CurrentHealth);
    }

    [Fact]
    public async Task UpdateAsync_TriggersSecondWind_WhenHealthReachesZeroAndNotUsed()
    {
        // Random returns 0.1 which is < 0.50 (Balanced chance), so Second Wind triggers
        var playerResponse = CreatePlayerResponse(health: 15, hasUsedSecondWind: false);
        _chatService.CompleteChatAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(playerResponse.ToJson());

        var updater = new PlayerStateUpdater(_chatService, randomProvider: () => 0.1);
        var context = CreateContext(health: 10, hasUsedSecondWind: false);
        var eval = new NarrativeEvaluation { UpdatePlayer = true, HealthDelta = -10 };
        var history = new List<ChatMessage>();

        var result = await updater.UpdateAsync(history, context, eval);

        Assert.True(result.Stats.HasUsedSecondWind);
        Assert.True(result.Stats.CurrentHealth >= 15);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotTriggerSecondWind_WhenAlreadyUsed()
    {
        var playerResponse = CreatePlayerResponse(health: 0, hasUsedSecondWind: true);
        _chatService.CompleteChatAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(playerResponse.ToJson());

        var updater = new PlayerStateUpdater(_chatService, randomProvider: () => 0.1);
        var context = CreateContext(health: 10, hasUsedSecondWind: true);
        var eval = new NarrativeEvaluation { UpdatePlayer = true, HealthDelta = -10 };
        var history = new List<ChatMessage>();

        var result = await updater.UpdateAsync(history, context, eval);

        Assert.Equal(0, result.Stats.CurrentHealth);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotTriggerSecondWind_WhenRandomExceedsChance()
    {
        // Random returns 0.99 which is > 0.50 (Balanced chance), so Second Wind does NOT trigger
        var playerResponse = CreatePlayerResponse(health: 0, hasUsedSecondWind: false);
        _chatService.CompleteChatAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(playerResponse.ToJson());

        var updater = new PlayerStateUpdater(_chatService, randomProvider: () => 0.99);
        var context = CreateContext(health: 10, hasUsedSecondWind: false);
        var eval = new NarrativeEvaluation { UpdatePlayer = true, HealthDelta = -10 };
        var history = new List<ChatMessage>();

        var result = await updater.UpdateAsync(history, context, eval);

        Assert.Equal(0, result.Stats.CurrentHealth);
        Assert.False(result.Stats.HasUsedSecondWind);
    }

    [Theory]
    [InlineData(DifficultyLevel.ExtremelyEasy, 0.95)]
    [InlineData(DifficultyLevel.Easy, 0.75)]
    [InlineData(DifficultyLevel.Balanced, 0.50)]
    [InlineData(DifficultyLevel.Hard, 0.25)]
    [InlineData(DifficultyLevel.ExtremelyHard, 0.05)]
    public void GetSecondWindChance_ReturnsCorrectChance(DifficultyLevel difficulty, double expectedChance)
    {
        var chance = PlayerStateUpdater.GetSecondWindChance(difficulty);
        Assert.Equal(expectedChance, chance);
    }

    [Fact]
    public async Task UpdateAsync_ClampsHealthToMaxHealth()
    {
        // LLM returns health of 120 but max is 100 — the target health calculation should clamp
        var playerResponse = CreatePlayerResponse(health: 100);
        _chatService.CompleteChatAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
            .Returns(playerResponse.ToJson());

        var updater = new PlayerStateUpdater(_chatService);
        var context = CreateContext(health: 90);
        var eval = new NarrativeEvaluation { UpdatePlayer = true, HealthDelta = 20 };
        var history = new List<ChatMessage>();

        var result = await updater.UpdateAsync(history, context, eval);

        Assert.True(result.Stats.CurrentHealth <= 100);
    }
}
