using System.Text;
using System.Text.Json;
using EchoesOfTheAbyss.Lib.Models;

namespace EchoesOfTheAbyss.Lib.Services;

public class SessionLogger : ILogger
{
    private string _sessionPath = string.Empty;
    private bool _isInitialized;
    private readonly JsonSerializerOptions _jsonOptions;

    public SessionLogger()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task InitializeAsync(string sessionId)
    {
        var baseDir = AppContext.BaseDirectory;
        var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName ?? baseDir;
        var baseLogPath = Path.Combine(projectRoot, "debug");
        
        if (!Directory.Exists(baseLogPath))
        {
            Directory.CreateDirectory(baseLogPath);
        }

        _sessionPath = Path.Combine(baseLogPath, sessionId);
        Directory.CreateDirectory(_sessionPath);

        _isInitialized = true;
    }

    public async Task LogNarrationAsync(NarrationLogEntry entry)
    {
        await EnsureInitializedAsync();
        
        var filePath = Path.Combine(_sessionPath, "narration.log");
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.AppendAllTextAsync(filePath, json + "\n", Encoding.UTF8);
    }

    public async Task LogWorldStateAsync(WorldStateLogEntry entry)
    {
        await EnsureInitializedAsync();
        
        var filePath = Path.Combine(_sessionPath, "world_state.log");
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.AppendAllTextAsync(filePath, json + "\n", Encoding.UTF8);
    }

    public async Task CloseSessionAsync()
    {
        if (_isInitialized)
        {
            var metadataPath = Path.Combine(_sessionPath, "session_metadata.json");
            var metadata = new SessionMetadata
            {
                SessionId = _sessionPath.Split(Path.DirectorySeparatorChar).Last(),
                StartedAt = DateTime.UtcNow,
                EndedAt = DateTime.UtcNow
            };
            
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, _jsonOptions));
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("SessionLogger not initialized. Call InitializeAsync first.");
        }
    }
}

public class SessionMetadata
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
}
