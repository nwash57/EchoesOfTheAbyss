namespace EchoesOfTheAbyss.Lib.Logging;

public class InputRuleLogEntry : LogEntry
{
    public string Input { get; set; } = string.Empty;
    public bool IsRejected { get; set; }
    public string? RejectionMessage { get; set; }
    public List<RuleViolationEntry> Violations { get; set; } = [];
}

public class RuleViolationEntry
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
