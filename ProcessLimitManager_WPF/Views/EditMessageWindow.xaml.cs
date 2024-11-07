using ProcessLimiterManager.WPF.ViewModels;
using ProcessLimitManager.WPF.ViewModels;
using System.Windows;

namespace ProcessLimiterManager.WPF.Views
{
    public partial class EditMessageWindow : Window
    {
        private readonly EditMessageViewModel _viewModel;

        public string UpdatedMessage => _viewModel.Message;

        public EditMessageWindow(string currentMessage)
        {
            InitializeComponent();
            _viewModel = new EditMessageViewModel(currentMessage);
            DataContext = _viewModel;
            Owner = Application.Current.MainWindow;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_viewModel.Message))
            {
                MessageBox.Show("Message cannot be empty.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}