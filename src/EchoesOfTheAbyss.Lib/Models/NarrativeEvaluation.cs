namespace EchoesOfTheAbyss.Lib.Models;

public class NarrativeEvaluation
{
    public bool   UpdateLocation  { get; set; } = true;
    public bool   UpdatePlayer    { get; set; } = true;
    public bool   UpdateEquipment { get; set; } = true;
    public int    HealthDelta     { get; set; } = 0;
    public string LogEntry        { get; set; } = "";

    public static string JsonSchema =>
        """
        {
            "type": "object",
            "properties": {
                "updateLocation": {
                    "type": "boolean",
                    "description": "true if the player moved to a new area or the current location materially changed"
                },
                "updatePlayer": {
                    "type": "boolean",
                    "description": "true if any player stat or demographic changed (health, armor, strength, revealed name/occupation)"
                },
                "updateEquipment": {
                    "type": "boolean",
                    "description": "true ONLY if items were EXPLICITLY taken, equipped, dropped, consumed, or destroyed. Searching or seeing an item is NOT an update."
                },
                "healthDelta": {
                    "type": "integer",
                    "description": "Net signed health change this turn. 0 = no change. Must reflect severity: lethal neck wound = -40 to -60, serious stab = -20 to -35, moderate cut = -8 to -15, minor scrape = -1 to -5, healing = positive. Net of armor mitigation."
                },
                "logEntry": {
                    "type": "string",
                    "description": "A brief past-tense chronicle entry (one sentence, 15 words or fewer) of the most significant event this turn. Describe completed actions and consequences (e.g. 'Slew a goblin in the dark forest', 'Arrived at the ruined watchtower'). Empty string if nothing notable occurred."
                }
            },
            "required": ["updateLocation", "updatePlayer", "updateEquipment", "healthDelta", "logEntry"]
        }
        """;
}
