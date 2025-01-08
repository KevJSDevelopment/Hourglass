using HourglassLibrary.Dtos;
using HourglassManager.WPF.ViewModels;
using System.Windows;

namespace HourglassManager.WPF.Views
{
    public partial class SetLimitsWindow : Window
    {
        public SetLimitsWindow(ProcessInfo processInfo)
        {
            InitializeComponent();
            DataContext = new SetLimitsViewModel(processInfo);
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
    }
}