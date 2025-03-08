// HourglassMaui/ViewModels/DashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourglassLibrary.Data;
using HourglassLibrary.Dtos;
using System.Collections.ObjectModel;

namespace HourglassMaui.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AppRepository _appRepo;
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly string _computerId;

        [ObservableProperty]
        private ObservableCollection<ProcessInfo> limits;

        [ObservableProperty]
        private ObservableCollection<MotivationalMessage> messages;

        [ObservableProperty]
        private string newLimitPath;

        [ObservableProperty]
        private string newLimitName;

        [ObservableProperty]
        private string newWarningTime;

        [ObservableProperty]
        private string newKillTime;

        [ObservableProperty]
        private bool newLimitIgnore;

        [ObservableProperty]
        private string newMessage;

        public DashboardViewModel(AppRepository appRepo, MotivationalMessageRepository messageRepo)
        {
            _appRepo = appRepo;
            _messageRepo = messageRepo;
            _computerId = ComputerIdentifier.GetUniqueIdentifier();
            LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var loadedLimits = await _appRepo.LoadAllLimits(_computerId);
            Limits = new ObservableCollection<ProcessInfo>(loadedLimits);

            var loadedMessages = await _messageRepo.GetMessagesForComputer(_computerId);
            Messages = new ObservableCollection<MotivationalMessage>(loadedMessages);
        }

        [RelayCommand]
        private async Task AddLimit()
        {
            if (!string.IsNullOrWhiteSpace(NewLimitPath) && !string.IsNullOrWhiteSpace(NewLimitName))
            {
                var processInfo = new ProcessInfo
                {
                    ComputerId = _computerId,
                    Name = NewLimitName,
                    Path = NewLimitPath,
                    WarningTime = NewWarningTime,
                    KillTime = NewKillTime,
                    Ignore = NewLimitIgnore,
                    IsWebsite = Uri.IsWellFormedUriString(NewLimitPath, UriKind.Absolute)
                };
                await _appRepo.SaveLimits(processInfo);
                await LoadDataAsync();
                NewLimitPath = string.Empty;
                NewLimitName = string.Empty;
                NewWarningTime = string.Empty;
                NewKillTime = string.Empty;
                NewLimitIgnore = false;
                await Application.Current.MainPage.DisplayAlert("Success", "Limit added successfully", "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteLimit(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                await _appRepo.DeleteApp(path);
                await LoadDataAsync();
                await Application.Current.MainPage.DisplayAlert("Success", "Limit deleted successfully", "OK");
            }
        }

        [RelayCommand]
        private async Task AddMessage()
        {
            if (!string.IsNullOrWhiteSpace(NewMessage))
            {
                await _messageRepo.AddMessage(_computerId, NewMessage);
                await LoadDataAsync();
                NewMessage = string.Empty;
                await Application.Current.MainPage.DisplayAlert("Success", "Message added successfully", "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteMessage(int id)
        {
            if (id > 0)
            {
                await _messageRepo.DeleteMessage(id);
                await LoadDataAsync();
                await Application.Current.MainPage.DisplayAlert("Success", "Message deleted successfully", "OK");
            }
        }
    }
}