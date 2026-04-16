using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.State;

public class EquipmentPersistenceRule : IStateRule
{
    private static readonly string[] LossKeywords =
    [
        "lost", "lose", "broke", "broken", "shattered", "destroyed", "dropped", "discard",
        "ripped", "torn", "stripped", "taken", "stolen", "crumbled", "dissolved",
        "consumed", "sacrificed", "fell", "removed"
    ];

    public StateRuleResult Evaluate(StateRuleContext context)
    {
        var prev = context.Previous.Equipment;
        var proposed = context.Proposed.Equipment;
        var eval = context.Eval;
        var narration = context.Narration.ToLowerInvariant();
        var violations = new List<RuleViolation>();

        // If equipment update wasn't flagged, carry forward everything exactly
        if (!eval.UpdateEquipment)
        {
            if (!EquipmentEquals(prev, proposed))
            {
                violations.Add(new RuleViolation(nameof(EquipmentPersistenceRule),
                    "Equipment changed without update flag; reverting to previous state",
                    RuleSeverity.Warning));

                return new StateRuleResult(
                    WorldContextCloner.WithEquipment(context.Proposed, prev),
                    violations);
            }

            return StateRuleResult.NoChange(context.Proposed);
        }

        // Update was flagged — check that losses are narratively justified
        var corrected = new Equipment.Equipment
        {
            Head = ValidateSlot(prev.Head, proposed.Head, "Head", narration, violations),
            Torso = ValidateSlot(prev.Torso, proposed.Torso, "Torso", narration, violations),
            Legs = ValidateSlot(prev.Legs, proposed.Legs, "Legs", narration, violations),
            Feet = ValidateSlot(prev.Feet, proposed.Feet, "Feet", narration, violations),
            RightHand = ValidateSlot(prev.RightHand, proposed.RightHand, "RightHand", narration, violations),
            LeftHand = ValidateSlot(prev.LeftHand, proposed.LeftHand, "LeftHand", narration, violations),
        };

        if (violations.Count == 0)
            return StateRuleResult.NoChange(context.Proposed);

        return new StateRuleResult(
            WorldContextCloner.WithEquipment(context.Proposed, corrected),
            violations);
    }

    private static T ValidateSlot<T>(T? prev, T? proposed, string slotName, string narration, List<RuleViolation> violations)
        where T : EquipmentPiece
    {
        var hadItem = prev is not null && !string.IsNullOrWhiteSpace(prev.Name);
        var hasItem = proposed is not null && !string.IsNullOrWhiteSpace(proposed.Name);

        // Item disappeared — check if narration justifies the loss
        if (hadItem && !hasItem)
        {
            var itemName = prev!.Name.ToLowerInvariant();
            var lossJustified = LossKeywords.Any(kw => narration.Contains(kw))
                || narration.Contains(itemName);

            if (!lossJustified)
            {
                violations.Add(new RuleViolation(nameof(EquipmentPersistenceRule),
                    $"{slotName}: \"{prev.Name}\" disappeared without narrative justification; restoring",
                    RuleSeverity.Warning));

                return prev;
            }
        }

        return proposed!;
    }

    private static bool EquipmentEquals(Equipment.Equipment a, Equipment.Equipment b)
    {
        return SlotEquals(a.Head, b.Head)
            && SlotEquals(a.Torso, b.Torso)
            && SlotEquals(a.Legs, b.Legs)
            && SlotEquals(a.Feet, b.Feet)
            && SlotEquals(a.RightHand, b.RightHand)
            && SlotEquals(a.LeftHand, b.LeftHand);
    }

    private static bool SlotEquals(EquipmentPiece? a, EquipmentPiece? b)
    {
        var aEmpty = a is null || string.IsNullOrWhiteSpace(a.Name);
        var bEmpty = b is null || string.IsNullOrWhiteSpace(b.Name);

        if (aEmpty && bEmpty) return true;
        if (aEmpty != bEmpty) return false;

        return a!.Name == b!.Name
            && a.Armor == b.Armor
            && a.Damage == b.Damage;
    }
}
