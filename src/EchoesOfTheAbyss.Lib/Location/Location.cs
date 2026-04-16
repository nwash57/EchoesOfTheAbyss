using System.Text.RegularExpressions;
using EchoesOfTheAbyss.Lib.Shared;

namespace EchoesOfTheAbyss.Lib.Location;

public partial class Location : ISchemable
{
	[GeneratedRegex(@"^\s*\(?\s*-?\d+\s*,\s*-?\d+\s*\)?\s*[-–—]\s*")]
	private static partial Regex CoordinatePrefixRegex();

	public Coordinates Coordinates { get; set; } = new(0,0);

	private string _title = string.Empty;
	public string Title
	{
		get => _title;
		set => _title = CoordinatePrefixRegex().Replace(value, "");
	}
	
    public string ShortDescription { get; set; } = string.Empty;
	
	public string LongDescription { get; set; } = string.Empty;

	public string Type { get; set; } = "default";
	
	#region IExpandableSection
	public string Header => $"Location: {Coordinates} - {Title}";
	public List<string> Details => LongDescription.Split('\n').ToList();
	public bool IsExpandable => true;
	public bool IsExpanded { get; set; }
	#endregion

	public override string ToString()
	{
		return string.IsNullOrEmpty($"{Title}{ShortDescription}{LongDescription}")
			? ""
			: $"{Coordinates} - {Title} - {ShortDescription} - {LongDescription}";
	}

	public static string JsonSchema =>
		"""
		{
			"type": "object",
			"properties": {
				"coordinates": {
					"type": "object",
					"properties": {
						"x": {
							"type": "integer",
							"description": "The east/west coordinate of the location"
						},
						"y": {
							"type": "integer",
							"description": "The north/south coordinate of the location"
						}
					},
					"required": [
						"x",
						"y"
					]
				},
				"title": {
					"type": "string",
					"description": "A short, catchy name for the location (e.g., 'The Whispering Woods')"
				},
				"shortDescription": {
					"type": "string",
					"description": "A very brief, 1-4 word objective description of the location"
				},
				"longDescription": {
					"type": "string",
					"description": "A more detailed, 1-2 sentence description of the location"
				},
				"type": {
					"type": "string",
					"enum": ["landmark", "notable", "default"],
					"description": "The classification of the location. landmark: major points of interest. notable: minor points of interest. default: regular locations."
				}
			},
			"required": [
				"coordinates",
				"title",
				"shortDescription",
				"longDescription",
				"type"
			]
		}
		""";

	
}