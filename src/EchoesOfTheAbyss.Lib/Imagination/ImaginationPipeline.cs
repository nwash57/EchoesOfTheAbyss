using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Location;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Player;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Imagination;

public class ImaginationPipeline : IImaginationPipeline
{
    private readonly INarrativeEvaluator _evaluator;
    private readonly ILocationExtractor _locationExtractor;
    private readonly IPlayerStateUpdater _playerStateUpdater;
    private readonly IEquipmentExtractor _equipmentExtractor;

    public ImaginationPipeline(
        INarrativeEvaluator evaluator,
        ILocationExtractor locationExtractor,
        IPlayerStateUpdater playerStateUpdater,
        IEquipmentExtractor equipmentExtractor)
    {
        _evaluator = evaluator;
        _locationExtractor = locationExtractor;
        _playerStateUpdater = playerStateUpdater;
        _equipmentExtractor = equipmentExtractor;
    }

    public Task<NarrativeEvaluation> EvaluateAsync(string playerInput, string narration, WorldContext current)
    {
        return _evaluator.EvaluateAsync(playerInput, narration, current);
    }

    public async Task<(WorldContext Context, NarrativeEvaluation Eval)> RunAsync(string playerInput, string narration, WorldContext current)
    {
        var eval = await _evaluator.EvaluateAsync(playerInput, narration, current);

        // First turn (exposition): force all updates so initial world state is populated
        var isFirstTurn = string.IsNullOrEmpty(playerInput);
        if (isFirstTurn)
        {
            eval.UpdateLocation = true;
            eval.UpdatePlayer = true;
            eval.UpdateEquipment = true;
        }

        var history = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.ImaginationSystem),
            new UserChatMessage($"""
                Latest narration: "{narration}"
                Previous world state: {current}

                Extract the current location.
                """)
        };

        var location = await _locationExtractor.ExtractAsync(history, current, eval);
        var player = await _playerStateUpdater.UpdateAsync(history, current, eval);
        var equipment = await _equipmentExtractor.ExtractAsync(history, current, eval);

        var newLog = string.IsNullOrWhiteSpace(eval.LogEntry)
            ? new List<string>(current.AdventureLog)
            : new List<string>(current.AdventureLog) { eval.LogEntry };

        return (new WorldContext
        {
            Difficulty = current.Difficulty,
            NarrationVerbosity = current.NarrationVerbosity,
            Player = player,
            CurrentLocation = location,
            Equipment = equipment,
            AdventureLog = newLog
        }, eval);
    }
}
