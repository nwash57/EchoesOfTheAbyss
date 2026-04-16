using System.Text.RegularExpressions;

namespace EchoesOfTheAbyss.Lib.Rules.Input;

public partial class TimeSkipRule : IInputRule
{
    // Matches phrases like "I wait 12 years", "wait 3 months", "5 years pass", "skip ahead 2 weeks"
    [GeneratedRegex(
        @"(?:wait|sleep|rest|meditate|skip\s+(?:ahead|forward)?)\s+(?:for\s+)?(\d+)\s+(year|month|week|day|decade|century|centurie|hour)",
        RegexOptions.IgnoreCase)]
    private static partial Regex TimeSkipActionPattern();

    [GeneratedRegex(
        @"(\d+)\s+(year|month|week|day|decade|century|centurie|hour)s?\s+(?:pass|go\s+by|elapse|later)",
        RegexOptions.IgnoreCase)]
    private static partial Regex TimePassPattern();

    private static readonly Dictionary<string, int> UnitToHours = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hour"] = 1,
        ["day"] = 24,
        ["week"] = 168,
        ["month"] = 720,
        ["year"] = 8760,
        ["decade"] = 87600,
        ["century"] = 876000,
        ["centurie"] = 876000,
    };

    private const int MaxAllowedHours = 24; // allow up to 1 day of waiting

    public InputRuleResult Evaluate(InputRuleContext context)
    {
        var input = context.PlayerInput;

        var match = TimeSkipActionPattern().Match(input);
        if (!match.Success)
            match = TimePassPattern().Match(input);

        if (!match.Success)
            return InputRuleResult.Pass(input);

        if (!int.TryParse(match.Groups[1].Value, out var amount))
            return InputRuleResult.Pass(input);

        var unit = match.Groups[2].Value.ToLowerInvariant();
        if (!UnitToHours.TryGetValue(unit, out var hoursPerUnit))
            return InputRuleResult.Pass(input);

        var totalHours = (long)amount * hoursPerUnit;

        if (totalHours <= MaxAllowedHours)
            return InputRuleResult.Pass(input);

        return InputRuleResult.Reject(input, nameof(TimeSkipRule),
            "Time does not bend to your will. What do you do right now?");
    }
}
