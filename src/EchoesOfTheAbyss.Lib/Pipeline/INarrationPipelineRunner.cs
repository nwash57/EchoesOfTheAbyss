namespace EchoesOfTheAbyss.Lib.Pipeline;

public interface INarrationPipelineRunner
{
    Task<NarrationPipelineContext> RunAsync(NarrationPipelineContext context, CancellationToken ct = default);
}
