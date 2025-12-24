namespace Franks.FileAggregator.Services;

public interface IDialogService
{
    string? BrowseForFolder(string? initialPath = null, string description = "Ordner auswaehlen");
    bool Confirm(string message, string title);
    void ShowError(string message, string title = "Fehler");
    void ShowInfo(string message, string title = "Hinweis");
}
