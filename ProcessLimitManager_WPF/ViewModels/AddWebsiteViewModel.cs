using HourglassLibrary.Data;
using HourglassLibrary.Dtos;
using HourglassManager.WPF.Commands;
using System.Windows.Input;

namespace HourglassManager.WPF.ViewModels
{
    public class AddWebsiteViewModel : ViewModelBase
    {
        private string _url;
        private string _computerId;
        private readonly AppRepository _appRepository;
        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value);
        }

        public ICommand AddUrlCommand { get; }

        public AddWebsiteViewModel(AppRepository appRepo, string computerId)
        {
            Url = string.Empty;
            _appRepository = appRepo;
            _computerId = computerId;
            AddUrlCommand = new RelayCommand(
                execute: _ => AddWebsite(),
                canExecute: _ => !string.IsNullOrWhiteSpace(Url)
            );
        }

        private async void AddWebsite()
        {
            var newSite = new ProcessInfo
            {
                Name = "Website",
                Path = Url,
                WarningTime = "00:00:00",
                KillTime = "00:00:00",
                ComputerId = _computerId,
                IsWebsite = true,
            };

            await _appRepository.SaveLimits(newSite);
        }
    }
}
