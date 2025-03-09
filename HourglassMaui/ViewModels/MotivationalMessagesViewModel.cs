// HourglassMaui/ViewModels/MotivationalMessagesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourglassLibrary.Data;
using HourglassLibrary.Dtos;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using HourglassMaui.Views;

namespace HourglassMaui.ViewModels
{
    public partial class MotivationalMessagesViewModel : ObservableObject
    {
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly string _computerId;

        [ObservableProperty]
        private ObservableCollection<MotivationalMessage> messages;

        public MotivationalMessagesViewModel(MotivationalMessageRepository messageRepo)
        {
            _messageRepo = messageRepo;
            _computerId = ComputerIdentifier.GetUniqueIdentifier();
            LoadMessagesAsync();
        }

        private async Task LoadMessagesAsync()
        {
            var loadedMessages = await _messageRepo.GetMessagesForComputer(_computerId);
            Messages = new ObservableCollection<MotivationalMessage>(loadedMessages);
        }

        [RelayCommand]
        private async Task AddMessage()
        {
            await Application.Current.MainPage.Navigation.PushModalAsync(new AddMessagePage(new AddMessageViewModel(_messageRepo, _computerId)));
            await LoadMessagesAsync(); // Auto-refresh after adding
        }

        [RelayCommand]
        private async Task Delete(int id)
        {
            if (id > 0)
            {
                await _messageRepo.DeleteMessage(id);
                await LoadMessagesAsync(); // Auto-refresh after deleting
                await Application.Current.MainPage.DisplayAlert("Success", "Message deleted successfully", "OK");
            }
        }
    }
}