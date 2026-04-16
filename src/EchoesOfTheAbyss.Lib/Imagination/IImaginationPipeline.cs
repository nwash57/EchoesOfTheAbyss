using EchoesOfTheAbyss.Lib.Narrative;

namespace EchoesOfTheAbyss.Lib.Imagination;

public interface IImaginationPipeline
{
    Task<(WorldContext Context, NarrativeEvaluation Eval)> RunAsync(
        string playerInput, string narration, WorldContext current);

    Task<NarrativeEvaluation> EvaluateAsync(
        string playerInput, string narration, WorldContext current);
}
