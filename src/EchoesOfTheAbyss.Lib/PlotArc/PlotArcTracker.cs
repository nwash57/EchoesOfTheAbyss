using EchoesOfTheAbyss.Lib.Narrative;

namespace EchoesOfTheAbyss.Lib.PlotArc;

public class PlotArcTracker : IPlotArcTracker
{
    private int _consecutiveOnTrack = 0;
    private int _driftScore = 0;

    private const int AdvanceThreshold = 2;
    private const int RegenerateThreshold = 5;

    public PlotArcAction Track(NarrativeEvaluation eval)
    {
        switch (eval.PlotAlignment)
        {
            case "on_track":
                _driftScore = 0;
                _consecutiveOnTrack++;
                if (_consecutiveOnTrack >= AdvanceThreshold)
                {
                    _consecutiveOnTrack = 0;
                    return PlotArcAction.AdvancePlot;
                }
                return PlotArcAction.None;

            case "drifting":
                _consecutiveOnTrack = 0;
                _driftScore++;
                break;

            case "diverged":
                _consecutiveOnTrack = 0;
                _driftScore += 2;
                break;

            default:
                _consecutiveOnTrack = 0;
                break;
        }

        if (_driftScore >= RegenerateThreshold)
        {
            _driftScore = 0;
            _consecutiveOnTrack = 0;
            return PlotArcAction.Regenerate;
        }

        return PlotArcAction.None;
    }

    public PlotArcTrackerState GetState() => new(_consecutiveOnTrack, _driftScore);
}
