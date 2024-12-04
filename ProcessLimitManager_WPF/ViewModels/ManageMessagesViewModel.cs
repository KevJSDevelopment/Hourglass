using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using AppLimiterLibrary.Services;
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
        private readonly AudioService _audioService;
        public event Action RequestClose;

        // Common Properties
        private MotivationalMessage _selectedMessage;
        public ObservableCollection<MotivationalMessage> Messages { get; } = new();
        public ObservableCollection<MotivationalMessage> AudioMessages { get; } = new();
        public ObservableCollection<MotivationalMessage> GoalMessages { get; } = new();

        // Text Message Properties
        private string _newMessageText = string.Empty;

        // Audio Message Properties
        private string _selectedAudioFile = string.Empty;

        // Goal Properties
        private ObservableCollection<GoalStep> _currentGoalSteps = new();

        // Properties
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
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public string SelectedAudioFile
        {
            get => _selectedAudioFile;
            set
            {
                if (SetProperty(ref _selectedAudioFile, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<GoalStep> CurrentGoalSteps
        {
            get => _currentGoalSteps;
            set => SetProperty(ref _currentGoalSteps, value);
        }

        // Commands
        public ICommand AddTextMessageCommand { get; }
        public ICommand SelectAudioFileCommand { get; }
        public ICommand AddAudioMessageCommand { get; }
        public ICommand ShowAddGoalDialogCommand { get; }
        public ICommand EditMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand PlayAudioCommand { get; }
        public ICommand ExitCommand { get; }

        // Constructor
        public ManageMessagesViewModel(string computerId)
        {
            _computerId = computerId;
            _messageRepo = new MotivationalMessageRepository();
            _audioService = new AudioService();

            // Initialize commands
            AddTextMessageCommand = new AsyncRelayCommand(AddTextMessage, _ => !string.IsNullOrWhiteSpace(NewMessageText));
            SelectAudioFileCommand = new AsyncRelayCommand(SelectAudioFile);
            AddAudioMessageCommand = new AsyncRelayCommand(AddAudioMessage, _ => !string.IsNullOrWhiteSpace(SelectedAudioFile));
            ShowAddGoalDialogCommand = new AsyncRelayCommand(ShowAddGoalDialog);
            EditMessageCommand = new AsyncRelayCommand(EditMessage, _ => SelectedMessage?.TypeId != 2);
            DeleteMessageCommand = new AsyncRelayCommand(DeleteMessage, _ => SelectedMessage != null);
            PlayAudioCommand = new RelayCommand(_ => PlayAudio(), _ => SelectedMessage?.TypeId == 2);
            ExitCommand = new RelayCommand(_ => Exit());

            LoadMessages();
        }

        // Load Messages
        private async void LoadMessages()
        {
            try
            {
                var messages = await _messageRepo.GetMessagesForComputer(_computerId);
                Messages.Clear();
                AudioMessages.Clear();
                GoalMessages.Clear();
                foreach (var message in messages)
                {
                    if(message.TypeId == 1) Messages.Add(message);
                    else if(message.TypeId == 2) AudioMessages.Add(message);
                    else GoalMessages.Add(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading messages: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Text Message Methods
        private async Task AddTextMessage(object _)
        {
            try
            {
                int messageId = await _messageRepo.AddMessage(_computerId, NewMessageText);
                if (messageId > 0)
                {
                    Messages.Add(new MotivationalMessage
                    {
                        Id = messageId,
                        Message = NewMessageText,
                        TypeId = 1,
                        TypeDescription = "Text"
                    });
                    NewMessageText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding text message: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Audio Message Methods
        private async Task SelectAudioFile(object _)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav",
                Title = "Select Audio File"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedAudioFile = dialog.FileName;
            }
        }

        private async Task AddAudioMessage(object _)
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
                    AudioMessages.Add(newAudio);
                    SelectedAudioFile = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding audio message: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlayAudio()
        {
            if (SelectedMessage?.TypeId != 2 || string.IsNullOrEmpty(SelectedMessage.FilePath)) return;

            try
            {
                _audioService.PlayAudio(SelectedMessage.FilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing audio: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Goal Methods
        private async Task ShowAddGoalDialog(object _)
        {
            var editGoalWindow = new EditGoalWindow(_messageRepo, _computerId, async () => RefreshMessages());
            editGoalWindow.ShowDialog();
        }

        // Common Methods
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

            var result = MessageBox.Show(
                "Are you sure you want to delete this message?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

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

        private void Exit()
        {
            RequestClose?.Invoke();
        }

        public void Cleanup()
        {
            _audioService?.Dispose();
        }

        public async Task RefreshMessages()
        {
            try
            {
                var messages = await _messageRepo.GetMessagesForComputer(_computerId);
                Messages.Clear();
                AudioMessages.Clear();
                GoalMessages.Clear();
                foreach (var message in messages)
                {
                    if (message.TypeId == 1) Messages.Add(message);
                    else if (message.TypeId == 2) AudioMessages.Add(message);
                    else GoalMessages.Add(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing messages: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}