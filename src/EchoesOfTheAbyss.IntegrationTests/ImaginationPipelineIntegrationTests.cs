using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Imagination;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Location;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Player;
using EchoesOfTheAbyss.Lib.Rules;
using EchoesOfTheAbyss.Lib.Rules.State;
using EchoesOfTheAbyss.Lib.Shared;

namespace EchoesOfTheAbyss.IntegrationTests;

[Trait("Category", "Integration")]
public class ImaginationPipelineIntegrationTests : IDisposable
{
    private readonly IChatService _chatService;
    private readonly ImaginationPipeline _pipeline;

    public ImaginationPipelineIntegrationTests()
    {
        var config = new LlmConfig("http://localhost", 1234, LlmModels.Qwen3_5__9B__Q8);
        _chatService = new OpenAiChatService(config);
        _pipeline = new ImaginationPipeline(
            new NarrativeEvaluator(_chatService),
            new LocationExtractor(_chatService),
            new PlayerStateUpdater(_chatService),
            new EquipmentExtractor(_chatService));
    }

    public void Dispose() { }

    /// <summary>
    /// The core bug: exposition mentions a belt knife and threadbare tunic,
    /// but equipment slots come back empty after the first imagination run.
    /// </summary>
    [Fact]
    public async Task Exposition_WithEquipment_PopulatesEquipmentSlots()
    {
        var narration = "The morning mist clings to the jagged peaks of the Obsidian Ridge "
            + "as you tighten the leather strap around your worn belt knife, the cold metal "
            + "biting into your palm. Your threadbare tunic offers little protection against "
            + "the howling wind, and every step feels heavier as the ancient forest begins to "
            + "loom ahead, hiding secrets that have swallowed lesser travelers whole.";

        var world = new WorldContext();

        var (result, eval) = await _pipeline.RunAsync(
            playerInput: "", // empty = exposition/first turn
            narration: narration,
            current: world);

        Assert.False(result.Equipment.IsEmpty(),
            "Equipment should not be empty after exposition that mentions items. " +
            $"Got: {result.Equipment}");

        // The narration mentions a belt knife — should be in a hand slot
        var hasKnife = !string.IsNullOrEmpty(result.Equipment.RightHand?.Name)
                    || !string.IsNullOrEmpty(result.Equipment.LeftHand?.Name);
        Assert.True(hasKnife,
            "Exposition mentions a 'belt knife' — expected it in RightHand or LeftHand. " +
            $"RightHand: {result.Equipment.RightHand?.Name}, LeftHand: {result.Equipment.LeftHand?.Name}");

        // The narration mentions a threadbare tunic — should be in torso
        Assert.False(string.IsNullOrEmpty(result.Equipment.Torso?.Name),
            "Exposition mentions a 'threadbare tunic' — expected Torso to be populated. " +
            $"Got: {result.Equipment.Torso?.Name}");
    }

    /// <summary>
    /// Verify the full pipeline populates location on first turn.
    /// </summary>
    [Fact]
    public async Task Exposition_PopulatesLocation()
    {
        var narration = "You stand at the edge of a crumbling watchtower, the remnants of an "
            + "ancient fortress overlooking a fog-choked valley. A rusted sword hangs from "
            + "your belt and a moth-eaten cloak drapes across your shoulders.";

        var world = new WorldContext();

        var (result, _) = await _pipeline.RunAsync("", narration, world);

        Assert.False(string.IsNullOrEmpty(result.CurrentLocation.Title),
            "Location title should be populated after exposition.");
        Assert.False(string.IsNullOrEmpty(result.CurrentLocation.ShortDescription),
            "Location short description should be populated after exposition.");
    }

    /// <summary>
    /// Verify the full pipeline populates player demographics on first turn.
    /// </summary>
    [Fact]
    public async Task Exposition_PopulatesPlayerDemographics()
    {
        var narration = "The old ranger tightens his grip on the worn bow, scanning the "
            + "treeline. His leather jerkin is scarred from years of service, and a hunting "
            + "knife rests in a sheath at his hip.";

        var world = new WorldContext();

        var (result, _) = await _pipeline.RunAsync("", narration, world);

        // Player should have some stats set
        Assert.True(result.Player.Stats.CurrentHealth > 0,
            "Player health should be positive after exposition.");
        Assert.True(result.Player.Stats.CurrentHealth <= 100,
            "Player health should be <= 100 after exposition.");
    }

    /// <summary>
    /// Equipment that exists should persist when the next turn doesn't involve equipment changes.
    /// </summary>
    [Fact]
    public async Task SecondTurn_NoEquipmentChange_PreservesEquipment()
    {
        // Set up world with existing equipment from a previous turn
        var world = new WorldContext
        {
            Equipment = new Lib.Equipment.Equipment
            {
                RightHand = new RightHandEquipment
                {
                    Name = "Worn Belt Knife",
                    Armor = 0,
                    Damage = 8,
                    Description = "A small knife with a leather-wrapped handle."
                },
                Torso = new TorsoEquipment
                {
                    Name = "Threadbare Tunic",
                    Armor = 3,
                    Damage = 0,
                    Description = "A thin tunic offering little protection from the elements."
                }
            },
            CurrentLocation = new Lib.Location.Location
            {
                Title = "Obsidian Ridge",
                ShortDescription = "A jagged mountain ridge",
                LongDescription = "Mist-shrouded peaks with treacherous footing.",
                Type = "landmark"
            }
        };

        var playerInput = "I look around carefully at my surroundings.";
        var narration = "You scan the area, taking in the twisted roots that claw up from "
            + "the rocky soil. The forest ahead is dense and dark, with gnarled oaks blocking "
            + "most of the pale morning light. Nothing stirs, but the silence itself feels predatory.";

        var (result, eval) = await _pipeline.RunAsync(playerInput, narration, world);

        // Equipment should still be there — nothing was picked up or dropped
        Assert.False(string.IsNullOrEmpty(result.Equipment.RightHand?.Name),
            "RightHand equipment should persist when no equipment changes occurred. " +
            $"Got: {result.Equipment.RightHand?.Name}");
        Assert.False(string.IsNullOrEmpty(result.Equipment.Torso?.Name),
            "Torso equipment should persist when no equipment changes occurred. " +
            $"Got: {result.Equipment.Torso?.Name}");
    }

    /// <summary>
    /// When the player picks up a new item, it should appear in the equipment.
    /// </summary>
    [Fact]
    public async Task Turn_PlayerPicksUpItem_EquipmentUpdated()
    {
        var world = new WorldContext
        {
            Equipment = new Lib.Equipment.Equipment
            {
                RightHand = new RightHandEquipment
                {
                    Name = "Worn Belt Knife",
                    Armor = 0,
                    Damage = 8,
                    Description = "A small knife with a leather-wrapped handle."
                }
            },
            CurrentLocation = new Lib.Location.Location
            {
                Title = "Abandoned Camp",
                ShortDescription = "A deserted campsite",
                LongDescription = "A campfire still smolders among scattered belongings.",
                Type = "notable"
            }
        };

        var playerInput = "I pick up the iron shield lying by the campfire.";
        var narration = "You reach down and heft the iron shield, its surface dented but still "
            + "sturdy. The leather straps on the inside are cracked but hold firm as you slide "
            + "your arm through. It's heavier than you expected, but reassuring.";

        var (result, eval) = await _pipeline.RunAsync(playerInput, narration, world);

        // Should now have a shield in left hand
        Assert.False(string.IsNullOrEmpty(result.Equipment.LeftHand?.Name),
            "Player explicitly picked up a shield — expected LeftHand to be populated. " +
            $"Got: {result.Equipment.LeftHand?.Name}");

        // Knife should still be there
        Assert.False(string.IsNullOrEmpty(result.Equipment.RightHand?.Name),
            "Existing knife should persist. " +
            $"Got: {result.Equipment.RightHand?.Name}");
    }

    /// <summary>
    /// The evaluator may not flag equipment update for exposition (items are described
    /// as already present, not explicitly taken), but the pipeline forces all updates
    /// on first turn. This test verifies the pipeline override works correctly.
    /// </summary>
    [Fact]
    public async Task Exposition_PipelineForcesAllUpdates_RegardlessOfEvaluator()
    {
        var narration = "You adjust the strap of your worn leather satchel and grip the "
            + "iron-shod walking staff tighter. The mountain pass narrows ahead.";

        var world = new WorldContext();

        // Even if the evaluator doesn't flag equipment, the pipeline forces it on first turn
        var (result, eval) = await _pipeline.RunAsync("", narration, world);

        Assert.False(result.Equipment.IsEmpty(),
            "Pipeline should force equipment extraction on first turn regardless of evaluator flags.");
        Assert.False(string.IsNullOrEmpty(result.CurrentLocation.Title),
            "Pipeline should force location extraction on first turn.");
        Assert.True(result.Player.Stats.CurrentHealth > 0,
            "Pipeline should force player extraction on first turn.");
    }

    /// <summary>
    /// When combat causes damage, the health delta should be negative.
    /// </summary>
    [Fact]
    public async Task NarrativeEvaluator_CombatDamage_NegativeHealthDelta()
    {
        var world = new WorldContext
        {
            Player = new Lib.Player.Player
            {
                Stats = new PlayerStats { CurrentHealth = 85 }
            }
        };

        var playerInput = "I attack the goblin with my sword.";
        var narration = "You swing your blade at the goblin, but the creature is faster than "
            + "you expected. It ducks under your strike and its jagged dagger rakes across your "
            + "forearm, drawing a bright line of blood. You stagger back, gritting your teeth.";

        var eval = await _pipeline.EvaluateAsync(playerInput, narration, world);

        Assert.True(eval.HealthDelta < 0,
            $"Narration describes physical harm — expected negative healthDelta, got {eval.HealthDelta}");
        Assert.True(eval.UpdatePlayer,
            "Health changed — updatePlayer should be true.");
    }

    /// <summary>
    /// Log entry should be populated for a meaningful turn.
    /// </summary>
    [Fact]
    public async Task NarrativeEvaluator_MeaningfulTurn_ProducesLogEntry()
    {
        var world = new WorldContext
        {
            CurrentLocation = new Lib.Location.Location
            {
                Title = "Dark Forest",
                ShortDescription = "A dense, foreboding forest"
            }
        };

        var playerInput = "I push through the undergrowth toward the light.";
        var narration = "Branches claw at your face as you force your way through the dense "
            + "undergrowth. After what feels like an eternity, you emerge into a moonlit "
            + "clearing. A crumbling stone well sits at its center, its rim overgrown with moss.";

        var eval = await _pipeline.EvaluateAsync(playerInput, narration, world);

        Assert.False(string.IsNullOrWhiteSpace(eval.LogEntry),
            "A meaningful turn should produce a non-empty log entry.");
    }

    /// <summary>
    /// Reproduces the actual game bug: the pipeline extracts equipment correctly on
    /// exposition, but the state rule runner uses the pre-pipeline eval (which has
    /// UpdateEquipment=false for exposition), causing EquipmentPersistenceRule to
    /// revert the equipment back to empty.
    ///
    /// This test runs the pipeline + state rules together, same as the orchestrator does.
    /// </summary>
    [Fact]
    public async Task Exposition_StateRules_DoNotRevertExtractedEquipment()
    {
        var narration = "The morning mist clings to the jagged peaks of the Obsidian Ridge "
            + "as you tighten the leather strap around your worn belt knife, the cold metal "
            + "biting into your palm. Your threadbare tunic offers little protection against "
            + "the howling wind.";

        var previousWorld = new WorldContext();

        // Step 1: Evaluate (like the orchestrator does before RunAsync)
        var preEval = await _pipeline.EvaluateAsync("", narration, previousWorld);

        // Step 2: Run pipeline (which forces flags on first turn internally)
        var (proposedWorld, pipelineEval) = await _pipeline.RunAsync("", narration, previousWorld);

        // Verify pipeline extracted equipment
        Assert.False(proposedWorld.Equipment.IsEmpty(),
            "Pipeline should have extracted equipment from exposition.");

        // Step 3: Run state rules using the pipeline's eval (the fix)
        var stateRules = new IStateRule[]
        {
            new EquipmentPersistenceRule(),
            new EquipmentSlotValidationRule()
        };
        var runner = new StateRuleRunner(stateRules);
        var result = runner.Evaluate(previousWorld, proposedWorld, narration, "", pipelineEval);

        // Equipment should survive the state rules
        Assert.False(result.CorrectedContext.Equipment.IsEmpty(),
            "State rules should NOT revert equipment extracted during exposition. " +
            "The pipeline eval has UpdateEquipment=true (forced for first turn). " +
            $"Got: {result.CorrectedContext.Equipment}");
    }
}
