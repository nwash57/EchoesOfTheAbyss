using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Logging;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Pipeline;
using EchoesOfTheAbyss.Lib.PlotArc;
using EchoesOfTheAbyss.Lib.Rules;
using EchoesOfTheAbyss.Lib.Rules.Input;
using EchoesOfTheAbyss.Lib.Shared;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Game;

public class WebGameOrchestrator : IGameOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IChatService _chatService;
    private readonly INarrationPipelineRunner _pipelineRunner;
    private readonly IPlotArcGenerator _plotArcGenerator;
    private readonly IPlotArcTracker _tracker;
    private readonly IInputRuleRunner _inputRuleRunner;
    private readonly ILogger _logger;
    private IClientConnection _connection = null!;
    private readonly Channel<string> _playerInputChannel = Channel.CreateUnbounded<string>();
    private TaskCompletionSource _setupTcs = new();
    private readonly List<ChatMessage> _chatHistory = [];
    private WorldContext _worldContext = new();
    private PlotArc.PlotArc? _plotArc;
    private string _sessionId = string.Empty;
    private int _round = 0;

    public WebGameOrchestrator(
        IChatService chatService,
        INarrationPipelineRunner pipelineRunner,
        IPlotArcGenerator plotArcGenerator,
        IPlotArcTracker tracker,
        IInputRuleRunner inputRuleRunner,
        ILogger logger)
    {
        _chatService = chatService;
        _pipelineRunner = pipelineRunner;
        _plotArcGenerator = plotArcGenerator;
        _tracker = tracker;
        _inputRuleRunner = inputRuleRunner;
        _logger = logger;
    }

    public void SetConnection(IClientConnection connection)
    {
        _connection = connection;
    }

    public async Task SetDifficulty(DifficultyLevel difficulty, CancellationToken ct = default)
    {
        _worldContext.Difficulty = difficulty;
        await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);
    }

    public async Task SetNarrationVerbosity(VerbosityLevel verbosity, CancellationToken ct = default)
    {
        _worldContext.NarrationVerbosity = verbosity;
        await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);
    }

    public void ConfirmSetup(DifficultyLevel? difficulty = null, VerbosityLevel? verbosity = null)
    {
        if (difficulty.HasValue) _worldContext.Difficulty = difficulty.Value;
        if (verbosity.HasValue) _worldContext.NarrationVerbosity = verbosity.Value;
        _setupTcs.TrySetResult();
    }

    public void EnqueuePlayerInput(string text) =>
        _playerInputChannel.Writer.TryWrite(text);

    public async Task RunAsync(CancellationToken ct = default)
    {
        _sessionId = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        await _logger.InitializeAsync(_sessionId);

        // Notify client that we are waiting for initial settings
        await SendJsonAsync(new { Type = "request_setup" }, ct);

        // Wait for difficulty and verbosity to be set
        using (ct.Register(() => _setupTcs.TrySetCanceled()))
        {
            await _setupTcs.Task;
        }

        // Generate plot arc before first narration
        _plotArc = await _plotArcGenerator.GenerateAsync(ct);
        await LogPlotArcAsync(_plotArc, false, _sessionId, _round);
        await SendDebugStateAsync(null, PlotArc.PlotArcAction.None, ct);

        // Initial setup - these stay in history as system context
        _chatHistory.Add(new SystemChatMessage(Prompts.Narrator));
        _chatHistory.Add(new UserChatMessage(Prompts.ExpositionWithArc(_plotArc, _worldContext.NarrationVerbosity)));

        var lastPlayerInput = string.Empty;

        while (!ct.IsCancellationRequested)
        {
            var messages = new List<ChatMessage>();
            messages.Add(new SystemChatMessage($"Current World Context:\n{_worldContext}"));
            if (_plotArc is not null)
                messages.Add(new SystemChatMessage($"Story Arc (hidden from player):\n{_plotArc.ToNarratorPrompt()}"));
            messages.AddRange(_chatHistory);

            var messageType = _round == 0 ? "exposition_complete" : "narrator_complete";
            var narration = _chatService.StreamChatAsync(messages, ct);
            var message = await StreamNarrationAsync(narration, ct, messageType);

            _chatHistory.Add(new AssistantChatMessage(message.Speech));

            await LogNarrationAsync(message.Thoughts, message.Speech, _sessionId, _round);

            // Run the post-narration pipeline (eval → drift → extract → assemble → validate)
            var pipelineContext = new NarrationPipelineContext
            {
                PlayerInput = lastPlayerInput,
                Narration = message.Speech,
                CurrentWorldContext = _worldContext,
                IsFirstTurn = _round == 0,
                Round = _round,
                PlotArc = _plotArc
            };

            await SendJsonAsync(new { Type = "imagination_starting" }, ct);
            var result = await _pipelineRunner.RunAsync(pipelineContext, ct);

            _worldContext = result.NewWorldContext!;
            _plotArc = result.PlotArc;

            await LogNarrativeEvalAsync(result.Evaluation!, _sessionId, _round);
            await SendDebugStateAsync(result.Evaluation, result.PlotAction, ct);

            if (_plotArc is not null && result.PlotAction != PlotArc.PlotArcAction.None)
            {
                await LogPlotArcAsync(_plotArc, isRegeneration: result.PlotAction == PlotArc.PlotArcAction.Regenerate, _sessionId, _round);
            }

            if (result.Violations.Count > 0)
            {
                await SendJsonAsync(new { Type = "rule_violations", Violations = result.Violations }, ct);
                await LogStateRuleResultAsync(result.Violations, _sessionId, _round);
            }

            await LogWorldStateAsync(_worldContext, _sessionId, _round);

            await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);

            if (_worldContext.Player.Stats.CurrentHealth <= 0)
            {
                var summary = await GenerateStorySummaryAsync(ct);
                await SendJsonAsync(new { Type = "game_over", Summary = summary }, ct);

                // Wait for restart
                var input = await _playerInputChannel.Reader.ReadAsync(ct);
                if (input.Trim().Equals("restart", StringComparison.OrdinalIgnoreCase))
                {
                    await RestartGameAsync(ct);
                    continue;
                }
                break; // Or handle other inputs
            }

            var playerInput = await _playerInputChannel.Reader.ReadAsync(ct);
            await LogPlayerInputAsync(playerInput, false, _sessionId, _round);

            // Checkpoint A: Input validation
            var inputResult = _inputRuleRunner.Evaluate(playerInput, _worldContext, message.Speech);
            await LogInputRuleResultAsync(playerInput, inputResult, _sessionId, _round);
            if (inputResult.IsRejected)
            {
                await SendJsonAsync(new { Type = "input_rejected", Message = inputResult.RejectionMessage }, ct);
                continue;
            }
            playerInput = inputResult.SanitizedInput;

            _chatHistory.Add(new UserChatMessage(playerInput));
            lastPlayerInput = playerInput;

            _round++;
        }

        await _logger.CloseSessionAsync();
    }

    private async Task<string> GenerateStorySummaryAsync(CancellationToken ct)
    {
        var summaryPrompt = "Provide a brief, abridged version of the player's story so far, highlighting their journey and their ultimate end. Keep it to 1-2 paragraphs.";
        var messages = new List<ChatMessage>();
        messages.AddRange(_chatHistory);
        messages.Add(new UserChatMessage(summaryPrompt));

        return await _chatService.CompleteChatAsync(messages, ct: ct);
    }

    private async Task RestartGameAsync(CancellationToken ct)
    {
        _chatHistory.Clear();
        _worldContext = new WorldContext();
        _round = 0;

        _sessionId = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss") + "_restart";
        await _logger.InitializeAsync(_sessionId);

        await SendJsonAsync(new { Type = "restart_confirmed" }, ct);
        await SendJsonAsync(new { Type = "request_setup" }, ct);

        // Wait for setup again on restart
        _setupTcs = new TaskCompletionSource();
        using (ct.Register(() => _setupTcs.TrySetCanceled()))
        {
            await _setupTcs.Task;
        }

        _plotArc = await _plotArcGenerator.GenerateAsync(ct);
        await LogPlotArcAsync(_plotArc, false, _sessionId, _round);
        await SendDebugStateAsync(null, PlotArc.PlotArcAction.None, ct);

        _chatHistory.Add(new SystemChatMessage(Prompts.Narrator));
        _chatHistory.Add(new UserChatMessage(Prompts.ExpositionWithArc(_plotArc, _worldContext.NarrationVerbosity)));

        await SendJsonAsync(new { Type = "world_update", WorldContext = _worldContext }, ct);
    }

    private async Task<Message> StreamNarrationAsync(
        IAsyncEnumerable<StreamingChatCompletionUpdate> narration,
        CancellationToken ct,
        string messageType = "narrator_complete")
    {
        var currentThought = string.Empty;
        var thoughts = new List<string>();
        var output = string.Empty;
        var isThinking = false;

        await foreach (var update in narration.WithCancellation(ct))
        {
            if (update.ContentUpdate.Count == 0) continue;

            var text = update.ContentUpdate[0].Text;

            if (text.Contains("<think>")) { isThinking = true; continue; }
            if (text.Contains("</think>")) { isThinking = false; continue; }

            if (isThinking)
            {
                if (text.Contains('\n'))
                {
                    if (currentThought.Length > 0)
                    {
                        thoughts.Add(currentThought + text.Replace("\n", ""));
                        currentThought = string.Empty;
                    }
                }
                else
                {
                    currentThought += text;
                }
            }
            else
            {
                output += text.Replace("\n", "").Replace("\"", "");
            }
        }

        var id = Guid.NewGuid().ToString();
        if (!string.IsNullOrWhiteSpace(output))
            await SendJsonAsync(new { Type = messageType, Id = id, Speech = output, Thoughts = thoughts.ToArray() }, ct);

        return new Message
        {
            Messager = MessagerEnum.Narrator,
            Thoughts = thoughts,
            Speech = output
        };
    }

    private Task SendDebugStateAsync(NarrativeEvaluation? evaluation, PlotArc.PlotArcAction plotAction, CancellationToken ct)
    {
        return SendJsonAsync(new
        {
            Type = "debug_state",
            Round = _round,
            PlotArc = _plotArc,
            PlotAction = plotAction.ToString(),
            TrackerState = _tracker.GetState(),
            Evaluation = evaluation
        }, ct);
    }

    private Task SendJsonAsync(object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return _connection.SendAsync(json);
    }

    private async Task LogNarrationAsync(List<string> thoughts, string speech, string sessionId, int round)
    {
        var entry = new NarrationLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            Thoughts = string.Join("\n", thoughts),
            Speech = speech
        };

        await _logger.LogNarrationAsync(entry);
    }

    private async Task LogWorldStateAsync(WorldContext context, string sessionId, int round)
    {
        var entry = WorldStateLogEntry.FromWorldContext(context, sessionId, round);
        await _logger.LogWorldStateAsync(entry);
    }

    private async Task LogPlayerInputAsync(string input, bool isExposition, string sessionId, int round)
    {
        var entry = new PlayerInputLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            Input = input,
            IsExposition = isExposition
        };
        await _logger.LogPlayerInputAsync(entry);
    }

    private async Task LogInputRuleResultAsync(string input, InputRuleResult result, string sessionId, int round)
    {
        var entry = new InputRuleLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            Input = input,
            IsRejected = result.IsRejected,
            RejectionMessage = result.RejectionMessage,
            Violations = result.Violations.Select(v => new RuleViolationEntry
            {
                RuleName = v.RuleName,
                Description = v.Description,
                Severity = v.Severity.ToString()
            }).ToList()
        };
        await _logger.LogInputRuleResultAsync(entry);
    }

    private async Task LogStateRuleResultAsync(List<RuleViolation> violations, string sessionId, int round)
    {
        var entry = new StateRuleLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            Violations = violations.Select(v => new RuleViolationEntry
            {
                RuleName = v.RuleName,
                Description = v.Description,
                Severity = v.Severity.ToString()
            }).ToList()
        };
        await _logger.LogStateRuleResultAsync(entry);
    }

    private async Task LogPlotArcAsync(PlotArc.PlotArc arc, bool isRegeneration, string sessionId, int round)
    {
        var entry = new PlotArcLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            EstablishedNpcs = arc.EstablishedNpcs,
            EstablishedLocations = arc.EstablishedLocations,
            EstablishedItems = arc.EstablishedItems,
            Backstory = arc.Backstory,
            PlotPoints = arc.PlotPoints,
            Climax = arc.Climax,
            CurrentPlotPointIndex = arc.CurrentPlotPointIndex,
            IsRegeneration = isRegeneration
        };

        await _logger.LogPlotArcAsync(entry);
    }

    private async Task LogNarrativeEvalAsync(NarrativeEvaluation eval, string sessionId, int round)
    {
        var entry = new NarrativeEvalLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            UpdateLocation = eval.UpdateLocation,
            UpdatePlayer = eval.UpdatePlayer,
            UpdateEquipment = eval.UpdateEquipment,
            HealthDelta = eval.HealthDelta,
            LogEntry = eval.LogEntry,
            PlotAlignment = eval.PlotAlignment
        };
        await _logger.LogNarrativeEvalAsync(entry);
    }
}
