using System.Windows;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;

namespace LimiterMessaging.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                if (e.Args.Length >= 2)
                {
                    MotivationalMessage message = new MotivationalMessage()
                    {
                        Id = 0,
                        TypeId = 1,
                        TypeDescription = "Message",
                        ComputerId = ComputerIdentifier.GetUniqueIdentifier(),
                        Message = e.Args[0],
                        FilePath = null
                    };

                    var window = new Views.MessagingWindow(
                        message,
                        "",
                        e.Args[1],
                        "",
                        new Dictionary<string, bool>(),
                        new AppRepository(),
                        new MotivationalMessageRepository(),
                        new SettingsRepository(""),
                        null);

                    window.Show();
                }
                else
                {
                    MessageBox.Show("Invalid arguments provided.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}