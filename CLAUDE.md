# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Echoes of the Abyss is a text adventure game powered by a local LLM. The LLM acts as narrator, responding to player input and imagining structured world state (player demographics, location, equipment) via JSON schema-constrained completions.

## Commands

```bash
# Build
dotnet build

# Run
dotnet run --project src/EchoesOfTheAbyss.App/EchoesOfTheAbyss.App.csproj

# Test
dotnet test SpectreConsoleTests/
```

The app requires a local LLM server running at `http://localhost:1234/v1` (LM Studio or compatible OpenAI-API server). The default model is `deepseek-r1-distill-qwen-14b`, configured in `src/EchoesOfTheAbyss.Lib/Llm/LlmModels.cs`.

## Architecture

### Core Flow

```
Program.cs → GameOrchestrator.RunAsync()
  ├─ LLM narrates in streaming mode (extracts <think>...</think> reasoning tags)
  ├─ After each narrator response, three "imagination" services query the LLM
  │   with JSON schema constraints to update world state:
  │   ├─ PlayerService     → Player (demographics)
  │   ├─ LocationService   → Location (coordinates + descriptions)
  │   └─ EquipmentService  → Equipment (6 body slots)
  └─ UiManager renders the terminal UI and captures player input
```

### Imagination Context Pattern

All three world-state services share the same pattern:
1. Accept an `ImaginationContext` (a slice of recent messages + current `WorldContext`)
2. Build `ChatCompletionOptions` with a strict JSON Schema `ResponseFormat`
3. Call the LLM, parse the JSON response into a strongly-typed model
4. Return the updated model

The LLM is asked to *imagine* consistent values based on narrative context—it doesn't invent new world state arbitrarily, it infers what was implied by the story so far.

### UI System

Two-panel Spectre.Console layout (60/40 split):
- **Left** (`ConversationUiPanel`): scrollable message history with expandable `<think>` blocks
- **Right** (`DetailsUiPanel`): expandable sections for Player / Location / Equipment

Keyboard: `↑`/`↓` scroll, `←`/`→` expand/collapse, `Tab` switch panels, `Enter` submit input.

### Project Structure (EchoesOfTheAbyss.Lib)

Domain-based folder organization:

| Domain | Contents |
|---|---|
| `Player/` | Player model, PlayerStateUpdater |
| `Equipment/` | Equipment model, EquipmentExtractor |
| `Location/` | Location/Coordinates models, LocationExtractor |
| `Narrative/` | Message, NarrativeEvaluation, NarrativeEvaluator, MessagerEnum |
| `Imagination/` | WorldContext, ImaginationPipeline |
| `Game/` | WebGameOrchestrator, IClientConnection |
| `Llm/` | OpenAiChatService, LlmConfig, LlmModels, Prompts |
| `Logging/` | SessionLogger, LogEntry models |
| `Shared/` | DifficultyLevel, VerbosityLevel, ISchemable, extensions |

### Key Files

| Purpose | Path |
|---|---|
| Web entry point | `src/EchoesOfTheAbyss.Web/Program.cs` |
| Main game loop | `src/EchoesOfTheAbyss.Lib/Game/WebGameOrchestrator.cs` |
| World state model | `src/EchoesOfTheAbyss.Lib/Imagination/WorldContext.cs` |
| LLM system prompts | `src/EchoesOfTheAbyss.Lib/Llm/Prompts.cs` |
| Available models | `src/EchoesOfTheAbyss.Lib/Llm/LlmModels.cs` |

### Build Notes

- Target: .NET 10.0, C# preview language features
- `TreatWarningsAsErrors: true` — all warnings must be resolved
- Centralized package versions in `Directory.Packages.props`
- `AutoGen.SourceGenerator` is a Roslyn incremental source generator (netstandard2.0) that turns `[Function]`-attributed partial class methods into JSON schema function definitions for AutoGen tool-calling
