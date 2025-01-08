using System.Collections.ObjectModel;
using System.Windows.Input;
using HourglassLibrary.Dtos;
using HourglassManager.WPF.Commands;

namespace HourglassManager.WPF.ViewModels
{
    public class SetLimitsViewModel : ViewModelBase
    {
        private ProcessInfo _processInfo;
        private string _selectedWarningHours;
        private string _selectedWarningMinutes;
        private string _selectedWarningSeconds;
        private string _selectedKillHours;
        private string _selectedKillMinutes;
        private string _selectedKillSeconds;
        private bool _ignoreLimits;

        public ObservableCollection<string> Hours { get; } = new();
        public ObservableCollection<string> MinutesSeconds { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }

        public string SelectedWarningHours
        {
            get => _selectedWarningHours;
            set => SetProperty(ref _selectedWarningHours, value);
        }

        public string SelectedWarningMinutes
        {
            get => _selectedWarningMinutes;
            set => SetProperty(ref _selectedWarningMinutes, value);
        }

        public string SelectedWarningSeconds
        {
            get => _selectedWarningSeconds;
            set => SetProperty(ref _selectedWarningSeconds, value);
        }

        public string SelectedKillHours
        {
            get => _selectedKillHours;
            set => SetProperty(ref _selectedKillHours, value);
        }

        public string SelectedKillMinutes
        {
            get => _selectedKillMinutes;
            set => SetProperty(ref _selectedKillMinutes, value);
        }

        public string SelectedKillSeconds
        {
            get => _selectedKillSeconds;
            set => SetProperty(ref _selectedKillSeconds, value);
        }

        public bool IgnoreLimits
        {
            get => _ignoreLimits;
            set => SetProperty(ref _ignoreLimits, value);
        }

        public string ProcessName => _processInfo.Name;

        public SetLimitsViewModel(ProcessInfo processInfo)
        {
            _processInfo = processInfo;
            InitializeCollections();
            LoadCurrentValues();

            SaveCommand = new RelayCommand(_ => Save());
            ResetCommand = new RelayCommand(_ => Reset());
        }

        private void InitializeCollections()
        {
            // Populate hours (0-48)
            for (int i = 0; i <= 48; i++)
            {
                Hours.Add(i.ToString("D2"));
            }

            // Populate minutes and seconds (0-59)
            for (int i = 0; i <= 59; i++)
            {
                MinutesSeconds.Add(i.ToString("D2"));
            }
        }

        private void LoadCurrentValues()
        {
            var warningParts = _processInfo.WarningTime.Split(':');
            var killParts = _processInfo.KillTime.Split(':');

            SelectedWarningHours = warningParts[0];
            SelectedWarningMinutes = warningParts[1];
            SelectedWarningSeconds = warningParts[2];

            SelectedKillHours = killParts[0];
            SelectedKillMinutes = killParts[1];
            SelectedKillSeconds = killParts[2];

            IgnoreLimits = _processInfo.Ignore;
        }

        private void Save()
        {
            _processInfo.WarningTime = $"{SelectedWarningHours}:{SelectedWarningMinutes}:{SelectedWarningSeconds}";
            _processInfo.KillTime = $"{SelectedKillHours}:{SelectedKillMinutes}:{SelectedKillSeconds}";
            _processInfo.Ignore = IgnoreLimits;
        }

        private void Reset()
        {
            SelectedWarningHours = "00";
            SelectedWarningMinutes = "00";
            SelectedWarningSeconds = "00";
            SelectedKillHours = "00";
            SelectedKillMinutes = "00";
            SelectedKillSeconds = "00";
            IgnoreLimits = false;
        }
    }
}
