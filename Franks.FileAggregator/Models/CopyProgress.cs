namespace Franks.FileAggregator.Models;

public sealed class CopyProgress
{
    public int CurrentIndex { get; init; }
    public int Total { get; init; }
    public string FileName { get; init; } = string.Empty;
    public double Percent { get; init; }
}
