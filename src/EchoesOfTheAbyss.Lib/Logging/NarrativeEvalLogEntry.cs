namespace EchoesOfTheAbyss.Lib.Logging;

public class NarrativeEvalLogEntry : LogEntry
{
    public bool UpdateLocation { get; set; }
    public bool UpdatePlayer { get; set; }
    public bool UpdateEquipment { get; set; }
    public int HealthDelta { get; set; }
    public string LogEntry { get; set; } = string.Empty;
    public string PlotAlignment { get; set; } = string.Empty;
}
