using HourglassLibrary.Data;
using HourglassManager.WPF.ViewModels;
using System.Security.Policy;
using System.Windows;


namespace HourglassManager.WPF.Views
{
    /// <summary>
    /// Interaction logic for AddWebsite.xaml
    /// </summary>
    public partial class AddWebsite : Window
    {
        private readonly AddWebsiteViewModel _viewModel;
        public AddWebsite(AppRepository appRepo, string computerId)
        {
            InitializeComponent();
            _viewModel = new AddWebsiteViewModel(appRepo, computerId);
            DataContext = _viewModel;
        }
    }
}
