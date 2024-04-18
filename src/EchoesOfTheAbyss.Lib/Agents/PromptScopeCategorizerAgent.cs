// Copyright (c) Microsoft Corporation. All rights reserved.
// Example09_LMStudio_FunctionCall.cs

using System.Text.Json;
using AutoGen;
using AutoGen.Core;
using AutoGen.LiteLLM;

namespace EchoesOfTheAbyss.Lib.Agents;

public partial class PromptScopeCategorizerAgent
{
    public static readonly string ScopeList = JsonSerializer.Serialize(new[]
    {
        "Protagonist",
        "Environment",
        "Adversary",
        "Ally",
        "Acquaintance"
    });


    public static IAgent BuildInputCategorizerAgent(LiteLlmConfig litellmConfig)
    {
        var instance = new PromptScopeCategorizerAgent();
        var systemMessage = GetSystemMessage();

        var config = new ConversableAgentConfig
        {
            Temperature = 0,
            ConfigList = [litellmConfig],
            FunctionContracts = new[]
            {
                instance.CategorizeInputFunctionContract,
            },
        };

        return new AssistantAgent(
                name: "scopeCategorizer",
                systemMessage: systemMessage,
                llmConfig: config,
                functionMap: new Dictionary<string, Func<string, Task<string>>>
                {
                    { nameof(CategorizeInput), instance.CategorizeInputWrapper },
                });
            // .RegisterPrintMessage();
    }

    public static async Task RunAsync(LiteLlmConfig config, string initialPrompt)
    {
        var janAgent = BuildInputCategorizerAgent(config);

        var userProxyAgent = new UserProxyAgent(
            name: "user",
            humanInputMode: HumanInputMode.ALWAYS);

        await userProxyAgent.SendAsync(
            receiver: janAgent,
            initialPrompt);
    }

    public static string GetSystemMessage()
    {
        var instance = new PromptScopeCategorizerAgent();

        object[] functionList =
        [
            instance.CategorizeInputFunction.SerializeFunctionDefinition(),
        ];
        var functionListString =
            JsonSerializer.Serialize(functionList, new JsonSerializerOptions { WriteIndented = true });

        return @$"
Your job is to categorize input into the following scopes:

{ScopeList}

You have access to the following functions. Use them if required:

{functionListString}";
    }

    /// <summary>
    /// Assigns the scope of a prompt,
    /// </summary>
    /// <param name="scope">The scope a prompt is related to</param>
    /// <returns>The scope</returns>
    [Function]
    public async Task<string> CategorizeInput(string scope)
    {
        CurrentPrompt.Scope = scope;
        return $"Scope: {scope}";
    }
}

