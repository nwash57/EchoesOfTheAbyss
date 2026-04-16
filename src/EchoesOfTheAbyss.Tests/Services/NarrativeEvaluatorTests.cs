using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Player;
using EchoesOfTheAbyss.Lib.Shared;
using NSubstitute;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Tests.Services;

public class NarrativeEvaluatorTests
{
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly NarrativeEvaluator _evaluator;

    public NarrativeEvaluatorTests()
    {
        _evaluator = new NarrativeEvaluator(_chatService);
    }

    [Fact]
    public async Task EvaluateAsync_DeserializesResponse_Correctly()
    {
        var expectedEval = new NarrativeEvaluation
        {
            UpdateLocation = true,
            UpdatePlayer = true,
            UpdateEquipment = false,
            HealthDelta = -15,
            LogEntry = "Slew a goblin in combat"
        };

        _chatService.CompleteChatAsync(
            Arg.Any<IList<ChatMessage>>(),
            Arg.Any<ChatCompletionOptions>(),
            Arg.Any<CancellationToken>()
        ).Returns(expectedEval.ToJson());

        var context = new WorldContext
        {
            Player = new Player
            {
                Stats = new PlayerStats { CurrentHealth = 80, MaxHealth = 100, BaseArmor = 10 }
            }
        };

        var result = await _evaluator.EvaluateAsync("attack goblin", "You strike the goblin.", context);

        Assert.True(result.UpdateLocation);
        Assert.True(result.UpdatePlayer);
        Assert.False(result.UpdateEquipment);
        Assert.Equal(-15, result.HealthDelta);
        Assert.Equal("Slew a goblin in combat", result.LogEntry);
    }

    [Fact]
    public async Task EvaluateAsync_HandlesNoChanges()
    {
        var expectedEval = new NarrativeEvaluation
        {
            UpdateLocation = false,
            UpdatePlayer = false,
            UpdateEquipment = false,
            HealthDelta = 0,
            LogEntry = ""
        };

        _chatService.CompleteChatAsync(
            Arg.Any<IList<ChatMessage>>(),
            Arg.Any<ChatCompletionOptions>(),
            Arg.Any<CancellationToken>()
        ).Returns(expectedEval.ToJson());

        var context = new WorldContext();
        var result = await _evaluator.EvaluateAsync("look around", "You see nothing new.", context);

        Assert.False(result.UpdateLocation);
        Assert.False(result.UpdatePlayer);
        Assert.False(result.UpdateEquipment);
        Assert.Equal(0, result.HealthDelta);
        Assert.Empty(result.LogEntry);
    }

    [Fact]
    public async Task EvaluateAsync_PassesCorrectPromptContext()
    {
        _chatService.CompleteChatAsync(
            Arg.Any<IList<ChatMessage>>(),
            Arg.Any<ChatCompletionOptions>(),
            Arg.Any<CancellationToken>()
        ).Returns(new NarrativeEvaluation().ToJson());

        var context = new WorldContext
        {
            Player = new Player
            {
                Stats = new PlayerStats { CurrentHealth = 50, MaxHealth = 100, BaseArmor = 25 }
            }
        };

        await _evaluator.EvaluateAsync("swing sword", "The sword connects.", context);

        await _chatService.Received(1).CompleteChatAsync(
            Arg.Is<IList<ChatMessage>>(msgs =>
                msgs.Count == 2 &&
                msgs[0] is SystemChatMessage &&
                msgs[1] is UserChatMessage),
            Arg.Any<ChatCompletionOptions>(),
            Arg.Any<CancellationToken>());
    }
}
