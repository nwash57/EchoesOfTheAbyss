namespace EchoesOfTheAbyss.Lib.PlotArc;

public interface IPlotArcGenerator
{
    Task<PlotArc> GenerateAsync(CancellationToken ct = default);
    Task<PlotArc> RegenerateAsync(PlotArc current, List<string> adventureLog, CancellationToken ct = default);
}
