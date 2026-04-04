using EchoesOfTheAbyss.Lib.Models;

namespace EchoesOfTheAbyss.Lib.Models;

public class NarrationLogEntry : LogEntry
{
    public string Thoughts { get; set; } = string.Empty;
    public string Speech { get; set; } = string.Empty;
}
