using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Location;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Player;
using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Shared;
using NSubstitute;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Tests.Services;

public class ImaginationPipelineTests
{
    private readonly INarrativeEvaluator _evaluator = Substitute.For<INarrativeEvaluator>();
    private readonly ILocationExtractor _locationExtractor = Substitute.For<ILocationExtractor>();
    private readonly IPlayerStateUpdater _playerStateUpdater = Substitute.For<IPlayerStateUpdater>();
    private readonly IEquipmentExtractor _equipmentExtractor = Substitute.For<IEquipmentExtractor>();
    private readonly ImaginationPipeline _pipeline;

    public ImaginationPipelineTests()
    {
        _pipeline = new ImaginationPipeline(_evaluator, _locationExtractor, _playerStateUpdater, _equipmentExtractor);
    }

    private WorldContext CreateDefaultContext()
    {
        return new WorldContext
        {
            Difficulty = DifficultyLevel.Balanced,
            NarrationVerbosity = VerbosityLevel.Balanced,
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
                    CurrentHealth = 80,
                    MaxHealth = 100,
                    BaseArmor = 10,
                    BaseStrength = 15,
                    HasUsedSecondWind = false
                }
            },
            CurrentLocation = new Location
            {
                Coordinates = new Coordinates(0, 0),
                Title = "Village Square",
                ShortDescription = "A quiet village",
                LongDescription = "A peaceful square with a fountain.",
                Type = "default"
            },
            Equipment = new Equipment(),
            AdventureLog = ["Arrived at the village"]
        };
    }

    [Fact]
    public async Task EvaluateAsync_DelegatesToEvaluator()
    {
        var expectedEval = new NarrativeEvaluation
        {
            UpdateLocation = true,
            UpdatePlayer = false,
            UpdateEquipment = false,
            HealthDelta = -10,
            LogEntry = "Took a hit"
        };

        var context = CreateDefaultContext();
        _evaluator.EvaluateAsync("attack", "The goblin strikes!", context).Returns(expectedEval);

        var result = await _pipeline.EvaluateAsync("attack", "The goblin strikes!", context);

        Assert.Equal(expectedEval, result);
    }

    [Fact]
    public async Task RunAsync_CallsAllExtractors()
    {
        var context = CreateDefaultContext();
        var eval = new NarrativeEvaluation
        {
            UpdateLocation = true,
            UpdatePlayer = true,
            UpdateEquipment = true,
            HealthDelta = -5,
            LogEntry = "Fought a goblin"
        };

        _evaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorldContext>()).Returns(eval);
        _locationExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.CurrentLocation);
        _playerStateUpdater.UpdateAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Player);
        _equipmentExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Equipment);

        var (result, returnedEval) = await _pipeline.RunAsync("attack", "You fight.", context);

        await _evaluator.Received(1).EvaluateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorldContext>());
        await _locationExtractor.Received(1).ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>());
        await _playerStateUpdater.Received(1).UpdateAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>());
        await _equipmentExtractor.Received(1).ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>());
    }

    [Fact]
    public async Task RunAsync_AddsLogEntry_WhenPresent()
    {
        var context = CreateDefaultContext();
        var eval = new NarrativeEvaluation
        {
            UpdateLocation = false,
            UpdatePlayer = false,
            UpdateEquipment = false,
            HealthDelta = 0,
            LogEntry = "Rested by the campfire"
        };

        _evaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorldContext>()).Returns(eval);
        _locationExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.CurrentLocation);
        _playerStateUpdater.UpdateAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Player);
        _equipmentExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Equipment);

        var originalLogCount = context.AdventureLog.Count;
        var (result, _) = await _pipeline.RunAsync("rest", "You rest.", context);

        Assert.Equal(originalLogCount + 1, result.AdventureLog.Count);
        Assert.Equal("Rested by the campfire", result.AdventureLog.Last());
    }

    [Fact]
    public async Task RunAsync_DoesNotAddLogEntry_WhenEmpty()
    {
        var context = CreateDefaultContext();
        var eval = new NarrativeEvaluation
        {
            UpdateLocation = false,
            UpdatePlayer = false,
            UpdateEquipment = false,
            HealthDelta = 0,
            LogEntry = ""
        };

        _evaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorldContext>()).Returns(eval);
        _locationExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.CurrentLocation);
        _playerStateUpdater.UpdateAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Player);
        _equipmentExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Equipment);

        var originalLogCount = context.AdventureLog.Count;
        var (result, _) = await _pipeline.RunAsync("look", "Nothing.", context);

        Assert.Equal(originalLogCount, result.AdventureLog.Count);
    }

    [Fact]
    public async Task RunAsync_PreservesDifficultyAndVerbosity()
    {
        var context = CreateDefaultContext();
        context.Difficulty = DifficultyLevel.Hard;
        context.NarrationVerbosity = VerbosityLevel.Verbose;

        var eval = new NarrativeEvaluation { LogEntry = "" };
        _evaluator.EvaluateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WorldContext>()).Returns(eval);
        _locationExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.CurrentLocation);
        _playerStateUpdater.UpdateAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Player);
        _equipmentExtractor.ExtractAsync(Arg.Any<List<ChatMessage>>(), Arg.Any<WorldContext>(), Arg.Any<NarrativeEvaluation>()).Returns(context.Equipment);

        var (result, _) = await _pipeline.RunAsync("look", "Nothing.", context);

        Assert.Equal(DifficultyLevel.Hard, result.Difficulty);
        Assert.Equal(VerbosityLevel.Verbose, result.NarrationVerbosity);
    }
}
