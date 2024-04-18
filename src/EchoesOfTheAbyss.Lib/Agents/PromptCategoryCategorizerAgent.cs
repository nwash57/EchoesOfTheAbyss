// Copyright (c) Microsoft Corporation. All rights reserved.
// Example09_LMStudio_FunctionCall.cs

using System.Text.Json;
using AutoGen;
using AutoGen.Core;
using AutoGen.LiteLLM;

namespace EchoesOfTheAbyss.Lib.Agents;

public partial class PromptCategoryCategorizerAgent
{
    public static readonly string PromptCategoryList = JsonSerializer.Serialize(new []
    {
        "Location",
        "Weather",
        "Quest",
        "Equipment",
        "Other",
    });
    //
    // public static readonly string ScopeList = JsonSerializer.Serialize(new[]
    // {
    //     "Protagonist",
    //     "Environment",
    //     "Adversary",
    //     "Ally",
    //     "Acquaintance"
    // });


    public static IAgent BuildInputCategorizerAgent(LiteLlmConfig litellmConfig)
    {
        var instance = new PromptCategoryCategorizerAgent();
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
            name: "categoryCategorizer",
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
        var instance = new PromptCategoryCategorizerAgent();

        object[] functionList =
        [
            instance.CategorizeInputFunction.SerializeFunctionDefinition(),
        ];
        var functionListString =
            JsonSerializer.Serialize(functionList, new JsonSerializerOptions { WriteIndented = true });

        return @$"
Your job is to categorize input into the following categories:

{PromptCategoryList}

You have access to the following functions. Use them if required:

{functionListString}";
    }

    /// <summary>
    /// Assigns a category of game element to the prompt,
    /// </summary>
    /// <param name="category">Game element the prompt is about</param>
    /// <returns>The category</returns>
    [Function]
    public async Task<string> CategorizeInput(string category)
    {
        CurrentPrompt.Category = category;
        return $"Category: {category}";
    }
}

