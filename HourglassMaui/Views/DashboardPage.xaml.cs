// HourglassMaui/Views/DashboardPage.xaml.cs
using HourglassMaui.ViewModels;
using Microsoft.Maui.Controls;

namespace HourglassMaui.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}