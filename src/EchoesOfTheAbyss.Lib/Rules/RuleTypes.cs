namespace EchoesOfTheAbyss.Lib.Rules;

public enum RuleSeverity
{
    Info,
    Warning,
    Rejection
}

public record RuleViolation(string RuleName, string Description, RuleSeverity Severity);
