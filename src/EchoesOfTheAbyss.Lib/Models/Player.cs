using EchoesOfTheAbyss.Lib.Interfaces;

namespace EchoesOfTheAbyss.Lib.Models;

public class Player : ISchemable
{
	public ActorDemographics Demographics { get; set; } = new();
	public PlayerStats Stats { get; set; } = new();


	public string Header => $"Player: {Demographics.FirstName} {Demographics.LastName}";
	public List<string> Details  => new () { $"Age: {Demographics.Age} years", $"Occupation: {Demographics.Occupation}", $"Health: {Stats.CurrentHealth}/{Stats.MaxHealth}", $"Armor: {Stats.BaseArmor}", $"Strength: {Stats.BaseStrength}" };
	public bool IsExpandable => true;
	public bool IsExpanded { get; set; }

	public override string ToString()
	{
		return $$"""
				 ({{(string.IsNullOrEmpty(Demographics.FirstName + Demographics.LastName) ? "" : $"(Player Name: {Demographics.FirstName} {Demographics.LastName})")}}.
				 {{(Demographics.Age > 0 ? $"(Player Age: {Demographics.Age} years old)" : "")}}.
				 {{(string.IsNullOrEmpty(Demographics.Occupation) ? "" : $"(Player Occupation: {Demographics.Occupation})")}}.
				 (Player Health: {{Stats.CurrentHealth}}/{{Stats.MaxHealth}}).
				 (Player Base Armor: {{Stats.BaseArmor}}).
				 (Player Base Strength: {{Stats.BaseStrength}}))
				 """;
	}

	public static string JsonSchema =>
		$$"""
		{
			"type": "object",
			"properties": {
				"demographics": {{ActorDemographics.JsonSchema}},
				"stats": {{PlayerStats.JsonSchema}}
			},
			"required": [
				"demographics",
				"stats"
			]
		}
		""";
}

public class PlayerStats : ISchemable
{
	public int CurrentHealth { get; set; } = 100;
	public int MaxHealth { get; set; } = 100;
	public int BaseArmor { get; set; } = 0;
	public int BaseStrength { get; set; } = 0;

	public static string JsonSchema =>
		"""
		{
			"type": "object",
			"properties": {
				"currentHealth": {
					"type": "integer",
					"minimum": 0,
					"maximum": 100,
					"description": "The current health of the player"
				},
				"maxHealth": {
					"type": "integer",
					"const": 100,
					"description": "The maximum health of the player"
				},
				"baseArmor": {
					"type": "integer",
					"minimum": 0,
					"maximum": 100,
					"description": "The base armor of the player"
				},
				"baseStrength": {
					"type": "integer",
					"minimum": 0,
					"maximum": 100,
					"description": "The base strength of the player"
				}
			},
			"required": [
				"currentHealth",
				"maxHealth",
				"baseArmor",
				"baseStrength"
			]
		}
		""";
}

public class ActorDemographics : ISchemable
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public int Age { get; set; } = 0;
	public string Occupation { get; set; } = string.Empty;

	public static string JsonSchema =>
		"""
		{
			"type": "object",
			"properties": {
				"firstName": {
					"type": "string",
					"description": "The first name of the actor, something inspired by role-playing games"
				},
				"lastName": {
					"type": "string",
					"description": "The last name of the actor, something inspired by role-playing games"
				},
				"age": {
					"type": "integer",
					"description": "The age of the actor"
				},
				"occupation": {
					"type": "string",
					"description": "The occupation of the actor, something inspired by role-playing games"
				}
			},
			"required": [
				"firstName",
				"lastName",
				"age",
				"occupation"
			]
		}
		""";
}