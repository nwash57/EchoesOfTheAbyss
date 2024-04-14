using Azure.AI.OpenAI;

namespace EchoesOfTheAbyss.Lib;

public static class FunctionDefinitionExtensions
{
    public static object SerializeFunctionDefinition(this FunctionDefinition functionDefinition)
    {
        return new
        {
            type = "function",
            function = new
            {
                name = functionDefinition.Name,
                description = functionDefinition.Description,
                parameters = functionDefinition.Parameters.ToObjectFromJson<object>(),
            }
        };
    }
}