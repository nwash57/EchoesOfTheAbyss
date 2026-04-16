namespace EchoesOfTheAbyss.Lib.Logging;

public interface ILogger
{
    string? CurrentSessionPath { get; }
    Task InitializeAsync(string sessionId);
    Task LogNarrationAsync(NarrationLogEntry entry);
    Task LogWorldStateAsync(WorldStateLogEntry entry);
    Task LogPlayerInputAsync(PlayerInputLogEntry entry);
    Task LogInputRuleResultAsync(InputRuleLogEntry entry);
    Task LogStateRuleResultAsync(StateRuleLogEntry entry);
    Task LogNarrativeEvalAsync(NarrativeEvalLogEntry entry);
    Task LogPlotArcAsync(PlotArcLogEntry entry);
    Task CloseSessionAsync();
}
