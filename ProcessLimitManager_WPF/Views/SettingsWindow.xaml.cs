using ProcessLimiterManager.WPF.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace ProcessLimiterManager.WPF.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(string computerId)
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel(computerId);
            DataContext = _viewModel;
            Owner = Application.Current.MainWindow;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _viewModel.Cleanup();
        }
    }
}