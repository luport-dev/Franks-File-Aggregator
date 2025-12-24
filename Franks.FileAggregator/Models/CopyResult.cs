using System;
using System.Collections.Generic;

namespace Franks.FileAggregator.Models;

public sealed class CopyResult
{
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset EndedAt { get; set; } = DateTimeOffset.Now;
    public int TotalFiles { get; set; }
    public int CopiedFiles { get; set; }
    public bool WasCanceled { get; set; }
    public List<CopyError> Errors { get; } = new();
}

public sealed class CopyError
{
    public string FilePath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
