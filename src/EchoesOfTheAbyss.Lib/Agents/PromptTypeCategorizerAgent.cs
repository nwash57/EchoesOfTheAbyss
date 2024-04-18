// Copyright (c) Microsoft Corporation. All rights reserved.
// Example09_LMStudio_FunctionCall.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using AutoGen;
using AutoGen.Core;
using AutoGen.LiteLLM;
using AutoGen.LMStudio;
using Azure.AI.OpenAI;
using EchoesOfTheAbyss.Lib.Agents;
using Json.More;

namespace EchoesOfTheAbyss.Lib;

public partial class PromptTypeCategorizerAgent
{
    public static readonly string PromptTypeList = JsonSerializer.Serialize(new[]
    {
        "Query",
        "Action",
        "Chat",
    });



    public static IAgent BuildInputCategorizerAgent(LiteLlmConfig litellmConfig)
    {
        var instance = new PromptTypeCategorizerAgent();
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
                name: "typeCategorizer",
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
        var instance = new PromptTypeCategorizerAgent();

        object[] functionList =
        [
            instance.CategorizeInputFunction.SerializeFunctionDefinition(),
        ];
        var functionListString =
            JsonSerializer.Serialize(functionList, new JsonSerializerOptions { WriteIndented = true });

        return @$"
You assist in the narration of a grand quest. 
Your job is to categorize input from the protagonist into type, category, and scope. 
You are limited to the following types:

{PromptTypeList}

You have access to the following functions. Use them if required:

{functionListString}";
    }

    /// <summary>
    /// Assigns a type to the input
    /// </summary>
    /// <param name="type">The type of input, whether it is a question, action, or chatter</param>
    /// <returns>The prompt type</returns>
    [Function]
    public async Task<string> CategorizeInput(string type)
    {
        CurrentPrompt.Type = type;
        return $"Type: {type}";
    }
}