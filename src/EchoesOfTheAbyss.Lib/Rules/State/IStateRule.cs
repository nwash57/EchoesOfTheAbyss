using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Narrative;

namespace EchoesOfTheAbyss.Lib.Rules.State;

public record StateRuleContext(
    WorldContext Previous,
    WorldContext Proposed,
    string Narration,
    string PlayerInput,
    NarrativeEvaluation Eval);

public record StateRuleResult(WorldContext CorrectedContext, List<RuleViolation> Violations)
{
    public static StateRuleResult NoChange(WorldContext context) =>
        new(context, []);
}

public interface IStateRule
{
    StateRuleResult Evaluate(StateRuleContext context);
}

public interface IStateRuleRunner
{
    StateRuleResult Evaluate(WorldContext previous, WorldContext proposed, string narration, string playerInput, NarrativeEvaluation eval);
}
