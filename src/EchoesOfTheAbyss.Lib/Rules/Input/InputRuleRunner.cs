using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.Input;

public class InputRuleRunner : IInputRuleRunner
{
    private readonly IEnumerable<IInputRule> _rules;

    public InputRuleRunner(IEnumerable<IInputRule> rules)
    {
        _rules = rules;
    }

    public InputRuleResult Evaluate(string playerInput, WorldContext world, string lastNarration)
    {
        var context = new InputRuleContext(playerInput, world, lastNarration);
        var allViolations = new List<RuleViolation>();
        var currentInput = playerInput;

        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(context with { PlayerInput = currentInput });

            if (result.IsRejected)
                return result;

            allViolations.AddRange(result.Violations);
            currentInput = result.SanitizedInput;
        }

        return new InputRuleResult(false, null, currentInput, allViolations);
    }
}
