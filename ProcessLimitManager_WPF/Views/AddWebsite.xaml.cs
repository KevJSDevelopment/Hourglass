using AppLimiterLibrary.Data;
using ProcessLimitManager.WPF.ViewModels;
using System.Security.Policy;
using System.Windows;


namespace ProcessLimitManager.WPF.Views
{
    /// <summary>
    /// Interaction logic for AddWebsite.xaml
    /// </summary>
    public partial class AddWebsite : Window
    {
        private readonly AddWebsiteViewModel _viewModel;

        public string Url => _viewModel.Url;
        public AddWebsite(AppRepository appRepo, string computerId)
        {
            InitializeComponent();
            _viewModel = new AddWebsiteViewModel(appRepo, computerId);
            DataContext = _viewModel;
        }
    }
}
