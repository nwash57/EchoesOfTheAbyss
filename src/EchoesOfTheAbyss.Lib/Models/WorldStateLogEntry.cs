using EchoesOfTheAbyss.Lib.Models;

namespace EchoesOfTheAbyss.Lib.Models;

public class WorldStateLogEntry : LogEntry
{
    public int Difficulty { get; set; }
    public ActorDemographics Demographics { get; set; } = new();
    public PlayerStats Stats { get; set; } = new();
    public Dictionary<string, string> Equipment { get; set; } = new();
    public Location CurrentLocation { get; set; } = new();

    public static WorldStateLogEntry FromWorldContext(WorldContext context, string sessionId, int round)
    {
        return new WorldStateLogEntry
        {
            Timestamp = DateTime.UtcNow,
            SessionId = sessionId,
            Round = round,
            Difficulty = context.Difficulty,
            Demographics = context.Player.Demographics,
            Stats = context.Player.Stats,
            Equipment = new Dictionary<string, string>
            {
                ["head"] = context.Equipment.Head?.ToString() ?? "Empty",
                ["torso"] = context.Equipment.Torso?.ToString() ?? "Empty",
                ["legs"] = context.Equipment.Legs?.ToString() ?? "Empty",
                ["feet"] = context.Equipment.Feet?.ToString() ?? "Empty",
                ["rightHand"] = context.Equipment.RightHand?.ToString() ?? "Empty",
                ["leftHand"] = context.Equipment.LeftHand?.ToString() ?? "Empty"
            },
            CurrentLocation = context.CurrentLocation
        };
    }
}
