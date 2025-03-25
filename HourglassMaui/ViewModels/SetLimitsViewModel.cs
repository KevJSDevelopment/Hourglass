// HourglassMaui/ViewModels/SetLimitsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourglassLibrary.Data;
using HourglassLibrary.Dtos;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace HourglassMaui.ViewModels
{
    public partial class SetLimitsViewModel : ObservableObject
    {
        private readonly AppRepository _appRepo;
        private readonly string _computerId;

        [ObservableProperty]
        private string path;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string warningTime;

        [ObservableProperty]
        private string killTime;

        [ObservableProperty]
        private bool ignore;

        [ObservableProperty]
        private bool isWebsite;

        [ObservableProperty]
        private string placeholder;

        public SetLimitsViewModel(AppRepository appRepo, string computerId, ProcessInfo limit = null)
        {
            _appRepo = appRepo;
            _computerId = computerId;
            if (limit.IsWebsite) Placeholder = "Add Url";
            else Placeholder = "Add executable path";

            if (limit != null)
            {
                Path = limit.Path;
                Name = limit.Name;
                WarningTime = limit.WarningTime;
                KillTime = limit.KillTime;
                Ignore = limit.Ignore;
                IsWebsite = limit.IsWebsite;
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (!string.IsNullOrWhiteSpace(Path) && !string.IsNullOrWhiteSpace(Name))
            {
                var processInfo = new ProcessInfo
                {
                    ComputerId = _computerId,
                    Path = Path,
                    Name = Name,
                    WarningTime = WarningTime,
                    KillTime = KillTime,
                    Ignore = Ignore,
                    IsWebsite = IsWebsite
                };
                await _appRepo.SaveLimits(processInfo);
                await Application.Current.MainPage.Navigation.PopModalAsync();
                await Application.Current.MainPage.DisplayAlert("Success", "Limit saved successfully", "OK");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            // close and navigate
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }
    }
}