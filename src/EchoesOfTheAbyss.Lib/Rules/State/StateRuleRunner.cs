using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Narrative;

namespace EchoesOfTheAbyss.Lib.Rules.State;

public class StateRuleRunner : IStateRuleRunner
{
    private readonly IEnumerable<IStateRule> _rules;

    public StateRuleRunner(IEnumerable<IStateRule> rules)
    {
        _rules = rules;
    }

    public StateRuleResult Evaluate(WorldContext previous, WorldContext proposed, string narration, string playerInput, NarrativeEvaluation eval)
    {
        var allViolations = new List<RuleViolation>();
        var current = proposed;

        foreach (var rule in _rules)
        {
            var context = new StateRuleContext(previous, current, narration, playerInput, eval);
            var result = rule.Evaluate(context);

            allViolations.AddRange(result.Violations);
            current = result.CorrectedContext;
        }

        return new StateRuleResult(current, allViolations);
    }
}
