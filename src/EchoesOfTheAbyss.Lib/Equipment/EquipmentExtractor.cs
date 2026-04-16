using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Shared;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Equipment;

public class EquipmentExtractor : IEquipmentExtractor
{
    private readonly IChatService _chatService;

    public EquipmentExtractor(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<Equipment> ExtractAsync(List<ChatMessage> history, WorldContext current, NarrativeEvaluation eval)
    {
        if (!eval.UpdateEquipment)
            return current.Equipment;

        var isFirstTurn = current.Equipment.IsEmpty();

        var baseInstructions = $"""
            Analyze the narration and extract the player's current equipment.

            CURRENT EQUIPMENT (carry forward unless explicitly changed):
            {current.Equipment}

            SLOT RULES — items MUST go in the correct body slot:
            - Head: helmets, hoods, caps, crowns, circlets
            - Torso: armor, shirts, robes, tunics, chainmail, breastplates, cloaks
            - Legs: pants, trousers, leggings, greaves
            - Feet: boots, shoes, sandals, foot wraps
            - Right Hand: primary weapons (swords, axes, staffs, bows)
            - Left Hand: shields, secondary weapons, torches
            """;

        var itemRules = isFirstTurn
            ? """
            INITIAL EQUIPMENT SETUP:
            - This is the opening narration specifically written to describe the player's starting equipment.
            - Extract EVERY item the narration mentions the player wearing, carrying, or having on their person.
            - Items described at the belt, hip, or side are weapons — place them in Right Hand.
            - A cloak, tunic, or armor goes in Torso. Boots go in Feet. A hood or helm goes in Head.
            - If the narration mentions an item but not its exact slot, infer the correct slot from the item type.
            - Do NOT leave slots empty if the narration describes an item that fits that slot.
            """
            : """
            CRITICAL:
            - Maintain all existing equipment that is still in the player's possession.
            - Only add new equipment if the player EXPLICITLY took it (e.g., "you pick up the dagger").
            - Do NOT add items that were only mentioned or seen (e.g., "you find a chest with a rusted sword" does NOT mean the sword is now equipped).
            - A helmet MUST go in the Head slot, boots MUST go in Feet, etc.
            """;

        history.Add(new UserChatMessage($"{baseInstructions}\n{itemRules}"));

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "equipment_context",
                jsonSchema: BinaryData.FromString(Equipment.JsonSchema),
                jsonSchemaIsStrict: false)
        };

        var equipmentJson = await _chatService.CompleteChatAsync(history, options);
        return equipmentJson.FromJson<Equipment>();
    }
}
