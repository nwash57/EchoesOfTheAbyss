using System.ComponentModel;
using EchoesOfTheAbyss.Lib.Shared;

namespace EchoesOfTheAbyss.Lib.Equipment;

public class Equipment : ISchemable
{
	public HeadEquipment Head { get; set; }
	public TorsoEquipment Torso { get; set; }
	public LegsEquipment Legs { get; set; }
	public FeetEquipment Feet { get; set; }
	public RightHandEquipment RightHand { get; set; }
	public LeftHandEquipment LeftHand { get; set; }
	
	public bool IsEmpty() =>
		string.IsNullOrEmpty(Head?.Name)
		&& string.IsNullOrEmpty(Torso?.Name)
		&& string.IsNullOrEmpty(Legs?.Name)
		&& string.IsNullOrEmpty(Feet?.Name)
		&& string.IsNullOrEmpty(RightHand?.Name)
		&& string.IsNullOrEmpty(LeftHand?.Name);

	public override string ToString()
	{
		return $$"""
				 {{Head}}
				 {{Torso}}
				 {{Legs}}
				 {{Feet}}
				 {{RightHand}}
				 {{LeftHand}}
				 """;
	}

	public static string JsonSchema =>
		$$"""
		{
			"type": "object",
			"properties": {
				"head": {{HeadEquipment.JsonSchema}},
				"torso": {{TorsoEquipment.JsonSchema}},
				"legs": {{LegsEquipment.JsonSchema}},
				"feet": {{FeetEquipment.JsonSchema}},
				"rightHand": {{RightHandEquipment.JsonSchema}},
				"leftHand": {{LeftHandEquipment.JsonSchema}}
			},
			"required": [
				"head",
				"torso",
				"legs",
				"feet",
				"rightHand",
				"leftHand"
			]
		}
		""";
}

public class HeadEquipment : EquipmentPiece
{
	public override EquipmentSlot Slot => EquipmentSlot.Head;
	public static string JsonSchema => BuildJsonSchema(EquipmentSlot.Head);
}

public class TorsoEquipment : EquipmentPiece
{
	public override EquipmentSlot Slot => EquipmentSlot.Torso;
	public static string JsonSchema => BuildJsonSchema(EquipmentSlot.Torso);
}

public class LegsEquipment : EquipmentPiece
{
	public override EquipmentSlot Slot => EquipmentSlot.Legs;
	public static string JsonSchema => BuildJsonSchema(EquipmentSlot.Legs);
}

public class FeetEquipment : EquipmentPiece
{
	public override EquipmentSlot Slot => EquipmentSlot.Feet;
	public static string JsonSchema => BuildJsonSchema(EquipmentSlot.Feet);
}

public class RightHandEquipment : EquipmentPiece
{
	public override EquipmentSlot Slot => EquipmentSlot.RightHand;
	public static string JsonSchema => BuildJsonSchema(EquipmentSlot.RightHand);
}

public class LeftHandEquipment : EquipmentPiece
{
	public override EquipmentSlot Slot => EquipmentSlot.LeftHand;
	public static string JsonSchema => BuildJsonSchema(EquipmentSlot.LeftHand);
}

public abstract class EquipmentPiece : ISchemable
{
	public abstract EquipmentSlot Slot { get; }
	public string Name { get; set; }
	public int Armor { get; set; }
	public int Damage { get; set; }
	public string Description { get; set; }

	public override string ToString()
	{
		return $$"""
				 {{(string.IsNullOrEmpty(Name) ? "" : $"{Slot.GetDescription()} Equipment: {Name} - {Armor} armor, {Damage} damage - {Description}")}}
				 """;
	}

	public static string BuildJsonSchema(EquipmentSlot slot) =>
		$$"""
		{
			"type": "object",
			"properties": {
				"slot": { 
					"const": "{{(int)slot}}",
					"description": "This equipment occupies the {{slot.GetDescription()}} slot"
				},
				"name": {
					"type": "string",
					"description": "A descriptive name for a {{slot.GetDescription()}} piece of equipment"
				},
				"armor": {
					"type": "integer",
					"minimum": 0,
					"maximum": 100,
					"description": "The amount of armor this piece of equipment provides"
				},
				"damage": {
					"type": "integer",
					"minimum": 0,
					"maximum": 100,
					"description": "The amount of damage this piece of equipment provides"
				},
				"description": {
					"type": "string",
					"description": "A very brief 1-2 short sentence description of the equipment piece"
				}
			},
			"description": "a piece of equipment that can be worn on the {{slot.GetDescription()}} slot",
			"required": [
				"slot",
				"name",
				"armor",
				"damage",
				"description"
			]
		}
		""";
}

public enum EquipmentSlot
{
	[Description("Head")] Head = 1,

	[Description("Torso")] Torso,

	[Description("Legs")] Legs,

	[Description("Feet")] Feet,

	[Description("Right Hand")] RightHand,

	[Description("Left Hand")] LeftHand,

	[Description("Right Ring")] RightRing,

	[Description("Left Ring")] LeftRing,

	[Description("Neck")] Neck,

	[Description("Back")] Back,

	[Description("Accessory")] Accessory
}