using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Narrative;

public interface INarrativeEvaluator
{
    Task<NarrativeEvaluation> EvaluateAsync(string playerInput, string narration, WorldContext current, string? plotObjective = null);
}
