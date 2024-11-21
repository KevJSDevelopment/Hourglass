﻿using System.Collections.ObjectModel;
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

        // Common Properties
        private MotivationalMessage _selectedMessage;
        public ObservableCollection<MotivationalMessage> Messages { get; } = new();

        // Text Message Properties
        private string _newMessageText = string.Empty;

        // Audio Message Properties
        private string _selectedAudioFile = string.Empty;

        // Goal Properties
        private string _newGoalText = string.Empty;
        private string _newStepText = string.Empty;
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

        public string NewGoalText
        {
            get => _newGoalText;
            set
            {
                if (SetProperty(ref _newGoalText, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public string NewStepText
        {
            get => _newStepText;
            set
            {
                if (SetProperty(ref _newStepText, value))
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
        public ICommand AddStepCommand { get; }
        public ICommand RemoveStepCommand { get; }
        public ICommand AddGoalCommand { get; }
        public ICommand EditMessageCommand { get; }
        public ICommand DeleteMessageCommand { get; }
        public ICommand PlayAudioCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

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
            AddStepCommand = new RelayCommand(AddStep, _ => !string.IsNullOrWhiteSpace(NewStepText));
            RemoveStepCommand = new RelayCommand(RemoveStep);
            AddGoalCommand = new AsyncRelayCommand(AddGoal, CanAddGoal);
            EditMessageCommand = new AsyncRelayCommand(EditMessage, _ => SelectedMessage?.TypeId != 2);
            DeleteMessageCommand = new AsyncRelayCommand(DeleteMessage, _ => SelectedMessage != null);
            PlayAudioCommand = new RelayCommand(_ => PlayAudio(), _ => SelectedMessage?.TypeId == 2);
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            LoadMessages();
        }

        // Load Messages
        private async void LoadMessages()
        {
            try
            {
                var messages = await _messageRepo.GetMessagesForComputer(_computerId);
                Messages.Clear();
                foreach (var message in messages)
                {
                    Messages.Add(message);
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
                    Messages.Add(newAudio);
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
        private void AddStep(object _)
        {
            if (string.IsNullOrWhiteSpace(NewStepText)) return;

            CurrentGoalSteps.Add(new GoalStep
            {
                Index = CurrentGoalSteps.Count + 1,
                Text = NewStepText
            });
            NewStepText = string.Empty;
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveStep(object parameter)
        {
            if (parameter is GoalStep step)
            {
                CurrentGoalSteps.Remove(step);
                // Update indices
                for (int i = 0; i < CurrentGoalSteps.Count; i++)
                {
                    CurrentGoalSteps[i].Index = i + 1;
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool CanAddGoal(object _)
        {
            return !string.IsNullOrWhiteSpace(NewGoalText) && CurrentGoalSteps.Any();
        }

        private async Task AddGoal(object _)
        {
            try
            {
                string formattedGoal = FormatGoalWithSteps();
                int messageId = await _messageRepo.AddGoalMessage(_computerId, formattedGoal);

                if (messageId > 0)
                {
                    Messages.Add(new MotivationalMessage
                    {
                        Id = messageId,
                        Message = formattedGoal,
                        TypeId = 3,
                        TypeDescription = "Goal"
                    });

                    // Clear inputs
                    NewGoalText = string.Empty;
                    CurrentGoalSteps.Clear();
                    NewStepText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding goal: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatGoalWithSteps()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Goal: {NewGoalText}");

            if (CurrentGoalSteps.Any())
            {
                sb.AppendLine("\nSteps to achieve this goal:");
                foreach (var step in CurrentGoalSteps)
                {
                    sb.AppendLine($"{step.Index}. {step.Text}");
                }
            }

            return sb.ToString();
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

        private void Save()
        {
            if (Window.GetWindow(Application.Current.MainWindow) is Window window)
            {
                window.DialogResult = true;
            }
        }

        private void Cancel()
        {
            if (Window.GetWindow(Application.Current.MainWindow) is Window window)
            {
                window.DialogResult = false;
            }
        }

        public void Cleanup()
        {
            _audioService?.Dispose();
        }
    }
}