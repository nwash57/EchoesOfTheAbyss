namespace EchoesOfTheAbyss.Lib.Pipeline;

public class NarrationPipelineRunner : INarrationPipelineRunner
{
    private readonly IEnumerable<INarrationPipelineAgent> _agents;

    public NarrationPipelineRunner(IEnumerable<INarrationPipelineAgent> agents)
    {
        _agents = agents;
    }

    public async Task<NarrationPipelineContext> RunAsync(NarrationPipelineContext context, CancellationToken ct = default)
    {
        var groups = _agents.OrderBy(a => a.Order).GroupBy(a => a.Order);

        foreach (var group in groups)
        {
            var agents = group.ToList();
            if (agents.Count == 1)
                await agents[0].ExecuteAsync(context, ct);
            else
                await Task.WhenAll(agents.Select(a => a.ExecuteAsync(context, ct)));
        }

        return context;
    }
}
