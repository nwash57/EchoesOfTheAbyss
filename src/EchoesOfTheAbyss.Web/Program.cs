using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using EchoesOfTheAbyss.Lib.Game;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Shared;
using EchoesOfTheAbyss.Web;

var llmConfig = new LlmConfig("http://localhost", 1234, LlmModels.Qwen3_5__9B__Q8);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule(new GameModule(llmConfig));
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseWebSockets();

// Log access API — resolves current session via debug/latest-session pointer file
string ResolveDebugPath(IConfiguration config)
{
    var debugPath = config["DebugPath"];
    if (string.IsNullOrEmpty(debugPath))
    {
        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName
                          ?? AppContext.BaseDirectory;
        debugPath = Path.Combine(projectRoot, "debug");
    }
    return debugPath;
}

app.MapGet("/api/logs/current", (IConfiguration config) =>
{
    var debugPath = ResolveDebugPath(config);
    var latestFile = Path.Combine(debugPath, "latest-session");
    if (!File.Exists(latestFile))
        return Results.NotFound(new { error = "No active session" });

    var sessionPath = File.ReadAllText(latestFile).Trim();
    if (!Directory.Exists(sessionPath))
        return Results.NotFound(new { error = "Session directory not found" });

    var sessionId = Path.GetFileName(sessionPath);
    var files = Directory.GetFiles(sessionPath).Select(Path.GetFileName).ToArray();

    return Results.Ok(new { sessionId, sessionPath, files });
});

app.MapGet("/api/logs/current/{filename}", (string filename, IConfiguration config) =>
{
    // Prevent path traversal
    if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\'))
        return Results.BadRequest(new { error = "Invalid filename" });

    var debugPath = ResolveDebugPath(config);
    var latestFile = Path.Combine(debugPath, "latest-session");
    if (!File.Exists(latestFile))
        return Results.NotFound(new { error = "No active session" });

    var sessionPath = File.ReadAllText(latestFile).Trim();
    var filePath = Path.Combine(sessionPath, filename);

    if (!File.Exists(filePath))
        return Results.NotFound(new { error = $"File '{filename}' not found" });

    var content = File.ReadAllText(filePath);
    return Results.Text(content, "application/x-ndjson");
});

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    using var cts = new CancellationTokenSource();

    var lifetimeScope = context.RequestServices.GetRequiredService<ILifetimeScope>();
    await using var connectionScope = lifetimeScope.BeginLifetimeScope();

    var orchestrator = connectionScope.Resolve<WebGameOrchestrator>();
    orchestrator.SetConnection(new WebSocketClientConnection(ws));

    var gameTask = orchestrator.RunAsync(cts.Token);

    var buffer = new byte[4096];
    try
    {
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, cts.Token);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var msg = JsonSerializer.Deserialize<WsClientMessage>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (msg?.Type == "player_input" && msg.Text is not null)
            {
                orchestrator.EnqueuePlayerInput(msg.Text);
            }
            else if (msg?.Type == "restart_game")
            {
                orchestrator.EnqueuePlayerInput("restart");
            }
            else if (msg?.Type == "set_difficulty" && msg.Difficulty is not null
                     && Enum.TryParse<DifficultyLevel>(msg.Difficulty, out var difficulty))
            {
                await orchestrator.SetDifficulty(difficulty, cts.Token);
            }
            else if (msg?.Type == "set_narration_verbosity" && msg.NarrationVerbosity is not null
                     && Enum.TryParse<VerbosityLevel>(msg.NarrationVerbosity, out var verbosity))
            {
                await orchestrator.SetNarrationVerbosity(verbosity, cts.Token);
            }
            else if (msg?.Type == "confirm_setup")
            {
                DifficultyLevel? diff = msg.Difficulty is not null && Enum.TryParse<DifficultyLevel>(msg.Difficulty, out var d) ? d : null;
                VerbosityLevel? verb = msg.NarrationVerbosity is not null && Enum.TryParse<VerbosityLevel>(msg.NarrationVerbosity, out var v) ? v : null;
                orchestrator.ConfirmSetup(diff, verb);
            }
        }
    }
    catch (OperationCanceledException) { }
    finally
    {
        cts.Cancel();
        try { await gameTask; } catch (OperationCanceledException) { }
    }
});

app.Run("http://localhost:5000");

record WsClientMessage(string Type, string? Text, string? Difficulty, string? NarrationVerbosity);

public partial class Program { }
