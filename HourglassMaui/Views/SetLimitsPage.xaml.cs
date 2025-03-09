// HourglassMaui/Views/SetLimitsPage.xaml.cs
using HourglassMaui.ViewModels;
using Microsoft.Maui.Controls;

namespace HourglassMaui.Views
{
    public partial class SetLimitsPage : ContentPage
    {
        public SetLimitsPage(SetLimitsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}