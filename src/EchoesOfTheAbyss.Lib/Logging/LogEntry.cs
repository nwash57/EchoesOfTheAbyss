namespace EchoesOfTheAbyss.Lib.Logging;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SessionId { get; set; } = string.Empty;
    public int Round { get; set; }
}
