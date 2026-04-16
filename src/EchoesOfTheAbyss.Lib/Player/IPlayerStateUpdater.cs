using OpenAI.Chat;
using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Narrative;

namespace EchoesOfTheAbyss.Lib.Player;

public interface IPlayerStateUpdater
{
    Task<Player> UpdateAsync(List<ChatMessage> history, WorldContext current, NarrativeEvaluation eval);
}
