using System.Text.Json;
using AutoGen.Core;
using AutoGen.LiteLLM;
using Json.More;

namespace EchoesOfTheAbyss.Lib.Agents;

public partial class ArchivalAgent
{
    public static readonly string QueryCategoryList = JsonSerializer.Serialize(new[]
    {
        "Protagonist",
        "Location",
        "Weather",
        "Quest",
        "Equipment",
        "Other"
    });

    public static async Task RunAsync(LiteLlmConfig config, string prompt)
    {
        var instance = new ArchivalAgent();

        // TODO: abstract this somehow
        var functionResponseExample = JsonSerializer.Serialize(new FunctionCall
        {
            Name = instance.FindKnownInfoFunction.Name,
            Arguments = new Dictionary<string, object>() { ["category"] = "Location"}.ToJsonDocument().RootElement
        });

        var systemMessage = GetSystemMessage();

        var janAgent = new LiteLlmAgent(
            "archivalist",
            config: config,
            systemMessage)
            .RegisterMiddleware(async (msgs, option, innerAgent, ct) =>
            {
                // inject few-shot example to the message
                var (exampleQuery, exampleAnswer) = instance.GetExampleLocationQueryAndResponse(innerAgent);
                msgs = new[] { exampleQuery, exampleAnswer }.Concat(msgs).ToArray();

                var reply = await innerAgent.GenerateReplyAsync(msgs, option, ct);

                // if reply is a function call, invoke function
                var content = reply.GetContent();
                try
                {
                    if (JsonSerializer.Deserialize<FunctionCall>(content) is { } functionCall)
                    {
                        var arguments = JsonSerializer.Serialize(functionCall.Arguments);
                        // invoke function wrapper
                        if (functionCall.Name == instance.FindKnownInfoFunction.Name)
                        {
                            var result = await instance.FindKnownInfoWrapper(arguments);
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
            });

    }

    // TODO: abstract this somehow
    public (IMessage exampleQuery, IMessage exampleAnswer) GetExampleLocationQueryAndResponse(IAgent innerAgent)
    {
        var functionResponseExample = JsonSerializer.Serialize(new FunctionCall
        {
            Name = FindKnownInfoFunction.Name,
            Arguments = new Dictionary<string, object> { ["category"] = "Equipment", ["scope"] = "Protagonist" }.ToJsonDocument().RootElement
        });
        var exampleQuery = new TextMessage(Role.User, "What is known about the protagonist's equipment?");
        var exampleAnswer = new TextMessage(Role.Assistant, functionResponseExample, from: innerAgent.Name);

        return (exampleQuery, exampleAnswer);
    }

    public static string GetSystemMessage()
    {
        var instance = new ArchivalAgent();

        object[] functionList =
        [
            instance.FindKnownInfoFunction.SerializeFunctionDefinition(),
        ];
        var functionListString =
            JsonSerializer.Serialize(functionList, new JsonSerializerOptions { WriteIndented = true });

        return @$"
You assist in the narration of a grand quest. 
Your job is to retrieve known information about the world and protagonist. 
 
You are limited to retrieving info from the following categories:
{QueryCategoryList}

You have access to the following functions. Use them if required:

{functionListString}";
    }

    [Function]
    public async Task<string> FindKnownInfo(string topic)
    {
        return "I should return known info about equipment";
    }
}