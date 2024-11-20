using System.Windows;

namespace ProcessLimitManager.WPF.Views
{
    /// <summary>
    /// Interaction logic for ManageMessages.xaml
    /// </summary>
    public partial class ManageMessages : Window
    {
        public ManageMessages(string computerId)
        {
            InitializeComponent();
            DataContext = new ViewModels.ManageMessagesViewModel(computerId);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (DataContext is ViewModels.ManageMessagesViewModel viewModel)
            {
                viewModel.Cleanup();
            }
        }
    }
}