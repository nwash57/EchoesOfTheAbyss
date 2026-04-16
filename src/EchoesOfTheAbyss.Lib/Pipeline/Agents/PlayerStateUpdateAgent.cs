using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Player;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Pipeline.Agents;

public class PlayerStateUpdateAgent : INarrationPipelineAgent
{
    private readonly IPlayerStateUpdater _updater;

    public PlayerStateUpdateAgent(IPlayerStateUpdater updater)
    {
        _updater = updater;
    }

    public int Order => 30;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken ct = default)
    {
        var history = BuildHistory(context);
        context.NewPlayer = await _updater.UpdateAsync(history, context.CurrentWorldContext, context.Evaluation!);
    }

    private static List<ChatMessage> BuildHistory(NarrationPipelineContext context) =>
    [
        new SystemChatMessage(Prompts.ImaginationSystem),
        new UserChatMessage($"""
            Latest narration: "{context.Narration}"
            Previous world state: {context.CurrentWorldContext}

            Extract the player state.
            """)
    ];
}
