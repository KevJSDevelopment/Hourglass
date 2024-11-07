using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using Microsoft.Win32;
using NAudio.Wave;
using ProcessLimiterManager.WPF.Views;
using ProcessLimitManager.WPF.Commands;

namespace ProcessLimiterManager.WPF.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly SettingsRepository _settingsRepo;
        private readonly string _computerId;
        private string _newMessageText = string.Empty;
        private string _newGoalText = string.Empty;
        private MotivationalMessage _selectedMessage;
        private MotivationalMessage _selectedAudio;
        private MotivationalMessage _selectedGoal;
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;

        public ObservableCollection<MotivationalMessage> Messages { get; } = new();
        public ObservableCollection<MotivationalMessage> AudioFiles { get; } = new();
        public ObservableCollection<MotivationalMessage> Goals { get; } = new();

        public string NewMessageText
        {
            get => _newMessageText;
            set => SetProperty(ref _newMessageText, value);
        }

        public string NewGoalText
        {
            get => _newGoalText;
            set => SetProperty(ref _newGoalText, value);
        }

        public MotivationalMessage SelectedMessage
        {
            get => _selectedMessage;
            set => SetProperty(ref _selectedMessage, value);
        }

        public MotivationalMessage SelectedAudio
        {
            get => _selectedAudio;
            set => SetProperty(ref _selectedAudio, value);
        }

        public MotivationalMessage SelectedGoal
        {
            get => _selectedGoal;
            set => SetProperty(ref _selectedGoal, value);
        }

        // Commands
        public ICommand AddMessageCommand { get; }
        public ICommand EditMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand AddAudioCommand { get; }
        public ICommand PlayAudioCommand { get; }
        public ICommand DeleteAudioCommand { get; }
        public ICommand AddGoalCommand { get; }
        public ICommand EditGoalCommand { get; }
        public ICommand DeleteGoalCommand { get; }
        public ICommand SaveCommand { get; }

        public SettingsViewModel(string computerId)
        {
            _computerId = computerId;
            _messageRepo = new MotivationalMessageRepository();
            _settingsRepo = new SettingsRepository(computerId);

            // Initialize commands
            AddMessageCommand = new AsyncRelayCommand(_ => AddMessage(), _ => !string.IsNullOrWhiteSpace(NewMessageText));
            EditMessageCommand = new AsyncRelayCommand(_ => EditMessage(), _ => SelectedMessage != null);
            DeleteMessageCommand = new AsyncRelayCommand(_ => DeleteMessage(), _ => SelectedMessage != null);
            AddAudioCommand = new AsyncRelayCommand(_ => AddAudio());
            PlayAudioCommand = new RelayCommand(_ => PlayAudio(), _ => SelectedAudio != null);
            DeleteAudioCommand = new AsyncRelayCommand(_ => DeleteAudio(), _ => SelectedAudio != null);
            AddGoalCommand = new AsyncRelayCommand(_ => AddGoal(), _ => !string.IsNullOrWhiteSpace(NewGoalText));
            EditGoalCommand = new AsyncRelayCommand(_ => EditGoal(), _ => SelectedGoal != null);
            DeleteGoalCommand = new AsyncRelayCommand(_ => DeleteGoal(), _ => SelectedGoal != null);
            SaveCommand = new AsyncRelayCommand(_ => Save());

            LoadSettings();
        }

        private async void LoadSettings()
        {
            var messages = await _messageRepo.GetMessagesForComputer(_computerId);
            foreach (var message in messages)
            {
                switch (message.TypeId)
                {
                    case 1:
                        Messages.Add(message);
                        break;
                    case 2:
                        AudioFiles.Add(message);
                        break;
                    case 3:
                        Goals.Add(message);
                        break;
                }
            }
        }

        private async Task AddMessage()
        {
            int messageId = await _messageRepo.AddMessage(_computerId, NewMessageText);
            if (messageId > 0)
            {
                Messages.Add(new MotivationalMessage { Id = messageId, Message = NewMessageText, TypeId = 1 });
                NewMessageText = string.Empty;
            }
        }

        private async Task EditMessage()
        {
            if (SelectedMessage == null) return;

            var dialog = new EditMessageWindow(SelectedMessage.Message);
            if (dialog.ShowDialog() == true)
            {
                SelectedMessage.Message = dialog.UpdatedMessage;
                await _messageRepo.UpdateMessage(SelectedMessage);
                int index = Messages.IndexOf(SelectedMessage);
                Messages.RemoveAt(index);
                Messages.Insert(index, SelectedMessage);
            }
        }

        private async Task DeleteMessage()
        {
            if (SelectedMessage != null && await _messageRepo.DeleteMessage(SelectedMessage.Id))
            {
                Messages.Remove(SelectedMessage);
            }
        }

        private async Task AddAudio()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav"
            };

            if (dialog.ShowDialog() == true)
            {
                using var stream = File.OpenRead(dialog.FileName);
                var newAudio = await _messageRepo.AddAudioMessage(
                    _computerId,
                    stream,
                    Path.GetFileNameWithoutExtension(dialog.FileName),
                    Path.GetExtension(dialog.FileName));

                if (newAudio != null)
                {
                    AudioFiles.Add(newAudio);
                }
            }
        }

        private void PlayAudio()
        {
            if (SelectedAudio?.FilePath == null) return;

            try
            {
                StopAudio();
                audioFile = new AudioFileReader(SelectedAudio.FilePath);
                outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing audio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StopAudio();
            }
        }

        private void StopAudio()
        {
            outputDevice?.Stop();
            outputDevice?.Dispose();
            outputDevice = null;
            audioFile?.Dispose();
            audioFile = null;
        }

        private async Task DeleteAudio()
        {
            if (SelectedAudio != null && await _messageRepo.DeleteMessage(SelectedAudio.Id))
            {
                AudioFiles.Remove(SelectedAudio);
            }
        }

        private async Task AddGoal()
        {
            int messageId = await _messageRepo.AddGoalMessage(_computerId, NewGoalText);
            if (messageId > 0)
            {
                Goals.Add(new MotivationalMessage { Id = messageId, Message = NewGoalText, TypeId = 3 });
                NewGoalText = string.Empty;
            }
        }

        private async Task EditGoal()
        {
            if (SelectedGoal == null) return;

            var dialog = new EditMessageWindow(SelectedGoal.Message);
            if (dialog.ShowDialog() == true)
            {
                SelectedGoal.Message = dialog.UpdatedMessage;
                await _messageRepo.UpdateMessage(SelectedGoal);
                int index = Goals.IndexOf(SelectedGoal);
                Goals.RemoveAt(index);
                Goals.Insert(index, SelectedGoal);
            }
        }

        private async Task DeleteGoal()
        {
            if (SelectedGoal != null && await _messageRepo.DeleteMessage(SelectedGoal.Id))
            {
                Goals.Remove(SelectedGoal);
            }
        }

        private async Task Save()
        {
            // Save any general settings here
            await Task.CompletedTask;
        }

        public void Cleanup()
        {
            StopAudio();
        }
    }
}
