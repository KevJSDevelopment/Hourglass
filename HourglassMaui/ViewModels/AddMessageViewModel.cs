// HourglassMaui/ViewModels/AddMessageViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourglassLibrary.Data;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace HourglassMaui.ViewModels
{
    public partial class AddMessageViewModel : ObservableObject
    {
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly string _computerId;

        [ObservableProperty]
        private string newMessage;

        public AddMessageViewModel(MotivationalMessageRepository messageRepo, string computerId)
        {
            _messageRepo = messageRepo;
            _computerId = computerId;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (!string.IsNullOrWhiteSpace(NewMessage))
            {
                await _messageRepo.AddMessage(_computerId, NewMessage);
                await Application.Current.MainPage.Navigation.PopModalAsync();
                await Application.Current.MainPage.DisplayAlert("Success", "Message added successfully", "OK");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}