namespace EchoesOfTheAbyss.Lib.PlotArc;

public class PlotArc
{
    public string EstablishedNpcs { get; set; } = string.Empty;
    public string EstablishedLocations { get; set; } = string.Empty;
    public string EstablishedItems { get; set; } = string.Empty;
    public string Backstory { get; set; } = string.Empty;
    public List<string> PlotPoints { get; set; } = [];
    public int CurrentPlotPointIndex { get; set; } = 0;
    public string Climax { get; set; } = string.Empty;

    public string CurrentObjective =>
        CurrentPlotPointIndex < PlotPoints.Count
            ? PlotPoints[CurrentPlotPointIndex]
            : Climax;

    public string ToNarratorPrompt()
    {
        var nextPoints = PlotPoints.Skip(CurrentPlotPointIndex + 1).ToList();
        var upcoming = nextPoints.Count > 0
            ? $"\nUPCOMING: {string.Join(" -> ", nextPoints)} -> {Climax}"
            : $"\nFINAL: {Climax}";

        return $"""
            ESTABLISHED FACTS (you must respect these — never contradict them):
            NPCs: {EstablishedNpcs}
            Locations: {EstablishedLocations}
            Key Items: {EstablishedItems}
            Backstory: {Backstory}

            CURRENT OBJECTIVE: {CurrentObjective}{upcoming}

            Guide the player toward the current objective without railroading.
            Reference established facts consistently. Never contradict them.
            """;
    }

    public static string JsonSchema =>
        """
        {
            "type": "object",
            "properties": {
                "establishedNpcs": {
                    "type": "string",
                    "description": "2-3 key NPCs with name, role, and relationship to the player. Comma-separated. Example: 'Master Holloway (mentor, quest-giver), Sera (innkeeper, old friend)'"
                },
                "establishedLocations": {
                    "type": "string",
                    "description": "2-3 key locations the story involves. Comma-separated. Example: 'Thornfield (home village), The Ashen Wastes (dangerous frontier)'"
                },
                "establishedItems": {
                    "type": "string",
                    "description": "1-2 plot-significant items the player will encounter. Example: 'A tarnished silver compass, a sealed letter'"
                },
                "backstory": {
                    "type": "string",
                    "description": "The backstory premise in 1-2 sentences. What happened before the adventure begins."
                },
                "plotPoints": {
                    "type": "array",
                    "items": { "type": "string" },
                    "minItems": 4,
                    "maxItems": 6,
                    "description": "Ordered story beats forming a hero's journey. Each is one short sentence."
                },
                "climax": {
                    "type": "string",
                    "description": "The final objective in one sentence."
                }
            },
            "required": ["establishedNpcs", "establishedLocations", "establishedItems", "backstory", "plotPoints", "climax"]
        }
        """;
}
