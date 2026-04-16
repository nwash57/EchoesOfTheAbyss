using EchoesOfTheAbyss.Lib.Imagination;

namespace EchoesOfTheAbyss.Lib.Rules.State;

internal static class WorldContextCloner
{
    public static WorldContext WithPlayer(WorldContext ctx, Player.Player player) => new()
    {
        Difficulty = ctx.Difficulty,
        NarrationVerbosity = ctx.NarrationVerbosity,
        Player = player,
        Equipment = ctx.Equipment,
        CurrentLocation = ctx.CurrentLocation,
        AdventureLog = ctx.AdventureLog
    };

    public static WorldContext WithEquipment(WorldContext ctx, Equipment.Equipment equipment) => new()
    {
        Difficulty = ctx.Difficulty,
        NarrationVerbosity = ctx.NarrationVerbosity,
        Player = ctx.Player,
        Equipment = equipment,
        CurrentLocation = ctx.CurrentLocation,
        AdventureLog = ctx.AdventureLog
    };

    public static WorldContext WithLocation(WorldContext ctx, Location.Location location) => new()
    {
        Difficulty = ctx.Difficulty,
        NarrationVerbosity = ctx.NarrationVerbosity,
        Player = ctx.Player,
        Equipment = ctx.Equipment,
        CurrentLocation = location,
        AdventureLog = ctx.AdventureLog
    };
}
