using System.Configuration;
using System.Data;
using System.Windows;

namespace Franks.FileAggregator;

public partial class App : System.Windows.Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var mainWindow = new MainWindow
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen
		};

		MainWindow = mainWindow;
		mainWindow.Show();
	}
}

