using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.State;

public class EquipmentSlotValidationRule : IStateRule
{
    private static readonly Dictionary<EquipmentSlot, string[]> SlotKeywords = new()
    {
        [EquipmentSlot.Head] = ["helmet", "helm", "hood", "cap", "crown", "circlet", "hat", "headband", "mask", "coif"],
        [EquipmentSlot.Torso] = ["armor", "shirt", "robe", "tunic", "chainmail", "breastplate", "cloak", "vest", "jerkin", "jacket", "mail", "cuirass", "tabard"],
        [EquipmentSlot.Legs] = ["pants", "trousers", "leggings", "greaves", "breeches", "skirt", "chaps", "leg"],
        [EquipmentSlot.Feet] = ["boots", "shoes", "sandals", "wrap", "moccasin", "slipper", "sabatons", "foot"],
        [EquipmentSlot.RightHand] = ["sword", "axe", "staff", "bow", "mace", "dagger", "spear", "hammer", "blade", "club", "wand", "knife", "scythe", "halberd", "pike", "rapier", "weapon"],
        [EquipmentSlot.LeftHand] = ["shield", "torch", "lantern", "buckler", "orb", "tome", "book",
            "sword", "axe", "dagger", "knife", "blade", "club", "mace", "wand"],
    };

    public StateRuleResult Evaluate(StateRuleContext context)
    {
        var prev = context.Previous.Equipment;
        var proposed = context.Proposed.Equipment;
        var violations = new List<RuleViolation>();

        var corrected = new Equipment.Equipment
        {
            Head = ValidateSlot(prev.Head, proposed.Head, EquipmentSlot.Head, violations),
            Torso = ValidateSlot(prev.Torso, proposed.Torso, EquipmentSlot.Torso, violations),
            Legs = ValidateSlot(prev.Legs, proposed.Legs, EquipmentSlot.Legs, violations),
            Feet = ValidateSlot(prev.Feet, proposed.Feet, EquipmentSlot.Feet, violations),
            RightHand = ValidateSlot(prev.RightHand, proposed.RightHand, EquipmentSlot.RightHand, violations),
            LeftHand = ValidateSlot(prev.LeftHand, proposed.LeftHand, EquipmentSlot.LeftHand, violations),
        };

        if (violations.Count == 0)
            return StateRuleResult.NoChange(context.Proposed);

        return new StateRuleResult(
            WorldContextCloner.WithEquipment(context.Proposed, corrected),
            violations);
    }

    private static T ValidateSlot<T>(T? prev, T? proposed, EquipmentSlot slot, List<RuleViolation> violations)
        where T : EquipmentPiece
    {
        if (proposed is null || string.IsNullOrWhiteSpace(proposed.Name))
            return proposed!;

        var itemName = proposed.Name.ToLowerInvariant();
        var bestSlot = FindBestSlot(itemName);

        if (bestSlot is not null && bestSlot != slot)
        {
            violations.Add(new RuleViolation(nameof(EquipmentSlotValidationRule),
                $"\"{proposed.Name}\" doesn't belong in {slot} slot (looks like {bestSlot}); reverting to previous",
                RuleSeverity.Warning));

            return prev!;
        }

        return proposed;
    }

    private static EquipmentSlot? FindBestSlot(string itemName)
    {
        EquipmentSlot? bestSlot = null;
        var bestScore = 0;

        foreach (var (slot, keywords) in SlotKeywords)
        {
            var score = keywords.Count(kw => itemName.Contains(kw));
            if (score > bestScore)
            {
                bestScore = score;
                bestSlot = slot;
            }
        }

        return bestSlot;
    }
}
