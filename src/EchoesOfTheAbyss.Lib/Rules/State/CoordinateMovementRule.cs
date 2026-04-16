using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.State;

public class CoordinateMovementRule : IStateRule
{
    private const int MaxUnitsPerTurn = 5; // 5 units = 50 meters

    public StateRuleResult Evaluate(StateRuleContext context)
    {
        var prev = context.Previous.CurrentLocation.Coordinates;
        var proposed = context.Proposed.CurrentLocation.Coordinates;

        var dx = proposed.X - prev.X;
        var dy = proposed.Y - prev.Y;
        var distance = Math.Abs(dx) + Math.Abs(dy);

        if (distance <= MaxUnitsPerTurn)
            return StateRuleResult.NoChange(context.Proposed);

        var scale = (double)MaxUnitsPerTurn / distance;
        var clampedX = prev.X + (int)Math.Round(dx * scale);
        var clampedY = prev.Y + (int)Math.Round(dy * scale);

        var correctedLocation = new Location.Location
        {
            Coordinates = new Location.Coordinates(clampedX, clampedY),
            Title = context.Proposed.CurrentLocation.Title,
            ShortDescription = context.Proposed.CurrentLocation.ShortDescription,
            LongDescription = context.Proposed.CurrentLocation.LongDescription,
            Type = context.Proposed.CurrentLocation.Type
        };

        return new StateRuleResult(
            WorldContextCloner.WithLocation(context.Proposed, correctedLocation),
            [new RuleViolation(nameof(CoordinateMovementRule),
                $"Movement clamped from ({prev.X},{prev.Y})->({proposed.X},{proposed.Y}) to ({clampedX},{clampedY}) (max {MaxUnitsPerTurn} units/turn)",
                RuleSeverity.Info)]);
    }
}
