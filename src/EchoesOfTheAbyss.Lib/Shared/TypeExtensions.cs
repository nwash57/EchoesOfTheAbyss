using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Collections.Generic;

namespace EchoesOfTheAbyss.Lib.Shared;

public static class TypeExtensions
{
    public static string GetJsonSchema(this Type type)
    {
        var properties = type.GetProperties();

        Dictionary<string, object> schemaDict = new Dictionary<string, object>();

        foreach (var prop in properties)
        {
            schemaDict[prop.Name] = GetJsonSchemaForType(prop.PropertyType);
        }

        return JsonSerializer.Serialize(schemaDict);
    }

    public static string GetJsonSchemaForType(this Type type)
    {
        if (type == typeof(string))
            return "string";
        else if (type == typeof(int))
            return "integer";
        else if (type == typeof(bool))
            return "boolean";
        // Add more types as needed...

        throw new ArgumentException("Unsupported type");
    }
}