using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Narrative;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Equipment;

public interface IEquipmentExtractor
{
    Task<Equipment> ExtractAsync(List<ChatMessage> history, WorldContext current, NarrativeEvaluation eval);
}
