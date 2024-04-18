using System.Text.Json;
using AutoGen;
using AutoGen.Core;
using AutoGen.LiteLLM;
using Azure.AI.OpenAI;
using EchoesOfTheAbyss.Lib.Agents;
using Json.More;

namespace EchoesOfTheAbyss.Lib;

public static class CurrentPrompt
{
    public static string Type { get; set; }
    public static string Category { get; set; }
    public static string Scope { get; set; }
}

public static class GameState
{
}

public class GameOrchestrator
{
    public static async Task RunAsync(LiteLlmConfig config)
    {
        var userProxyAgent = new UserProxyAgent(
            name: "user",
            humanInputMode: HumanInputMode.ALWAYS);

        // var narratorAgent = NarratorAgent.BuildNarratorAgent(config);
        // var typeCategorizerAgent = PromptTypeCategorizerAgent.BuildInputCategorizerAgent(config);
        // var categoryCategorizerAgent = Agents.PromptCategoryCategorizerAgent.BuildInputCategorizerAgent(config);
        // var scopeCategorizerAgent = PromptScopeCategorizerAgent.BuildInputCategorizerAgent(config);

        #region groupchat method

        // var narratorToUserTransition = Transition.Create(narratorAgent, userProxyAgent);
        // var userToTypeCategorizer = Transition.Create(userProxyAgent, typeCategorizerAgent);
        // var typeToCategory = Transition.Create(typeCategorizerAgent, categoryCategorizerAgent);
        // var categoryToScope = Transition.Create(categoryCategorizerAgent, scopeCategorizerAgent);
        //
        // var fallthroughTransition = Transition.Create(scopeCategorizerAgent, userProxyAgent);
        //
        // var workflow = new Graph(
        // [
        //     narratorToUserTransition,
        //     userToTypeCategorizer,
        //     typeToCategory,
        //     categoryToScope,
        //     fallthroughTransition
        // ]);
        //
        // var groupChat = new GroupChat(
        //     members:
        //     [
        //         narratorAgent, userProxyAgent, typeCategorizerAgent, categoryCategorizerAgent, scopeCategorizerAgent
        //     ],
        //     workflow: workflow);
        //
        // var groupChatManager = new GroupChatManager(groupChat);
        //
        // var initialPrompt = "Write a very brief exposition to start the protagonist on their path. " +
        //                     "A few sentences without exposing much about the game world, you will expand knowledge of the world as it is relevant.";
        // var initialMessage = await narratorAgent.SendAsync(initialPrompt);
        //
        // var chatHistory = await userProxyAgent.SendAsync(
        //     groupChatManager, [initialMessage], maxRound: 30);
        //
        // var lastMessage = chatHistory.Last();
        // Console.WriteLine(lastMessage.GetContent());

        #endregion

        // List<IMessage> chatHistory = new();
        IMessage userInput = null;

        var previousNarrations = new List<string>();

        var lastUserMessage = string.Empty;
        var narrationPrompt = $@" 
Imagine new information that sheds light on a grand adventure and narrate for the player: 
You are not limited to information given in the context, imagined information will be added for consistency.
Do not repeat yourself.
Context: {{
Location: {{'description': 'dense forest'}}
Equipment: {{ 'torso': 'ratty tunic' }}
LastAction: {{ 'description': 'how did I get here?' }}
}}
";

        var narratorAgent = NarratorAgent.BuildNarratorAgent(config);
        var narration = await narratorAgent.SendAsync(new TextMessage(Role.System, narrationPrompt));


        var round = 0;
        do
        {
            previousNarrations.Add(narration.GetContent());
            var userMessage = await userProxyAgent.SendAsync();
            // chatHistory.Add(userMessage);
            lastUserMessage = userMessage.GetContent();


            var typeCategorizerAgent = PromptTypeCategorizerAgent.BuildInputCategorizerAgent(config);
            var categoryCategorizerAgent = Agents.PromptCategoryCategorizerAgent.BuildInputCategorizerAgent(config);
            var scopeCategorizerAgent = PromptScopeCategorizerAgent.BuildInputCategorizerAgent(config);

            await typeCategorizerAgent.GenerateReplyAsync([userMessage]);
            await categoryCategorizerAgent.GenerateReplyAsync([userMessage]);
            await scopeCategorizerAgent.GenerateReplyAsync([userMessage]);

            Console.WriteLine(
                $"Type: {CurrentPrompt.Type}  |  Category: {CurrentPrompt.Category}  |  Scope: {CurrentPrompt.Scope}");

            narratorAgent = NarratorAgent.BuildNarratorAgent(config);
            narrationPrompt = $@" 
Imagine new information that sheds light on a grand adventure and narrate for the player: 
You are not limited to information given in the context, imagined information will be added for consistency.

Do not repeat any of these previous narrations: 
{JsonSerializer.Serialize(previousNarrations)}

The current context is: {{
        Location: {{'description': 'dense forest'}}
        Equipment: {{ 'torso': 'ratty tunic' }}
        LastAction: {{ 'description': 'how did I get here?' }}
    }}

The message to respond to is: '{lastUserMessage}'
";
            narration = await narratorAgent.SendAsync(new TextMessage(Role.System, narrationPrompt));

        } while (round++ < 30);
    }
}