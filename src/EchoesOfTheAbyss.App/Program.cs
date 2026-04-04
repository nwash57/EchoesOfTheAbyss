using EchoesOfTheAbyss.Lib;
using EchoesOfTheAbyss.Lib.Configuration;
using EchoesOfTheAbyss.Lib.Models;
using EchoesOfTheAbyss.Lib.Services;

System.Console.OutputEncoding = System.Text.Encoding.UTF8;
	
var config = new LlmConfig(
    "http://localhost",
    1234,
    LlmModels.DeepSeekR1DistillQwen14B);

var gameOrchestrator = new GameOrchestrator(config);
await gameOrchestrator.RunAsync();