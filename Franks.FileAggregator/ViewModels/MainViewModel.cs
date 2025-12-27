using System.Collections.ObjectModel;
using System.IO;
using Franks.FileAggregator.Infrastructure;
using Franks.FileAggregator.Models;
using Franks.FileAggregator.Services;

namespace Franks.FileAggregator.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IFileCopyService _fileCopyService;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource? _copyCts;
    private CancellationTokenSource? _countCts;

    private string? _sourcePath;
    private string? _targetPath;
    private int _totalFiles;
    private int _copiedFiles;
    private int _currentIndex;
    private double _progressValue;
    private string _statusText = string.Empty;
    private string _summaryText = string.Empty;
    private bool _isBusy;
    private bool _isCounting;
    private bool _isUnAuthorizedAccess;

    public MainViewModel(IFileCopyService fileCopyService, IDialogService dialogService)
    {
        _fileCopyService = fileCopyService;
        _dialogService = dialogService;

        ErrorMessages = new ObservableCollection<string>();

        BrowseSourceCommand = new RelayCommand(BrowseSource, () => IsIdle);
        BrowseTargetCommand = new RelayCommand(BrowseTarget, () => IsIdle);
        StartCopyCommand = new AsyncRelayCommand(StartCopyAsync, CanStartCopy);
        CancelCommand = new RelayCommand(CancelCopy, () => IsBusy);

        UpdateIdleStatus();
    }

    public RelayCommand BrowseSourceCommand { get; }
    public RelayCommand BrowseTargetCommand { get; }
    public AsyncRelayCommand StartCopyCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ObservableCollection<string> ErrorMessages { get; }

    public string? SourcePath
    {
        get => _sourcePath;
        set
        {
            if (SetProperty(ref _sourcePath, value?.Trim()))
            {
                _ = RefreshFileCountAsync();
                RaiseCommandStates();
                UpdateIdleStatus();
            }
        }
    }

    public string? TargetPath
    {
        get => _targetPath;
        set
        {
            if (SetProperty(ref _targetPath, value?.Trim()))
            {
                RaiseCommandStates();
                UpdateIdleStatus();
            }
        }
    }

    public int TotalFiles
    {
        get => _totalFiles;
        private set
        {
            if (SetProperty(ref _totalFiles, value))
            {
                OnPropertyChanged(nameof(FileCountText));
                RaiseCommandStates();
            }
        }
    }

    public int CopiedFiles
    {
        get => _copiedFiles;
        private set => SetProperty(ref _copiedFiles, value);
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        private set => SetProperty(ref _currentIndex, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsIdle));
                OnPropertyChanged(nameof(IsCopying));
                RaiseCommandStates();
            }
        }
    }

    public bool IsIdle => !IsBusy;

    public bool IsCopying => IsBusy;

    public bool IsCounting
    {
        get => _isCounting;
        private set
        {
            if (SetProperty(ref _isCounting, value))
            {
                RaiseCommandStates();

                if (!value)
                {
                    UpdateIdleStatus();
                }
            }
        }
    }

    public bool IsUnAuthorizedAccess
    {
        get => _isUnAuthorizedAccess;
        private set
        {
            if (SetProperty(ref _isUnAuthorizedAccess, value))
            {
                RaiseCommandStates();

                if (value)
                {
                    UpdateIdleStatus();
                }
            }
        }
    }

    public string FileCountText => TotalFiles > 0
        ? $"Gefundene Dateien: {TotalFiles}"
        : "Keine Dateien gefunden";

    private bool CanStartCopy()
    {
        return IsIdle
            && !IsCounting
            && !string.IsNullOrWhiteSpace(SourcePath)
            && Directory.Exists(SourcePath)
            && !string.IsNullOrWhiteSpace(TargetPath)
            && TotalFiles > 0;
    }

    private async Task RefreshFileCountAsync()
    {
        _countCts?.Cancel();

        TotalFiles = 0;
        CopiedFiles = 0;
        CurrentIndex = 0;
        ProgressValue = 0;
        IsUnAuthorizedAccess = false;
        SummaryText = string.Empty;

        if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
        {
            return;
        }

        var localCts = new CancellationTokenSource();
        _countCts = localCts;
        IsCounting = true;

        try
        {
            var count = await Task.Run(() => Directory.EnumerateFiles(SourcePath, "*",SearchOption.AllDirectories).Count(), localCts.Token);

            if (!localCts.IsCancellationRequested)
            {
                TotalFiles = count;
            }
        }
        catch (UnauthorizedAccessException)
        {
            IsUnAuthorizedAccess = true;

        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation
        }
        finally
        {
            if (_countCts == localCts)
            {
                IsCounting = false;
                _countCts.Dispose();
                _countCts = null;

                UpdateIdleStatus();
            }
        }
    }

    private void BrowseSource()
    {
        var selected = _dialogService.BrowseForFolder(SourcePath, "Quellordner auswaehlen");
        if (!string.IsNullOrWhiteSpace(selected))
        {
            SourcePath = selected;
        }
    }

    private void BrowseTarget()
    {
        var selected = _dialogService.BrowseForFolder(TargetPath, "Zielordner auswaehlen");
        if (!string.IsNullOrWhiteSpace(selected))
        {
            TargetPath = selected;
        }
    }

    private async Task StartCopyAsync()
    {
        if (!EnsureInputIsValid())
        {
            return;
        }

        _copyCts = new CancellationTokenSource();
        ErrorMessages.Clear();
        CopiedFiles = 0;
        CurrentIndex = 0;
        ProgressValue = 0;
        SummaryText = string.Empty;
        StatusText = "Starte Kopiervorgang...";
        IsBusy = true;

        var progress = new Progress<CopyProgress>(update =>
        {
            CurrentIndex = update.CurrentIndex;
            ProgressValue = update.Percent;
            StatusText = $"Kopiere Datei {update.CurrentIndex} von {update.Total}: {update.FileName}";
        });

        CopyResult? result = null;

        try
        {
            result = await _fileCopyService.CopyAllAsync(SourcePath!, TargetPath!, progress, _copyCts.Token);
        }
        catch (OperationCanceledException)
        {
            result ??= new CopyResult();
            result.WasCanceled = true;
            result.EndedAt = DateTimeOffset.Now;
        }
        finally
        {
            IsBusy = false;
            _copyCts?.Dispose();
            _copyCts = null;
            RaiseCommandStates();
        }

        if (result == null)
        {
            return;
        }

        TotalFiles = result.TotalFiles;
        CopiedFiles = result.CopiedFiles;

        foreach (var error in result.Errors)
        {
            ErrorMessages.Add($"{error.FilePath}: {error.Message}");
        }

        SummaryText = BuildSummary(result);
        StatusText = result.WasCanceled ? "Abgebrochen" : "Fertig";
    }

    private bool EnsureInputIsValid()
    {
        if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
        {
            _dialogService.ShowError("Bitte ein existierendes Quellverzeichnis angeben.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            _dialogService.ShowError("Bitte ein Zielverzeichnis angeben.");
            return false;
        }

        if (!Directory.Exists(TargetPath))
        {
            var create = _dialogService.Confirm($"Zielverzeichnis '{TargetPath}' anlegen?", "Zielverzeichnis existiert nicht");
            if (!create)
            {
                return false;
            }

            Directory.CreateDirectory(TargetPath);
        }

        if (TotalFiles == 0)
        {
            _dialogService.ShowInfo("Im Quellverzeichnis wurden keine Dateien gefunden.", "Keine Dateien");
            return false;
        }

        return true;
    }

    private void CancelCopy()
    {
        _copyCts?.Cancel();
    }

    private string BuildSummary(CopyResult result)
    {
        var duration = result.EndedAt - result.StartedAt;
        var status = result.WasCanceled ? "Abgebrochen" : "Abgeschlossen";
        var errorCount = result.Errors.Count;

        return $"{status} | Gestartet: {result.StartedAt:t} | Dauer: {duration:mm\\:ss} | Kopiert: {result.CopiedFiles}/{result.TotalFiles} | Fehler: {errorCount}";
    }

    private void RaiseCommandStates()
    {
        BrowseSourceCommand.RaiseCanExecuteChanged();
        BrowseTargetCommand.RaiseCanExecuteChanged();
        StartCopyCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
    }

    private void UpdateIdleStatus()
    {
        if (IsBusy)
        {
            return;
        }

        if (IsUnAuthorizedAccess)
        {
            StatusText = "Keine Dateizahl verfuegbar. Grund: Zugriff auf einige Ordner/Dateien wurde verweigert.";
            return;
        }

        if (IsCounting)
        {
            StatusText = "Zähle Dateien...";
            return;
        }

        if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
        {
            StatusText = "Bitte Quell-Verzeichnis wählen";
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetPath))
        {
            StatusText = "Bitte Ziel-Verzeichnis wählen";
            return;
        }

        if (CanStartCopy())
        {
            StatusText = "Bereit...";
            return;
        }

        StatusText = string.Empty;
    }
}
