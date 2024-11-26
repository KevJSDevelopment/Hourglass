using AppLimiterLibrary.Dtos;
using ProcessLimitManager.WPF.ViewModels;
using System.Windows;
using ProcessLimitManager.WPF.Converters;

namespace ProcessLimitManager.WPF.Views
{
    /// <summary>
    /// Interaction logic for EditGoalWindow.xaml
    /// </summary>
    public partial class EditGoalWindow : Window
    {
        private readonly EditGoalViewModel _viewModel;

        public MotivationalMessage UpdatedGoal => _viewModel.GetUpdatedGoal();

        public EditGoalWindow(MotivationalMessageRepository repo, string computerId, MotivationalMessage goal = null)
        {
            InitializeComponent();
            _viewModel = new EditGoalViewModel(repo, computerId, goal);
            DataContext = _viewModel;
            Owner = Application.Current.MainWindow;

            _viewModel.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Optional: Add window drag functionality or other window-specific features
        }
    }
}
