using System.ClientModel;
using EchoesOfTheAbyss.Lib.Configuration;
using EchoesOfTheAbyss.Lib.Enums;
using EchoesOfTheAbyss.Lib.Models;
using EchoesOfTheAbyss.Lib.UI;
using OpenAI;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Services;

public class GameOrchestrator
{
	private ChatClient _chatClient;
	private UiManager _uiManager;
	private ILogger _logger;
	private List<Message> _messages = new();
	private WorldContext _worldContext = new();
	private ImaginationPipeline _imaginationPipeline;
	private string _sessionId = string.Empty;

	public GameOrchestrator(LlmConfig config, ILogger? logger = null)
	{
		_chatClient = new ChatClient(
			config.Model,
			new ApiKeyCredential("aoeu"),
			new OpenAIClientOptions { Endpoint = new Uri($"{config.Host}:{config.Port}/v1") });

		_uiManager = new UiManager();
		_uiManager.ConversationEntered += ProcessTextInput;

		_imaginationPipeline = new ImaginationPipeline(_chatClient);
		
		_logger = logger ?? new SessionLogger();
	}
	
    public async Task RunAsync()
    {
        _sessionId = GenerateSessionId();
        await _logger.InitializeAsync(_sessionId);

		var narration = _chatClient.CompleteChatStreamingAsync(
			new SystemChatMessage(Prompts.Narrator),
			new SystemChatMessage(Prompts.Exposition));

		var waitingForInput = true;

		var round = 0;
        do
        {
			
			var newMessage = await ProcessThoughtsAsync(narration);
            var narrationLogEntry = new NarrationLogEntry
            {
                SessionId = _sessionId,
                Round = round,
                Thoughts = string.Join("\n", newMessage.Thoughts),
                Speech = newMessage.Speech
            };
            await _logger.LogNarrationAsync(narrationLogEntry);

			_worldContext = await _imaginationPipeline.RunAsync(newMessage.Speech, _worldContext);
			
            var worldStateLogEntry = WorldStateLogEntry.FromWorldContext(_worldContext, _sessionId, round);
            await _logger.LogWorldStateAsync(worldStateLogEntry);

			_messages.Add(newMessage);
			
			
			while (waitingForInput)
			{
				var uiLoopContext = _uiManager.RunUiLoop(_messages, _worldContext);
				
				waitingForInput = uiLoopContext.WaitingForInput;
			}
			
            round++;
        } while (round <= 30);

        await _logger.CloseSessionAsync();
    }

    private static string GenerateSessionId()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
    }

	private void ProcessTextInput(object sender, string text)
	{
		_messages.Add(new Message
		{
			Messager = MessagerEnum.User,
			IsExpandable = false,
			Speech = text
		});
	}

	private static async Task<Message> ProcessThoughtsAsync(AsyncCollectionResult<StreamingChatCompletionUpdate> narration)
	{
		var currentThought = string.Empty;
		var thoughts = new List<string>();
		var output = string.Empty;
		bool isThinking = false;
		
		await foreach (StreamingChatCompletionUpdate completionUpdate in narration)
		{
			if (completionUpdate.ContentUpdate.Count > 0)
			{
				var text = completionUpdate.ContentUpdate[0].Text;
				if (text.Contains("<think>"))
				{
					isThinking = true;
					continue;
				}

				if (text.Contains("</think>"))
				{
					isThinking = false;
					continue;
				}

				if (isThinking)
				{
					if (text.Contains("\n"))
					{
						if (currentThought.Length > 0)
						{
							thoughts.Add(currentThought + text.Replace("\n", ""));
							currentThought = string.Empty;
						}
					}
					else
					{
						currentThought += text;
					}
				}
				
				if (!isThinking)
				{
					output += text.Replace("\n", "").Replace("\"", "");
				}
			}
		}
		
		return new Message
		{
			Messager = MessagerEnum.Narrator,
			Thoughts = thoughts,
			Speech = output
		};
	}
}