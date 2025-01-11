using System.Windows;
using HourglassLibrary.Data;
using Microsoft.Extensions.Configuration;
using System.IO;
using HourglassManager.WPF.Views;

namespace HourglassManager.WPF
{
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }
        public App()
        {
            // Add global exception handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Hourglass", "startup_log.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, $"Application starting at {DateTime.Now}\n");

                // Look for config in the app's installation directory
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(baseDirectory, "appsettings.json");

                File.AppendAllText(logPath, $"Looking for config at: {configPath}\n");

                var builder = new ConfigurationBuilder()
                    .SetBasePath(baseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();
                DatabaseManager.Initialize(Configuration);

                // Create and show the main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Hourglass", "crash_log.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.WriteAllText(logPath, $"Startup crash at {DateTime.Now}:\n{ex}");
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("Dispatcher Exception", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogError("Domain Exception", e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogError("Task Exception", e.Exception);
            e.SetObserved();
        }

        private void LogError(string type, Exception ex)
        {
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Hourglass", "error_log.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                string message = $"{type} at {DateTime.Now}:\n{ex}\n\n";
                File.AppendAllText(logPath, message);
            }
            catch
            {
                // Fail silently if we can't log
            }
        }
    }
}