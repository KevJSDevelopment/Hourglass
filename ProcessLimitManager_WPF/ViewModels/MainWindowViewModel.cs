using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using HourglassLibrary.Data;
using HourglassLibrary.Dtos;
using Microsoft.Win32;
using HourglassManager.WPF.Views;
using HourglassManager.WPF.Commands;

namespace HourglassManager.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppRepository _appRepository;
        private readonly string _computerId;
        private ObservableCollection<ProcessInfo> _applications;
        private ProcessInfo _selectedApplication;
        public ICommand RefreshCommand { get; }
        public ICommand SetLimitsCommand { get; }
        public ICommand AddApplicationCommand { get; }
        public ICommand RemoveApplicationCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ManageMessagesCommand { get; }
        public ICommand AddWebsiteCommand { get; }
        public ObservableCollection<ProcessInfo> Applications
        {
            get => _applications;
            set => SetProperty(ref _applications, value);
        }

        public ProcessInfo SelectedApplication
        {
            get => _selectedApplication;
            set
            {
                if (SetProperty(ref _selectedApplication, value))
                {
                    // Trigger can-execute changed for commands that depend on selection
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public MainViewModel()
        {
            _appRepository = new AppRepository();
            _computerId = ComputerIdentifier.GetUniqueIdentifier();
            _applications = new ObservableCollection<ProcessInfo>();

            RefreshCommand = new AsyncRelayCommand(_ => LoadApplicationsAsync());
            SetLimitsCommand = new AsyncRelayCommand(SetLimitsAsync, _ => SelectedApplication != null);
            AddApplicationCommand = new AsyncRelayCommand(AddApplicationAsync);
            AddWebsiteCommand = new AsyncRelayCommand(AddWebsiteAsync);
            RemoveApplicationCommand = new AsyncRelayCommand(RemoveApplicationAsync, _ => SelectedApplication != null);
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
            ManageMessagesCommand = new RelayCommand(_ => ManageMessages());
            // Load applications on startup
            _ = LoadApplicationsAsync();
        }

        private async Task LoadApplicationsAsync()
        {
            var apps = await _appRepository.LoadAllLimits(_computerId);
            Applications = new ObservableCollection<ProcessInfo>(apps);
        }

        private async Task AddApplicationAsync(object _)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                Title = "Select an Application"
            };

            if (dialog.ShowDialog() == true)
            {
                string name = Path.GetFileNameWithoutExtension(dialog.FileName);
                var newApp = new ProcessInfo
                {
                    Name = name,
                    Path = dialog.FileName,
                    WarningTime = "00:00:00",
                    KillTime = "00:00:00",
                    ComputerId = _computerId,
                    IsWebsite = false,
                };

                await _appRepository.SaveLimits(newApp);
                await LoadApplicationsAsync();
            }
        }

        private async Task AddWebsiteAsync(object _)
        {
            var addWebsiteDialog = new AddWebsite(_appRepository, _computerId);
            if(addWebsiteDialog.ShowDialog() == true)
            {
                await LoadApplicationsAsync();
            }
        }
        private async Task RemoveApplicationAsync(object _)
        {
            if (SelectedApplication != null)
            {
                await _appRepository.DeleteApp(SelectedApplication.Path);
                await LoadApplicationsAsync();
            }
        }

        private async Task SetLimitsAsync(object _)
        {
            if (SelectedApplication != null)
            {
                var setLimitsWindow = new SetLimitsWindow(SelectedApplication);
                if (setLimitsWindow.ShowDialog() == true)
                {
                    await _appRepository.SaveLimits(SelectedApplication);
                    await LoadApplicationsAsync();
                }
            }
        }

        private void ManageMessages()
        {
            var messagesWindow = new ManageMessages(_computerId);
            messagesWindow.ShowDialog();
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(_computerId);
            settingsWindow.ShowDialog();
            // Refresh if needed after settings are changed
            _ = LoadApplicationsAsync();
        }
    }
}
