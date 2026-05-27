using System.IO;
using System.Windows;

namespace KoMDViewer;

public partial class App : Application
{
    public static string StartupFilePath { get; private set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Length > 0 && File.Exists(e.Args[0]))
        {
            StartupFilePath = e.Args[0];
        }

        var window = new MainWindow();
        window.Show();
    }
}
