using System.ComponentModel;
using System.Reflection;

namespace EchoesOfTheAbyss.Lib.Shared;

public static class EnumExtensions
{
	public static string GetDescription(this Enum enumValue)
	{
		var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
		var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>();
		return descriptionAttribute?.Description ?? enumValue.ToString();
	}
}