using System.Windows;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using LimiterMessaging.WPF.ViewModels;

namespace LimiterMessaging.WPF.Views
{
    public partial class MessagingWindow : Window
    {
        private readonly MessagingViewModel _viewModel;

        public MessagingWindow(
            MotivationalMessage message,
            string timerWarning,
            string processName,
            string computerId,
            Dictionary<string, bool> ignoreStatusCache,
            AppRepository appRepo,
            MotivationalMessageRepository messageRepo,
            SettingsRepository settingsRepo,
            List<MotivationalMessage> messagesSent)
        {
            InitializeComponent();
            _viewModel = new MessagingViewModel(
                message,
                timerWarning,
                processName,
                computerId,
                ignoreStatusCache,
                appRepo,
                messageRepo,
                settingsRepo,
                messagesSent);

            DataContext = _viewModel;
            _viewModel.RequestClose += () => Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            _viewModel.Dispose();
        }
    }
}