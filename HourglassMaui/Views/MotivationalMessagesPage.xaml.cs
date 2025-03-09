// HourglassMaui/Views/MotivationalMessagesPage.xaml.cs
using HourglassMaui.ViewModels;
using Microsoft.Maui.Controls;

namespace HourglassMaui.Views
{
    public partial class MotivationalMessagesPage : ContentPage
    {
        public MotivationalMessagesPage(MotivationalMessagesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}