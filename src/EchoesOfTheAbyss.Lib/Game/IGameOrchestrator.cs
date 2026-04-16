using EchoesOfTheAbyss.Lib.Shared;

namespace EchoesOfTheAbyss.Lib.Game;

public interface IGameOrchestrator
{
    Task RunAsync(CancellationToken ct = default);
    Task SetDifficulty(DifficultyLevel difficulty, CancellationToken ct = default);
    Task SetNarrationVerbosity(VerbosityLevel verbosity, CancellationToken ct = default);
    void ConfirmSetup(DifficultyLevel? difficulty = null, VerbosityLevel? verbosity = null);
    void EnqueuePlayerInput(string text);
}
