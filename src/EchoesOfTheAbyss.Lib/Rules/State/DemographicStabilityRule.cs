using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.State;

public class DemographicStabilityRule : IStateRule
{
    public StateRuleResult Evaluate(StateRuleContext context)
    {
        var prev = context.Previous.Player.Demographics;
        var proposed = context.Proposed.Player.Demographics;
        var violations = new List<RuleViolation>();

        // If previous demographics aren't set yet (Turn 0), allow the LLM to set them
        if (string.IsNullOrEmpty(prev.FirstName) && string.IsNullOrEmpty(prev.LastName))
            return StateRuleResult.NoChange(context.Proposed);

        var correctedFirst = proposed.FirstName;
        var correctedLast = proposed.LastName;
        var correctedOccupation = proposed.Occupation;
        var correctedAge = proposed.Age;

        if (!string.IsNullOrEmpty(prev.FirstName) && proposed.FirstName != prev.FirstName)
        {
            correctedFirst = prev.FirstName;
            violations.Add(new RuleViolation(nameof(DemographicStabilityRule),
                $"First name reverted from \"{proposed.FirstName}\" to \"{prev.FirstName}\"",
                RuleSeverity.Warning));
        }

        if (!string.IsNullOrEmpty(prev.LastName) && proposed.LastName != prev.LastName)
        {
            correctedLast = prev.LastName;
            violations.Add(new RuleViolation(nameof(DemographicStabilityRule),
                $"Last name reverted from \"{proposed.LastName}\" to \"{prev.LastName}\"",
                RuleSeverity.Warning));
        }

        if (!string.IsNullOrEmpty(prev.Occupation) && proposed.Occupation != prev.Occupation)
        {
            correctedOccupation = prev.Occupation;
            violations.Add(new RuleViolation(nameof(DemographicStabilityRule),
                $"Occupation reverted from \"{proposed.Occupation}\" to \"{prev.Occupation}\"",
                RuleSeverity.Warning));
        }

        if (prev.Age > 0 && Math.Abs(proposed.Age - prev.Age) > 1)
        {
            correctedAge = prev.Age;
            violations.Add(new RuleViolation(nameof(DemographicStabilityRule),
                $"Age reverted from {proposed.Age} to {prev.Age} (max 1 year change per turn)",
                RuleSeverity.Warning));
        }

        if (violations.Count == 0)
            return StateRuleResult.NoChange(context.Proposed);

        var correctedPlayer = new Player.Player
        {
            Demographics = new Player.ActorDemographics
            {
                FirstName = correctedFirst,
                LastName = correctedLast,
                Age = correctedAge,
                Occupation = correctedOccupation
            },
            Stats = context.Proposed.Player.Stats
        };

        return new StateRuleResult(
            WorldContextCloner.WithPlayer(context.Proposed, correctedPlayer),
            violations);
    }
}
