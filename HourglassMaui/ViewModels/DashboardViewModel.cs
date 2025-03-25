// HourglassMaui/ViewModels/LimitsDashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourglassLibrary.Data;
using HourglassLibrary.Dtos;
using HourglassMaui.Views;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace HourglassMaui.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AppRepository _appRepo;
        private readonly string _computerId;
        private ProcessInfo _selectedLimit;

        [ObservableProperty]
        private ObservableCollection<ProcessInfo> _limits; // Renamed to follow lowerCamel pattern

        [ObservableProperty]
        private bool isLimitSelected;

        public ProcessInfo SelectedLimit
        {
            get => _selectedLimit;
            set
            {
                SetProperty(ref _selectedLimit, value);
                IsLimitSelected = _selectedLimit != null;
            }
        }

        public DashboardViewModel(AppRepository appRepo)
        {
            _appRepo = appRepo;
            _computerId = ComputerIdentifier.GetUniqueIdentifier();
            LoadLimitsAsync();
        }

        private async Task LoadLimitsAsync()
        {
            var loadedLimits = await _appRepo.LoadAllLimits(_computerId);
            Limits = new ObservableCollection<ProcessInfo>(loadedLimits);
            SelectedLimit = null; // Reset selection after refresh
        }

        [RelayCommand]
        private async Task SetLimits()
        {
            if (SelectedLimit != null)
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(new SetLimitsPage(new SetLimitsViewModel(_appRepo, _computerId, SelectedLimit)));
                await LoadLimitsAsync(); // Auto-refresh after setting limits
            }
        }

        [RelayCommand]
        private async Task ShowAddOptions()
        {
            var action = await Application.Current.MainPage.DisplayActionSheet("Add New Limit", "Cancel", null, "Add Application", "Add Website");
            if (action == "Add Application" || action == "Add Website")
            {
                var viewModel = new SetLimitsViewModel(_appRepo, _computerId)
                {
                    IsWebsite = action == "Add Website"
                };
                await Application.Current.MainPage.Navigation.PushModalAsync(new SetLimitsPage(viewModel));
                await LoadLimitsAsync(); // Auto-refresh after adding
            }
        }

        [RelayCommand]
        private async Task Remove()
        {
            if (SelectedLimit != null)
            {
                await _appRepo.DeleteApp(SelectedLimit.Path);
                await LoadLimitsAsync(); // Auto-refresh after removing
                await Application.Current.MainPage.DisplayAlert("Success", "Limit removed successfully", "OK");
            }
        }

        [RelayCommand]
        private async Task ManageMessages()
        {
            var messagesViewModel = Application.Current.MainPage.Handler.MauiContext.Services.GetRequiredService<MotivationalMessagesViewModel>();
            await Application.Current.MainPage.Navigation.PushAsync(new MotivationalMessagesPage(messagesViewModel));
        }

        [RelayCommand]
        private async Task Settings()
        {
            await Application.Current.MainPage.DisplayAlert("Settings", "Settings page not implemented yet.", "OK");
            // Future: Navigate to a SettingsPage
        }
    }
}