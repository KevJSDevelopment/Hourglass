using ProcessLimitManager.WPF.ViewModels;
using System.Windows;

namespace ProcessLimitManager.WPF.Views
{
    /// <summary>
    /// Interaction logic for ManageMessages.xaml
    /// </summary>
    public partial class ManageMessages : Window
    {
        private readonly ManageMessagesViewModel _viewModel;

        public ManageMessages(string computerId)
        {
            InitializeComponent();
            _viewModel = new ViewModels.ManageMessagesViewModel(computerId);
            DataContext = _viewModel;

            // Subscribe to the RequestClose event
            _viewModel.RequestClose += () => Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is ViewModels.ManageMessagesViewModel viewModel)
            {
                viewModel.Cleanup();

                // Unsubscribe from the event to prevent memory leaks
                viewModel.RequestClose -= () => Close();
            }
        }
    }
}