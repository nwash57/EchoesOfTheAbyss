// See https://aka.ms/new-console-template for more information

using AutoGen.LiteLLM;
using EchoesOfTheAbyss.Lib;

// await PromptTypeCategorizerAgent.RunAsync("Where am I?");

var config = new LiteLlmConfig(
    "localhost",
    4000,
    Models.NaturalFunctions);
await GameOrchestrator.RunAsync(config);