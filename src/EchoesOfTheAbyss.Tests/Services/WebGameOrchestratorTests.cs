using System.Text.Json;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Logging;
using EchoesOfTheAbyss.Lib.Game;
using EchoesOfTheAbyss.Lib.Pipeline;
using EchoesOfTheAbyss.Lib.PlotArc;
using EchoesOfTheAbyss.Lib.Rules.Input;
using EchoesOfTheAbyss.Lib.Shared;
using NSubstitute;

namespace EchoesOfTheAbyss.Tests.Services;

public class WebGameOrchestratorTests
{
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly INarrationPipelineRunner _pipelineRunner = Substitute.For<INarrationPipelineRunner>();
    private readonly IPlotArcGenerator _plotArcGenerator = Substitute.For<IPlotArcGenerator>();
    private readonly IInputRuleRunner _inputRuleRunner = Substitute.For<IInputRuleRunner>();
    private readonly IPlotArcTracker _tracker = Substitute.For<IPlotArcTracker>();
    private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IClientConnection _connection = Substitute.For<IClientConnection>();
    private readonly WebGameOrchestrator _orchestrator;

    private readonly List<string> _sentMessages = [];

    public WebGameOrchestratorTests()
    {
        _orchestrator = new WebGameOrchestrator(_chatService, _pipelineRunner, _plotArcGenerator, _tracker, _inputRuleRunner, _logger);
        _orchestrator.SetConnection(_connection);

        _connection.SendAsync(Arg.Do<string>(json => _sentMessages.Add(json)))
            .Returns(Task.CompletedTask);

        _logger.InitializeAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _logger.LogNarrationAsync(Arg.Any<NarrationLogEntry>()).Returns(Task.CompletedTask);
        _logger.LogWorldStateAsync(Arg.Any<WorldStateLogEntry>()).Returns(Task.CompletedTask);
        _logger.CloseSessionAsync().Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task SetDifficulty_SendsWorldUpdate_WithCorrectDifficulty()
    {
        await _orchestrator.SetDifficulty(DifficultyLevel.Hard);

        Assert.Single(_sentMessages);
        var msg = JsonSerializer.Deserialize<JsonElement>(_sentMessages[0]);
        Assert.Equal("world_update", msg.GetProperty("type").GetString());
        Assert.Equal("Hard", msg.GetProperty("worldContext").GetProperty("difficulty").GetString());
    }

    [Fact]
    public async Task SetNarrationVerbosity_SendsWorldUpdate_WithCorrectVerbosity()
    {
        await _orchestrator.SetNarrationVerbosity(VerbosityLevel.Verbose);

        Assert.Single(_sentMessages);
        var msg = JsonSerializer.Deserialize<JsonElement>(_sentMessages[0]);
        Assert.Equal("world_update", msg.GetProperty("type").GetString());
        Assert.Equal("Verbose", msg.GetProperty("worldContext").GetProperty("narrationVerbosity").GetString());
    }

    [Fact]
    public async Task SetDifficulty_MultipleCalls_EachSendsUpdate()
    {
        await _orchestrator.SetDifficulty(DifficultyLevel.Easy);
        await _orchestrator.SetDifficulty(DifficultyLevel.ExtremelyHard);

        Assert.Equal(2, _sentMessages.Count);

        var first = JsonSerializer.Deserialize<JsonElement>(_sentMessages[0]);
        Assert.Equal("Easy", first.GetProperty("worldContext").GetProperty("difficulty").GetString());

        var second = JsonSerializer.Deserialize<JsonElement>(_sentMessages[1]);
        Assert.Equal("ExtremelyHard", second.GetProperty("worldContext").GetProperty("difficulty").GetString());
    }

    [Fact]
    public async Task SetDifficulty_AndVerbosity_BothReflectedInWorldContext()
    {
        await _orchestrator.SetDifficulty(DifficultyLevel.Hard);
        await _orchestrator.SetNarrationVerbosity(VerbosityLevel.ExtremelyConcise);

        // The second message should contain both the updated difficulty AND verbosity
        var msg = JsonSerializer.Deserialize<JsonElement>(_sentMessages[1]);
        Assert.Equal("Hard", msg.GetProperty("worldContext").GetProperty("difficulty").GetString());
        Assert.Equal("ExtremelyConcise", msg.GetProperty("worldContext").GetProperty("narrationVerbosity").GetString());
    }
}
