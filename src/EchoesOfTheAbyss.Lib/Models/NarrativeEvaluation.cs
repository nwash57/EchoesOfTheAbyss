namespace EchoesOfTheAbyss.Lib.Models;

public class NarrativeEvaluation
{
    public bool UpdateLocation  { get; set; } = true;
    public bool UpdatePlayer    { get; set; } = true;
    public bool UpdateEquipment { get; set; } = true;
    public int  HealthDelta     { get; set; } = 0;

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
                    "description": "true if any equipment was gained, lost, dropped, consumed, or equipped/unequipped"
                },
                "healthDelta": {
                    "type": "integer",
                    "description": "Net signed health change this turn. 0 = no change. Must reflect severity: lethal neck wound = -40 to -60, serious stab = -20 to -35, moderate cut = -8 to -15, minor scrape = -1 to -5, healing = positive. Net of armor mitigation."
                }
            },
            "required": ["updateLocation", "updatePlayer", "updateEquipment", "healthDelta"]
        }
        """;
}
