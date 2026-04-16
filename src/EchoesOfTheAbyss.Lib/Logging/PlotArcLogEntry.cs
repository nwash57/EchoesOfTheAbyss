namespace EchoesOfTheAbyss.Lib.Logging;

public class PlotArcLogEntry : LogEntry
{
    public string EstablishedNpcs { get; set; } = string.Empty;
    public string EstablishedLocations { get; set; } = string.Empty;
    public string EstablishedItems { get; set; } = string.Empty;
    public string Backstory { get; set; } = string.Empty;
    public List<string> PlotPoints { get; set; } = [];
    public string Climax { get; set; } = string.Empty;
    public int CurrentPlotPointIndex { get; set; }
    public bool IsRegeneration { get; set; }
}
