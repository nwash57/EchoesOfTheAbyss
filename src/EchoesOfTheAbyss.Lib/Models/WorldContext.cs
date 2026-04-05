using EchoesOfTheAbyss.Lib.Enums;

namespace EchoesOfTheAbyss.Lib.Models;

public class WorldContext
{
	public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Balanced;
	public VerbosityLevel NarrationVerbosity { get; set; } = VerbosityLevel.Balanced;

	public Player Player { get; set; } = new();

	public Equipment Equipment { get; set; } = new();

	public Location CurrentLocation { get; set; } = new();

	public List<string> AdventureLog { get; set; } = [];

	public override string ToString()
	{
		var recentLog = AdventureLog.Count > 0
			? $"\nRecent events:\n{string.Join("\n", AdventureLog.TakeLast(8).Select((e, i) => $"  {AdventureLog.Count - Math.Min(8, AdventureLog.Count) + i + 1}. {e}"))}"
			: "";

		return $$"""
				 Difficulty: {{DifficultyGuidance(Difficulty)}}
				 Narration Verbosity: {{VerbosityGuidance(NarrationVerbosity)}}
				 {{CurrentLocation}}.
				 {{Player}}.
				 {{Equipment}}.{{recentLog}}
				 """;
	}

	private static string DifficultyGuidance(DifficultyLevel level) => level switch
	{
		DifficultyLevel.ExtremelyEasy => "Extremely Easy — The world is exceptionally forgiving; things almost always go the player's way, failures are rare and mild.",
		DifficultyLevel.Easy          => "Easy — The world is kind; player attempts usually succeed with only minor complications.",
		DifficultyLevel.Balanced      => "Balanced — Successes and failures are equally likely; skill and luck both matter.",
		DifficultyLevel.Hard          => "Hard — The world is unforgiving; player attempts often fail and success requires cleverness.",
		DifficultyLevel.ExtremelyHard => "Extremely Hard — The world is brutal; things rarely go the player's way and survival demands extraordinary perseverance.",
		_                             => level.ToString()
	};

	private static string VerbosityGuidance(VerbosityLevel level) => level switch
	{
		VerbosityLevel.ExtremelyConcise => "Extremely Concise — Write exactly 1 short sentence per response.",
		VerbosityLevel.Concise          => "Concise — Write 1–2 short sentences per response with minimal detail.",
		VerbosityLevel.Balanced         => "Balanced — Write 2–3 sentences per response with moderate detail.",
		VerbosityLevel.Verbose          => "Verbose — Write 3–4 sentences per response with rich sensory detail.",
		VerbosityLevel.ExtremelyVerbose => "Extremely Verbose — Write 5+ sentences per response with lavish, immersive description.",
		_                               => level.ToString()
	};
}
