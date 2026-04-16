using EchoesOfTheAbyss.Lib.PlotArc;

namespace EchoesOfTheAbyss.Lib.Pipeline.Agents;

public class PlotDriftAgent : INarrationPipelineAgent
{
    private readonly IPlotArcTracker _tracker;
    private readonly IPlotArcGenerator _generator;

    public PlotDriftAgent(IPlotArcTracker tracker, IPlotArcGenerator generator)
    {
        _tracker = tracker;
        _generator = generator;
    }

    public int Order => 20;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken ct = default)
    {
        if (context.PlotArc is null || context.Evaluation is null)
            return;

        var action = _tracker.Track(context.Evaluation);
        context.PlotAction = action;

        switch (action)
        {
            case PlotArcAction.AdvancePlot:
                if (context.PlotArc.CurrentPlotPointIndex < context.PlotArc.PlotPoints.Count - 1)
                    context.PlotArc.CurrentPlotPointIndex++;
                break;

            case PlotArcAction.Regenerate:
                context.PlotArc = await _generator.RegenerateAsync(
                    context.PlotArc,
                    context.CurrentWorldContext.AdventureLog,
                    ct);
                break;
        }
    }
}
