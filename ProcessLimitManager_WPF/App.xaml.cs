using System.Windows;
using AppLimiterLibrary.Data;
using Microsoft.Extensions.Configuration;
using System.IO;
using ProcessLimiterManager.WPF.Views;

namespace ProcessLimitManager.WPF
{
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Get connection string from app.config
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDirectory = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\.."));
            string configPath = Path.Combine(solutionDirectory, "AppLimiter", "appsettings.json");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(configPath))
                .AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            DatabaseManager.Initialize(Configuration);

            // Create and show the main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}