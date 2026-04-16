using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Llm;
using OpenAI.Chat;

namespace EchoesOfTheAbyss.Lib.Pipeline.Agents;

public class EquipmentExtractionAgent : INarrationPipelineAgent
{
    private readonly IEquipmentExtractor _extractor;

    public EquipmentExtractionAgent(IEquipmentExtractor extractor)
    {
        _extractor = extractor;
    }

    public int Order => 30;

    public async Task ExecuteAsync(NarrationPipelineContext context, CancellationToken ct = default)
    {
        var history = BuildHistory(context);
        context.NewEquipment = await _extractor.ExtractAsync(history, context.CurrentWorldContext, context.Evaluation!);
    }

    private static List<ChatMessage> BuildHistory(NarrationPipelineContext context) =>
    [
        new SystemChatMessage(Prompts.ImaginationSystem),
        new UserChatMessage($"""
            Latest narration: "{context.Narration}"
            Previous world state: {context.CurrentWorldContext}

            Extract the equipment.
            """)
    ];
}
