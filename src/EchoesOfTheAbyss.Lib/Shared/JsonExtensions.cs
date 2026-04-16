using System.Text.Json;

namespace EchoesOfTheAbyss.Lib.Shared;

public static class JsonExtensions
{
	public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		AllowTrailingCommas = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};
    
	public static string ToJson<T>(this T obj)
	{
		return JsonSerializer.Serialize(obj, DefaultOptions);
	}
    
	public static T FromJson<T>(this string json)
	{
		return JsonSerializer.Deserialize<T>(json, DefaultOptions) ?? throw new JsonException("Deserialization failed.");
	}

	
}
