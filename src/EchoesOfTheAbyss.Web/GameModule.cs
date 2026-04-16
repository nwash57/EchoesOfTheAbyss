using Autofac;
using EchoesOfTheAbyss.Lib.Equipment;
using EchoesOfTheAbyss.Lib.Game;
using EchoesOfTheAbyss.Lib.Location;
using EchoesOfTheAbyss.Lib.Llm;
using EchoesOfTheAbyss.Lib.Logging;
using EchoesOfTheAbyss.Lib.Narrative;
using EchoesOfTheAbyss.Lib.Pipeline;
using EchoesOfTheAbyss.Lib.Pipeline.Agents;
using EchoesOfTheAbyss.Lib.Player;
using EchoesOfTheAbyss.Lib.PlotArc;
using EchoesOfTheAbyss.Lib.Rules.Input;
using EchoesOfTheAbyss.Lib.Rules.State;

namespace EchoesOfTheAbyss.Web;

public class GameModule : Module
{
    private readonly LlmConfig _llmConfig;

    public GameModule(LlmConfig llmConfig)
    {
        _llmConfig = llmConfig;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(_llmConfig).AsSelf().SingleInstance();

        builder.RegisterType<OpenAiChatService>()
            .As<IChatService>()
            .SingleInstance();

        builder.RegisterType<NarrativeEvaluator>()
            .As<INarrativeEvaluator>()
            .InstancePerLifetimeScope();

        builder.RegisterType<LocationExtractor>()
            .As<ILocationExtractor>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PlayerStateUpdater>()
            .As<IPlayerStateUpdater>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EquipmentExtractor>()
            .As<IEquipmentExtractor>()
            .InstancePerLifetimeScope();

        // Narration pipeline
        builder.RegisterType<NarrationPipelineRunner>()
            .As<INarrationPipelineRunner>()
            .InstancePerLifetimeScope();

        builder.RegisterType<NarrativeEvaluationAgent>().As<INarrationPipelineAgent>();
        builder.RegisterType<PlotDriftAgent>().As<INarrationPipelineAgent>();
        builder.RegisterType<LocationExtractionAgent>().As<INarrationPipelineAgent>();
        builder.RegisterType<PlayerStateUpdateAgent>().As<INarrationPipelineAgent>();
        builder.RegisterType<EquipmentExtractionAgent>().As<INarrationPipelineAgent>();
        builder.RegisterType<WorldContextAssemblyAgent>().As<INarrationPipelineAgent>();
        builder.RegisterType<StateValidationAgent>().As<INarrationPipelineAgent>();

        // Plot Arc
        builder.RegisterType<PlotArcGenerator>()
            .As<IPlotArcGenerator>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PlotArcTracker>()
            .As<IPlotArcTracker>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SessionLogger>()
            .As<Lib.Logging.ILogger>()
            .InstancePerLifetimeScope();

        builder.RegisterType<WebGameOrchestrator>()
            .As<IGameOrchestrator>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Rule runners
        builder.RegisterType<InputRuleRunner>().As<IInputRuleRunner>().InstancePerLifetimeScope();
        builder.RegisterType<StateRuleRunner>().As<IStateRuleRunner>().InstancePerLifetimeScope();

        // Input rules
        builder.RegisterType<TimeSkipRule>().As<IInputRule>();
        builder.RegisterType<PhantomItemRule>().As<IInputRule>();

        // State rules
        builder.RegisterType<HealthEnforcementRule>().As<IStateRule>();
        builder.RegisterType<DemographicStabilityRule>().As<IStateRule>();
        builder.RegisterType<EquipmentPersistenceRule>().As<IStateRule>();
        builder.RegisterType<EquipmentSlotValidationRule>().As<IStateRule>();
        builder.RegisterType<CoordinateMovementRule>().As<IStateRule>();
    }
}
