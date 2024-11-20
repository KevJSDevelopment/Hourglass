using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using Microsoft.Win32;
using NAudio.Wave;
using ProcessLimitManager.WPF.Commands;
using ProcessLimitManager.WPF.Views;

namespace ProcessLimitManager.WPF.ViewModels
{
    public class ManageMessagesViewModel : ViewModelBase
    {
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly string _computerId;
        private MotivationalMessage _selectedMessage;
        private string _newMessageText = string.Empty;
        private string _selectedAudioFile = string.Empty;
        private bool _isTextMessageSelected = true;
        private bool _isAudioMessageSelected;
        private bool _isGoalMessageSelected;
        private WaveOutEvent _outputDevice;
        private AudioFileReader _audioFile;

        public ObservableCollection<MotivationalMessage> Messages { get; } = new();

        public MotivationalMessage SelectedMessage
        {
            get => _selectedMessage;
            set => SetProperty(ref _selectedMessage, value);
        }

        public string NewMessageText
        {
            get => _newMessageText;
            set
            {
                if (SetProperty(ref _newMessageText, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SelectedAudioFile
        {
            get => _selectedAudioFile;
            set
            {
                if (SetProperty(ref _selectedAudioFile, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsTextMessageSelected
        {
            get => _isTextMessageSelected;
            set
            {
                if (SetProperty(ref _isTextMessageSelected, value))
                {
                    OnPropertyChanged(nameof(ShowTextInput));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsAudioMessageSelected
        {
            get => _isAudioMessageSelected;
            set
            {
                if (SetProperty(ref _isAudioMessageSelected, value))
                {
                    OnPropertyChanged(nameof(ShowAudioInput));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsGoalMessageSelected
        {
            get => _isGoalMessageSelected;
            set
            {
                if (SetProperty(ref _isGoalMessageSelected, value))
                {
                    OnPropertyChanged(nameof(ShowTextInput));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool ShowTextInput => IsTextMessageSelected || IsGoalMessageSelected;
        public bool ShowAudioInput => IsAudioMessageSelected;

        // Commands
        public ICommand AddMessageCommand { get; }
        public ICommand EditMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand PlayAudioCommand { get; }
        public ICommand SelectAudioFileCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ManageMessagesViewModel(string computerId)
        {
            _computerId = computerId;
            _messageRepo = new MotivationalMessageRepository();

            // Initialize commands
            AddMessageCommand = new AsyncRelayCommand(AddMessage, CanAddMessage);
            EditMessageCommand = new AsyncRelayCommand(EditMessage, _ => SelectedMessage?.TypeId != 2);
            DeleteMessageCommand = new AsyncRelayCommand(DeleteMessage, _ => SelectedMessage != null);
            PlayAudioCommand = new RelayCommand(_ => PlayAudio(), _ => SelectedMessage?.TypeId == 2);
            SelectAudioFileCommand = new AsyncRelayCommand(_ => SelectAudioFile());
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            LoadMessages();
        }

        private bool CanAddMessage(object _)
        {
            if (IsAudioMessageSelected)
                return !string.IsNullOrEmpty(SelectedAudioFile);
            return !string.IsNullOrEmpty(NewMessageText);
        }

        private async void LoadMessages()
        {
            var messages = await _messageRepo.GetMessagesForComputer(_computerId);
            Messages.Clear();
            foreach (var message in messages)
            {
                Messages.Add(message);
            }
        }

        private async Task AddMessage(object _)
        {
            try
            {
                if (IsAudioMessageSelected)
                {
                    await AddAudioMessage();
                }
                else
                {
                    int typeId = IsTextMessageSelected ? 1 : 3;
                    int messageId = typeId == 1
                        ? await _messageRepo.AddMessage(_computerId, NewMessageText)
                        : await _messageRepo.AddGoalMessage(_computerId, NewMessageText);

                    if (messageId > 0)
                    {
                        Messages.Add(new MotivationalMessage
                        {
                            Id = messageId,
                            Message = NewMessageText,
                            TypeId = typeId,
                            TypeDescription = typeId == 1 ? "Text" : "Goal"
                        });
                        NewMessageText = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding message: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddAudioMessage()
        {
            try
            {
                using var stream = File.OpenRead(SelectedAudioFile);
                var newAudio = await _messageRepo.AddAudioMessage(
                    _computerId,
                    stream,
                    Path.GetFileNameWithoutExtension(SelectedAudioFile),
                    Path.GetExtension(SelectedAudioFile));

                if (newAudio != null)
                {
                    Messages.Add(newAudio);
                    SelectedAudioFile = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding audio file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SelectAudioFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedAudioFile = dialog.FileName;
            }
        }

        private async Task EditMessage(object _)
        {
            if (SelectedMessage == null || SelectedMessage.TypeId == 2) return;

            var dialog = new EditMessageWindow(SelectedMessage.Message);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    SelectedMessage.Message = dialog.UpdatedMessage;
                    await _messageRepo.UpdateMessage(SelectedMessage);
                    int index = Messages.IndexOf(SelectedMessage);
                    Messages.RemoveAt(index);
                    Messages.Insert(index, SelectedMessage);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating message: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteMessage(object _)
        {
            if (SelectedMessage == null) return;

            var result = MessageBox.Show("Are you sure you want to delete this message?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (await _messageRepo.DeleteMessage(SelectedMessage.Id))
                    {
                        Messages.Remove(SelectedMessage);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting message: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PlayAudio()
        {
            if (SelectedMessage?.TypeId != 2 || string.IsNullOrEmpty(SelectedMessage.FilePath)) return;

            try
            {
                StopAudio();
                _audioFile = new AudioFileReader(SelectedMessage.FilePath);
                _outputDevice = new WaveOutEvent();
                _outputDevice.Init(_audioFile);
                _outputDevice.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing audio: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StopAudio();
            }
        }

        private void StopAudio()
        {
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _outputDevice = null;
            _audioFile?.Dispose();
            _audioFile = null;
        }

        private void Save()
        {
            DialogResult = true;
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        private bool? DialogResult
        {
            set
            {
                if (Window.GetWindow(Application.Current.MainWindow) is Window window)
                {
                    window.DialogResult = value;
                }
            }
        }

        public void Cleanup()
        {
            StopAudio();
        }
    }
}