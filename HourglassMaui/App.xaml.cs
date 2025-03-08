// HourglassMaui/App.xaml.cs
using HourglassMaui.ViewModels;
using HourglassMaui.Views;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace HourglassMaui
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // Copy appsettings.json to FileSystem.AppDataDirectory if it doesn't exist
            string targetPath = Path.Combine(FileSystem.AppDataDirectory, "appsettings.json");
            if (!File.Exists(targetPath))
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
                using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);
            }

            var dashboardViewModel = serviceProvider.GetRequiredService<DashboardViewModel>();
            MainPage = new NavigationPage(new DashboardPage(dashboardViewModel));
        }
    }
}