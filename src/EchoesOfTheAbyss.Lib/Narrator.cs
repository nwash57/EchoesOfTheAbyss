using System.Text.Json;
using AutoGen;
using AutoGen.Core;
using AutoGen.LMStudio;
using EchoesOfTheAbyss.Lib;

namespace EchoesOfTheAbyssApp;

public partial class Narrator
{
    [Function]
    public async Task<string> Narrate(string narration)
    {
        return narration;
    }

public static async Task RunAsync(string initialPrompt)
    {
        // This example has been verified to work with Trelis-Llama-2-7b-chat-hf-function-calling-v3
        var instance = new Narrator();
        var config = new LMStudioConfig("localhost", 1234);
        var systemMessage = Prompts.Narrator;


        object[] functionList =
        [
            instance.NarrateFunction.SerializeFunctionDefinition(),
        ];
        var functionListString =
            JsonSerializer.Serialize(functionList, new JsonSerializerOptions { WriteIndented = true });
        var lmAgent = new LMStudioAgent(
                name: "assistant",
                systemMessage: systemMessage,
                config: config)
            .RegisterMiddleware(async (msgs, option, innerAgent, ct) =>
            {
                var reply = await innerAgent.GenerateReplyAsync(msgs, option, ct);

                // if reply is a function call, invoke function
                var content = reply.GetContent();
                try
                {
                    if (JsonSerializer.Deserialize<FunctionCall>(content) is { } functionCall)
                    {
                        var arguments = JsonSerializer.Serialize(functionCall.Arguments);
                        // invoke function wrapper
                        if (functionCall.Name == instance.NarrateFunction.Name)
                        {
                            var result = await instance.NarrateWrapper(arguments);
                            return new TextMessage(Role.Assistant, result);
                        }
                        else
                        {
                            throw new Exception($"Unknown function call: {functionCall.Name}");
                        }
                    }
                }
                catch (JsonException)
                {
                    // Console.WriteLine($"Exception deserializing response: \n\t{ex.Message}");
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