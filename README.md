# Franks File Aggregator

Desktop WPF tool that copies all files from a source directory (including subdirectories) into a target directory with progress, status text, cancellation, and a final report.

## Running
- Build: `dotnet build Franks-File-Aggregator.sln`
- Run: `dotnet run --project Franks.FileAggregator`

## Publish (self-contained single EXE)
- `dotnet publish Franks.FileAggregator/Franks.FileAggregator.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
- The executable is emitted under `Franks.FileAggregator/bin/Release/net8.0-windows/win-x64/publish/`.

## Notes
- Start is enabled only when a valid source exists, a target is provided, and at least one file is discovered.
- The copy operation runs asynchronously with per-file error collection; failures are listed while the process continues.
- Cancel stops the copy loop and reports partial progress.
