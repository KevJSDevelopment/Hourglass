// HourglassMaui/Views/AddMessagePage.xaml.cs
using HourglassMaui.ViewModels;
using Microsoft.Maui.Controls;

namespace HourglassMaui.Views
{
    public partial class AddMessagePage : ContentPage
    {
        public AddMessagePage(AddMessageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}