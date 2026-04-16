namespace EchoesOfTheAbyss.Lib.Logging;

public class PlayerInputLogEntry : LogEntry
{
    public string Input { get; set; } = string.Empty;
    public bool IsExposition { get; set; }
}
