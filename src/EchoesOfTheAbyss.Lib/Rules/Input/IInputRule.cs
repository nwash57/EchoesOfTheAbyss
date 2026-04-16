using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.Input;

public record InputRuleContext(string PlayerInput, WorldContext CurrentWorld, string LastNarration);

public record InputRuleResult(
    bool IsRejected,
    string? RejectionMessage,
    string SanitizedInput,
    List<RuleViolation> Violations)
{
    public static InputRuleResult Pass(string input) =>
        new(false, null, input, []);

    public static InputRuleResult Reject(string input, string ruleName, string message) =>
        new(true, message, input, [new RuleViolation(ruleName, message, RuleSeverity.Rejection)]);
}

public interface IInputRule
{
    InputRuleResult Evaluate(InputRuleContext context);
}

public interface IInputRuleRunner
{
    InputRuleResult Evaluate(string playerInput, WorldContext world, string lastNarration);
}
