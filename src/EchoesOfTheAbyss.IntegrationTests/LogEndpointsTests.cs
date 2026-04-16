using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EchoesOfTheAbyss.IntegrationTests;

public class LogEndpointsTests : IDisposable
{
    private readonly string _debugPath;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LogEndpointsTests()
    {
        _debugPath = Path.Combine(Path.GetTempPath(), "echoes-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_debugPath);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["DebugPath"] = _debugPath
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();

        if (Directory.Exists(_debugPath))
            Directory.Delete(_debugPath, recursive: true);
    }

    [Fact]
    public async Task GetCurrentSession_NoSession_Returns404()
    {
        var response = await _client.GetAsync("/api/logs/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await DeserializeResponse(response);
        Assert.Equal("No active session", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetCurrentSession_SessionExists_ReturnsSessionInfo()
    {
        var sessionId = "test-session-123";
        var sessionPath = SetupSessionWithEmptyFiles(sessionId, "narration.log", "world_state.log");

        var response = await _client.GetAsync("/api/logs/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await DeserializeResponse(response);
        Assert.Equal(sessionId, body.GetProperty("sessionId").GetString());
        Assert.Equal(sessionPath, body.GetProperty("sessionPath").GetString());

        var files = body.GetProperty("files").EnumerateArray().Select(f => f.GetString()).ToList();
        Assert.Contains("narration.log", files);
        Assert.Contains("world_state.log", files);
    }

    [Fact]
    public async Task GetCurrentSession_SessionDirectoryDeleted_Returns404()
    {
        var sessionPath = SetupSessionWithEmptyFiles("deleted-session");
        Directory.Delete(sessionPath, recursive: true);

        var response = await _client.GetAsync("/api/logs/current");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await DeserializeResponse(response);
        Assert.Equal("Session directory not found", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetLogFile_ValidFile_ReturnsContentAsNdjson()
    {
        var logContent = """{"round":0,"text":"You awaken in darkness."}""" + "\n"
                       + """{"round":1,"text":"You see a faint light."}""" + "\n";

        SetupSession("file-session", ("narration.log", logContent));

        var response = await _client.GetAsync("/api/logs/current/narration.log");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/x-ndjson", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(logContent, body);
    }

    [Fact]
    public async Task GetLogFile_FileNotFound_Returns404()
    {
        SetupSessionWithEmptyFiles("sparse-session", "narration.log");

        var response = await _client.GetAsync("/api/logs/current/nonexistent.log");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await DeserializeResponse(response);
        Assert.Equal("File 'nonexistent.log' not found", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetLogFile_NoSession_Returns404()
    {
        var response = await _client.GetAsync("/api/logs/current/narration.log");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await DeserializeResponse(response);
        Assert.Equal("No active session", body.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("subdir/file.log")]
    public async Task GetLogFile_PathTraversalWithSlashes_BlockedByRouting(string maliciousFilename)
    {
        SetupSessionWithEmptyFiles("traversal-session", "narration.log");

        var response = await _client.GetAsync($"/api/logs/current/{maliciousFilename}");

        // Slashes in the filename cause routing to not match the {filename} parameter,
        // so these return 404 (no matching route) rather than 400
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLogFile_PathTraversalWithDots_Returns400()
    {
        SetupSessionWithEmptyFiles("traversal-session", "narration.log");

        // Filenames containing ".." are rejected by the endpoint's path traversal check
        var response = await _client.GetAsync("/api/logs/current/..narration.log");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private string SetupSession(string sessionId, params (string filename, string content)[] files)
    {
        var sessionPath = Path.Combine(_debugPath, sessionId);
        Directory.CreateDirectory(sessionPath);

        foreach (var (filename, content) in files)
            File.WriteAllText(Path.Combine(sessionPath, filename), content);

        File.WriteAllText(Path.Combine(_debugPath, "latest-session"), sessionPath);
        return sessionPath;
    }

    private string SetupSessionWithEmptyFiles(string sessionId, params string[] filenames)
    {
        return SetupSession(sessionId, filenames.Select(f => (f, "")).ToArray());
    }

    private static async Task<JsonElement> DeserializeResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
