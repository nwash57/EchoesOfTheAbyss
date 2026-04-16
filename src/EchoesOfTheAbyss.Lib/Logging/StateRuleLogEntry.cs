namespace EchoesOfTheAbyss.Lib.Logging;

public class StateRuleLogEntry : LogEntry
{
    public List<RuleViolationEntry> Violations { get; set; } = [];
}
