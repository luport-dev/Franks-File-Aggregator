using System.Threading;
using System.Threading.Tasks;
using Franks.FileAggregator.Models;

namespace Franks.FileAggregator.Services;

public interface IFileCopyService
{
    Task<CopyResult> CopyAllAsync(string source, string target, IProgress<CopyProgress>? progress, CancellationToken cancellationToken);
}
