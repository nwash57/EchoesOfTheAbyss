using EchoesOfTheAbyss.Lib.Rules.State;

namespace EchoesOfTheAbyss.Lib.Pipeline.Agents;

public class StateValidationAgent : INarrationPipelineAgent
{
    private readonly IStateRuleRunner _stateRuleRunner;

    public StateValidationAgent(IStateRuleRunner stateRuleRunner)
    {
        _stateRuleRunner = stateRuleRunner;
    }

    public int Order => 50;

    public Task ExecuteAsync(NarrationPipelineContext context, CancellationToken ct = default)
    {
        var result = _stateRuleRunner.Evaluate(
            context.CurrentWorldContext,
            context.NewWorldContext!,
            context.Narration,
            context.PlayerInput,
            context.Evaluation!);

        context.NewWorldContext = result.CorrectedContext;
        context.Violations = result.Violations;

        return Task.CompletedTask;
    }
}
