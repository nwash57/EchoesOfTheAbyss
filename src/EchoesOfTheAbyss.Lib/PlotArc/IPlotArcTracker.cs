using EchoesOfTheAbyss.Lib.Narrative;

namespace EchoesOfTheAbyss.Lib.PlotArc;

public enum PlotArcAction
{
    None,
    AdvancePlot,
    Regenerate
}

public record PlotArcTrackerState(int ConsecutiveOnTrack, int DriftScore);

public interface IPlotArcTracker
{
    PlotArcAction Track(NarrativeEvaluation eval);
    PlotArcTrackerState GetState();
}
