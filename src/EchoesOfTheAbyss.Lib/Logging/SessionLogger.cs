using System.Text;
using System.Text.Json;

namespace EchoesOfTheAbyss.Lib.Logging;

public class SessionLogger : ILogger
{
    private string _sessionPath = string.Empty;
    private bool _isInitialized;
    private readonly JsonSerializerOptions _jsonOptions;

    public string? CurrentSessionPath => _isInitialized ? _sessionPath : null;

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

        // Write a pointer file so external tools can find the current session
        var latestSessionFile = Path.Combine(baseLogPath, "latest-session");
        await File.WriteAllTextAsync(latestSessionFile, _sessionPath, new UTF8Encoding(false));

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

    public async Task LogPlayerInputAsync(PlayerInputLogEntry entry)
    {
        await EnsureInitializedAsync();

        var filePath = Path.Combine(_sessionPath, "player_input.log");
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.AppendAllTextAsync(filePath, json + "\n", Encoding.UTF8);
    }

    public async Task LogInputRuleResultAsync(InputRuleLogEntry entry)
    {
        await EnsureInitializedAsync();

        var filePath = Path.Combine(_sessionPath, "input_rules.log");
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.AppendAllTextAsync(filePath, json + "\n", Encoding.UTF8);
    }

    public async Task LogStateRuleResultAsync(StateRuleLogEntry entry)
    {
        await EnsureInitializedAsync();

        var filePath = Path.Combine(_sessionPath, "state_rules.log");
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.AppendAllTextAsync(filePath, json + "\n", Encoding.UTF8);
    }

    public async Task LogNarrativeEvalAsync(NarrativeEvalLogEntry entry)
    {
        await EnsureInitializedAsync();

        var filePath = Path.Combine(_sessionPath, "narrative_eval.log");
        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.AppendAllTextAsync(filePath, json + "\n", Encoding.UTF8);
    }

    public async Task LogPlotArcAsync(PlotArcLogEntry entry)
    {
        await EnsureInitializedAsync();

        var filePath = Path.Combine(_sessionPath, "plot_arc.log");
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
