namespace EchoesOfTheAbyss.Lib.Models;

public class WorldContext
{
	public int Difficulty { get; set; } = 50;

	public Player Player { get; set; } = new();
	
	public Equipment Equipment { get; set; } = new();
	
	public Location CurrentLocation { get; set; } = new();

	public override string ToString()
	{
		return $$"""
				 Difficulty: {{Difficulty}} (0=easy, 50=balanced, 100=hard).
				 {{CurrentLocation}}.
				 {{Player}}.
				 {{Equipment}}.
				 """;
	}
}