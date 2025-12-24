using System;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using WpfMessageBox = System.Windows.MessageBox;

namespace Franks.FileAggregator.Services;

public sealed class DialogService : IDialogService
{
    public string? BrowseForFolder(string? initialPath = null, string description = "Ordner auswaehlen")
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = description,
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true,
            SelectedPath = GetInitialPath(initialPath)
        };

        return dialog.ShowDialog() == Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }

    public bool Confirm(string message, string title)
    {
        return WpfMessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public void ShowError(string message, string title = "Fehler")
    {
        WpfMessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfo(string message, string title = "Hinweis")
    {
        WpfMessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string GetInitialPath(string? initialPath)
    {
        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
        {
            return initialPath;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
