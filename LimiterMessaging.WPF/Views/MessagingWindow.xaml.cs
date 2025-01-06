using System.Windows;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using LimiterMessaging.WPF.ViewModels;

namespace LimiterMessaging.WPF.Views
{
    public partial class MessagingWindow : Window
    {
        private readonly MessagingViewModel _viewModel;
        private readonly Action<string, bool> _updateIgnoreStatus;

        public MessagingWindow(
            MotivationalMessage message,
            string timerWarning,
            string processName,
            string computerId,
            Action<string, bool> updateIgnoreStatus,  // New callback
            AppRepository appRepo,
            MotivationalMessageRepository messageRepo,
            SettingsRepository settingsRepo,
            List<MotivationalMessage> messagesSent
            )
        {
            InitializeComponent();
            _updateIgnoreStatus = updateIgnoreStatus;
            _viewModel = new MessagingViewModel(
                message,
                timerWarning,
                processName,
                computerId,
                _updateIgnoreStatus,
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