using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Pipeline.Agents;

public class WorldContextAssemblyAgent : INarrationPipelineAgent
{
    public int Order => 40;

    public Task ExecuteAsync(NarrationPipelineContext context, CancellationToken ct = default)
    {
        var current = context.CurrentWorldContext;
        var eval = context.Evaluation!;

        var newLog = string.IsNullOrWhiteSpace(eval.LogEntry)
            ? new List<string>(current.AdventureLog)
            : new List<string>(current.AdventureLog) { eval.LogEntry };

        context.NewWorldContext = new WorldContext
        {
            Difficulty = current.Difficulty,
            NarrationVerbosity = current.NarrationVerbosity,
            Player = context.NewPlayer ?? current.Player,
            CurrentLocation = context.NewLocation ?? current.CurrentLocation,
            Equipment = context.NewEquipment ?? current.Equipment,
            AdventureLog = newLog
        };

        return Task.CompletedTask;
    }
}
