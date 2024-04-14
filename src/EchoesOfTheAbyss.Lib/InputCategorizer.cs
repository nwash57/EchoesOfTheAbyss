// Copyright (c) Microsoft Corporation. All rights reserved.
// Example09_LMStudio_FunctionCall.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using AutoGen;
using AutoGen.Core;
using AutoGen.LMStudio;
using Azure.AI.OpenAI;
using Json.More;

namespace EchoesOfTheAbyss.Lib;

public partial class InputCategorizer
{
    [Function]
    public async Task<string> CategorizeInput(string type, string category)
    {
        return $"Type: {type}  |  Category: {category}";
    }

    public static async Task RunAsync(string initialPrompt)
    {
        // This example has been verified to work with Trelis-Llama-2-7b-chat-hf-function-calling-v3
        var instance = new InputCategorizer();
        var config = new LMStudioConfig("localhost", 1234);
        var systemMessage = @$"You are a helpful AI assistant.";

        var promptTypeListString = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["Query"] = "Any question the protagonist asks, if it ends with a question mark it's a query",
            ["Action"] = "Any input from the protagonist that invokes an action from their perspective",
            ["Chat"] = "Any idle chatter with the narrator",
        });

        var categoryListString = JsonSerializer.Serialize(new[]
        {
            "Protagonist",
            "Location",
            "Weather",
            "Quest",
            "Equipment",
            "Other"
        });

        var functionResponseExample = JsonSerializer.Serialize(new FunctionCall
        {
            Name = instance.CategorizeInputFunction.Name,
            Arguments = new Dictionary<string, object>() { ["type"] = "Query", ["category"] = "Location"}.ToJsonDocument().RootElement
        });

        object[] functionList =
        [
            instance.CategorizeInputFunction.SerializeFunctionDefinition(),
        ];
        var functionListString =
            JsonSerializer.Serialize(functionList, new JsonSerializerOptions { WriteIndented = true });
        var lmAgent = new LMStudioAgent(
                name: "assistant",
                systemMessage: @$"
You assist in the narration of a grand quest. 
Your job is to categorize input from the protagonist into type and category. 
You are limited to the following types:

{promptTypeListString}
 
You are limited to the following categories:
{categoryListString}

You have access to the following functions. Use them if required:

{functionListString}",
                config: config)
            .RegisterMiddleware(async (msgs, option, innerAgent, ct) =>
            {
                // inject few-shot example to the message
                var exampleGetWeather = new TextMessage(Role.User, "Where am I?");
                var exampleAnswer = new TextMessage(Role.Assistant, functionResponseExample, from: innerAgent.Name);
                msgs = new[] { exampleGetWeather, exampleAnswer }.Concat(msgs).ToArray();

                var reply = await innerAgent.GenerateReplyAsync(msgs, option, ct);

                // if reply is a function call, invoke function
                var content = reply.GetContent();
                try
                {
                    if (JsonSerializer.Deserialize<FunctionCall>(content) is { } functionCall)
                    {
                        var arguments = JsonSerializer.Serialize(functionCall.Arguments);
                        // invoke function wrapper
                        if (functionCall.Name == instance.CategorizeInputFunction.Name)
                        {
                            var result = await instance.CategorizeInputWrapper(arguments);
                            return new TextMessage(Role.Assistant, result);
                        }
                        else
                        {
                            throw new Exception($"Unknown function call: {functionCall.Name}");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Exception deserializing response: \n\t{ex.Message}");
                    // ignore
                }

                return reply;
            })
            .RegisterPrintMessage();

        var userProxyAgent = new UserProxyAgent(
            name: "user",
            humanInputMode: HumanInputMode.ALWAYS);

        await userProxyAgent.SendAsync(
            receiver: lmAgent,
            initialPrompt);
    }
}

