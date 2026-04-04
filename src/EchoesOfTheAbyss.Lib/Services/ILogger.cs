using System.Text.Json;
using EchoesOfTheAbyss.Lib.Models;

namespace EchoesOfTheAbyss.Lib.Services;

public interface ILogger
{
    Task InitializeAsync(string sessionId);
    Task LogNarrationAsync(NarrationLogEntry entry);
    Task LogWorldStateAsync(WorldStateLogEntry entry);
    Task CloseSessionAsync();
}
