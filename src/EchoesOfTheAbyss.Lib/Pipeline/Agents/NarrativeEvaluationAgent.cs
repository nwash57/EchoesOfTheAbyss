using EchoesOfTheAbyss.Lib.Narrative;

namespace EchoesOfTheAbyss.Lib.Pipeline.Agents;

public class NarrativeEvaluationAgent : INarrationPipelineAgent
{
    private readonly INarrativeEvaluator _evaluator;

    public NarrativeEvaluationAgent(INarrativeEvaluator evaluator)
    {
        _evaluator = evaluator;
    }

    public int Order => 10;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken ct = default)
    {
        if (context.IsFirstTurn)
        {
            context.Evaluation = new NarrativeEvaluation
            {
                UpdateLocation = true,
                UpdatePlayer = true,
                UpdateEquipment = true,
                HealthDelta = 0,
                LogEntry = "The adventure begins.",
                PlotAlignment = "on_track"
            };
            return;
        }

        var plotObjective = context.PlotArc?.CurrentObjective;

        var eval = await _evaluator.EvaluateAsync(
            context.PlayerInput, context.Narration, context.CurrentWorldContext, plotObjective);

        context.Evaluation = eval;
    }
}
