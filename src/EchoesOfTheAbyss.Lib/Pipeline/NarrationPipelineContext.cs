using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.PlotArc;
using EchoesOfTheAbyss.Lib.Rules;

namespace EchoesOfTheAbyss.Lib.Pipeline;

public class NarrationPipelineContext
{
    // Inputs (set by orchestrator before pipeline runs)
    public required string PlayerInput { get; init; }
    public required string Narration { get; init; }
    public required WorldContext CurrentWorldContext { get; init; }
    public required bool IsFirstTurn { get; init; }
    public required int Round { get; init; }
    public PlotArc.PlotArc? PlotArc { get; set; }

    // Accumulated by agents
    public PlotArcAction PlotAction { get; set; } = PlotArcAction.None;
    public NarrativeEvaluation? Evaluation { get; set; }
    public Location.Location? NewLocation { get; set; }
    public Player.Player? NewPlayer { get; set; }
    public Equipment.Equipment? NewEquipment { get; set; }
    public WorldContext? NewWorldContext { get; set; }
    public List<RuleViolation> Violations { get; set; } = [];
}
