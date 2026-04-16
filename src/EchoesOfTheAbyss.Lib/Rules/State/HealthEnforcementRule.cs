using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.State;

public class HealthEnforcementRule : IStateRule
{
    public StateRuleResult Evaluate(StateRuleContext context)
    {
        var prev = context.Previous;
        var proposed = context.Proposed;
        var eval = context.Eval;

        var expectedHealth = Math.Clamp(
            prev.Player.Stats.CurrentHealth + eval.HealthDelta,
            0,
            prev.Player.Stats.MaxHealth);

        // Preserve Second Wind adjustment
        if (proposed.Player.Stats.HasUsedSecondWind && !prev.Player.Stats.HasUsedSecondWind)
        {
            expectedHealth = Math.Max(expectedHealth, 15);
        }

        if (proposed.Player.Stats.CurrentHealth == expectedHealth)
            return StateRuleResult.NoChange(proposed);

        var correctedPlayer = new Player.Player
        {
            Demographics = proposed.Player.Demographics,
            Stats = new Player.PlayerStats
            {
                CurrentHealth = expectedHealth,
                MaxHealth = proposed.Player.Stats.MaxHealth,
                BaseArmor = proposed.Player.Stats.BaseArmor,
                BaseStrength = proposed.Player.Stats.BaseStrength,
                HasUsedSecondWind = proposed.Player.Stats.HasUsedSecondWind
            }
        };

        return new StateRuleResult(
            WorldContextCloner.WithPlayer(proposed, correctedPlayer),
            [new RuleViolation(nameof(HealthEnforcementRule),
                $"HP corrected from {proposed.Player.Stats.CurrentHealth} to {expectedHealth} (delta: {eval.HealthDelta})",
                RuleSeverity.Warning)]);
    }
}
