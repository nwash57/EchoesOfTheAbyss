using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using EchoesOfTheAbyss.Lib.Configuration;
using EchoesOfTheAbyss.Lib.Enums;
using EchoesOfTheAbyss.Lib.Models;
using EchoesOfTheAbyss.Lib.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    using var cts = new CancellationTokenSource();

    async Task Send(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    var config = new LlmConfig("http://localhost", 1234, LlmModels.Qwen3_5__9B__Q8);
    var orchestrator = new WebGameOrchestrator(config, Send);

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
                await orchestrator.SetDifficulty(difficulty);
            }
            else if (msg?.Type == "set_narration_verbosity" && msg.NarrationVerbosity is not null
                     && Enum.TryParse<VerbosityLevel>(msg.NarrationVerbosity, out var verbosity))
            {
                await orchestrator.SetNarrationVerbosity(verbosity);
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
