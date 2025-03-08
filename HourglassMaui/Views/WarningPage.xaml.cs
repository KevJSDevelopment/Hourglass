// HourglassMaui/Views/WarningPage.xaml.cs
using Microsoft.Maui.Controls;

namespace HourglassMaui.Views
{
    public partial class WarningPage : ContentPage
    {
        public WarningPage(string warningMessage, string motivationalMessage)
        {
            InitializeComponent();
            BindingContext = new
            {
                WarningMessage = warningMessage,
                MotivationalMessage = motivationalMessage
            };
        }

        private async void OnOkClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}