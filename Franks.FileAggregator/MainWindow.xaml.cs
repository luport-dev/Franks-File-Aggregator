using System.Windows;
using Franks.FileAggregator.Services;
using Franks.FileAggregator.ViewModels;

namespace Franks.FileAggregator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new FileCopyService(), new DialogService());
    }
}