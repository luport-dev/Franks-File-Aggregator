using System.IO;
using Franks.FileAggregator.Models;

namespace Franks.FileAggregator.Services;

public sealed class FileCopyService : IFileCopyService
{
    public async Task<CopyResult> CopyAllAsync(string source, string target, IProgress<CopyProgress>? progress, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {source}");
        }

        var normalizedSource = Path.GetFullPath(source);
        var normalizedTarget = Path.GetFullPath(target);
        Directory.CreateDirectory(normalizedTarget);

        var files = Directory
            .EnumerateFiles(normalizedSource, "*", SearchOption.AllDirectories)
            .ToList();
            
        var result = new CopyResult
        {
            StartedAt = DateTimeOffset.Now,
            TotalFiles = files.Count
        };

        var index = 0;
        foreach (var file in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.WasCanceled = true;
                break;
            }

            index++;
            var destination = Path.Combine(normalizedTarget, Path.GetFileName(file));
            
            try
            {
                await CopyFileAsync(file, destination, cancellationToken).ConfigureAwait(false);
                result.CopiedFiles++;
            }
            catch (OperationCanceledException)
            {
                result.WasCanceled = true;
                break;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new CopyError
                {
                    FilePath = file,
                    Message = ex.Message
                });
            }

            var percent = result.TotalFiles == 0
                ? 0d
                : Math.Min(100d, Math.Round((double)index / result.TotalFiles * 100d, 2));

            progress?.Report(new CopyProgress
            {
                CurrentIndex = index,
                Total = result.TotalFiles,
                FileName = Path.GetFileName(file),
                Percent = percent
            });
        }

        result.EndedAt = DateTimeOffset.Now;
        return result;
    }

    private static async Task CopyFileAsync(string sourceFile, string destinationFile, CancellationToken cancellationToken)
    {
        const int bufferSize = 81920;

        await using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        await using var targetStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

        await sourceStream.CopyToAsync(targetStream, bufferSize, cancellationToken).ConfigureAwait(false);
    }
}
