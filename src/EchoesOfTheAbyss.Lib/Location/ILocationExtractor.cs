using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Narrative;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Location;

public interface ILocationExtractor
{
    Task<Location> ExtractAsync(List<ChatMessage> history, WorldContext current, NarrativeEvaluation eval);
}
