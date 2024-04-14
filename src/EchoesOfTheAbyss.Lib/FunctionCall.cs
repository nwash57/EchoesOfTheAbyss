using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoesOfTheAbyss.Lib;

public class FunctionCall
{
    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("arguments")] public JsonElement Arguments { get; set; }
}