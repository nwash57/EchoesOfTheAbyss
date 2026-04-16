using System.Text.RegularExpressions;
using EchoesOfTheAbyss.Lib.Equipment;

namespace EchoesOfTheAbyss.Lib.Rules.Input;

public partial class PhantomItemRule : IInputRule
{
    // Matches "I use my [item]", "I swing my [item]", "I wield the [item]", etc.
    [GeneratedRegex(
        @"(?:use|swing|wield|draw|equip|brandish|throw|fire|shoot|activate)\s+(?:my|the|a|an)\s+(.+?)(?:\s+(?:at|on|against|toward)|\.|,|!|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ItemActionPattern();

    public InputRuleResult Evaluate(InputRuleContext context)
    {
        var input = context.PlayerInput;
        var equipment = context.CurrentWorld.Equipment;

        var match = ItemActionPattern().Match(input);
        if (!match.Success)
            return InputRuleResult.Pass(input);

        var mentionedItem = match.Groups[1].Value.Trim();

        if (string.IsNullOrWhiteSpace(mentionedItem))
            return InputRuleResult.Pass(input);

        var equippedNames = GetEquippedItemNames(equipment);

        // If the player has no equipment at all, reject — prevents phantom items
        if (equippedNames.Count == 0)
            return InputRuleResult.Reject(input, nameof(PhantomItemRule),
                "You aren't carrying any items to use.");

        // Check if any equipped item name fuzzy-matches the mentioned item
        if (equippedNames.Any(name => FuzzyMatch(mentionedItem, name)))
            return InputRuleResult.Pass(input);

        return InputRuleResult.Reject(input, nameof(PhantomItemRule),
            $"You don't have a \"{mentionedItem}\" in your possession.");
    }

    private static List<string> GetEquippedItemNames(Equipment.Equipment equipment)
    {
        var names = new List<string>();
        AddIfEquipped(names, equipment.Head);
        AddIfEquipped(names, equipment.Torso);
        AddIfEquipped(names, equipment.Legs);
        AddIfEquipped(names, equipment.Feet);
        AddIfEquipped(names, equipment.RightHand);
        AddIfEquipped(names, equipment.LeftHand);
        return names;
    }

    private static void AddIfEquipped(List<string> names, EquipmentPiece? piece)
    {
        if (piece is not null && !string.IsNullOrWhiteSpace(piece.Name))
            names.Add(piece.Name);
    }

    private static bool FuzzyMatch(string mentioned, string equipped)
    {
        mentioned = mentioned.ToLowerInvariant();
        equipped = equipped.ToLowerInvariant();

        // Exact match
        if (equipped.Contains(mentioned) || mentioned.Contains(equipped))
            return true;

        // Check if all words in the mentioned item appear in the equipped item name (or vice versa)
        var mentionedWords = mentioned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var equippedWords = equipped.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // If any significant word from mentioned matches any word in equipped
        return mentionedWords.Any(mw => mw.Length > 2 && equippedWords.Any(ew => ew.Contains(mw) || mw.Contains(ew)));
    }
}
