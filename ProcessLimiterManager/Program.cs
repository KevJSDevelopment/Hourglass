using AppLimiterLibrary;
using Microsoft.Extensions.Configuration;

namespace ProcessLimiterManager
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>

        public static IConfiguration Configuration { get; private set; }

        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDirectory = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\.."));
            string configPath = Path.Combine(solutionDirectory, "AppLimiter", "appsettings.json");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(configPath))
                .AddJsonFile(Path.GetFileName(configPath), optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            DatabaseManager.Initialize(Configuration);
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}