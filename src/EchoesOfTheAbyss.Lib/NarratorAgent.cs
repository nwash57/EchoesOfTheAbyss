using System.Text.Json;
using AutoGen;
using AutoGen.Core;
using AutoGen.LiteLLM;
using AutoGen.OpenAI;

namespace EchoesOfTheAbyss.Lib;

public partial class NarratorAgent
{
    /// <summary>
    /// Imagines narration for a grand quest
    /// </summary>
    /// <param name="narration">A brief narration of the next part of the story</param>
    /// <returns>the narration</returns>
    [Function]
    public async Task<string> Narrate(string narration)
    {
        return narration;
    }

    public static IAgent BuildNarratorAgent(
        LiteLlmConfig litellmConfig,
        string? message = null)
    {
        var instance = new NarratorAgent();

        var systemMessage = message ?? Prompts.Narrator;

        var config = new ConversableAgentConfig
        {
            Temperature = 0.0F,
            ConfigList = [litellmConfig],
            FunctionContracts = new[]
            {
                instance.NarrateFunctionContract,
            },
        };

        return new AssistantAgent(
                name: "narrator",
                systemMessage: systemMessage,
                llmConfig: config,
                functionMap: new Dictionary<string, Func<string, Task<string>>>
                {
                    { nameof(Narrate), instance.NarrateWrapper },
                })
            .RegisterPrintMessage();
    }

    public static async Task RunAsync(LiteLlmConfig config, string initialPrompt)
    {
        var narratorAgent = BuildNarratorAgent(config);

        var userProxyAgent = new UserProxyAgent(
            name: "user",
            humanInputMode: HumanInputMode.ALWAYS);

        await userProxyAgent.SendAsync(
            receiver: narratorAgent,
            initialPrompt);
    }
}