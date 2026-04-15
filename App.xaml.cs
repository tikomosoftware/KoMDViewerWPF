using System;
using Microsoft.UI.Xaml;

namespace KoMDViewer
{
    public partial class App : Application
    {
        private Window m_window;

        public static string StartupFilePath { get; private set; } = "";

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {
                string candidate = cmdArgs[1];
                if (System.IO.File.Exists(candidate))
                    StartupFilePath = candidate;
            }

            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}
