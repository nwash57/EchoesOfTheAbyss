namespace EchoesOfTheAbyss.Lib.Pipeline;

public interface INarrationPipelineAgent
{
    int Order { get; }
    Task ExecuteAsync(NarrationPipelineContext context, CancellationToken ct = default);
}
