using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using LimiterMessaging.WPF.Services;
using LimiterMessaging.WPF.Views;
using System.Windows.Input;
using System.Windows;
using LimiterMessaging.WPF.ViewModels;
using LimiterMessaging.WPF.Commands;
using System.Windows.Threading;

public class MessagingViewModel : ViewModelBase, IDisposable
{
    private readonly string _processName;
    private readonly AppRepository _appRepo;
    private readonly MotivationalMessageRepository _messageRepo;
    private readonly SettingsRepository _settingsRepo;
    private readonly MotivationalMessage _currentMessage;
    private readonly List<MotivationalMessage> _messagesSent;
    private readonly Dictionary<string, bool> _ignoreStatusCache;
    private readonly string _computerId;
    private readonly string _timerWarning;
    private readonly AudioService _audioService;
    private string _displayMessage;
    private bool _showAudioControls;
    private readonly DispatcherTimer _timer;
    private double _currentPosition;
    private double _duration;
    private bool _isPlaying;
    public double CurrentPosition
    {
        get => _currentPosition;
        set => SetProperty(ref _currentPosition, value);
    }

    public double Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        set => SetProperty(ref _isPlaying, value);
    }

    public string CurrentPositionText => TimeSpan.FromSeconds(CurrentPosition).ToString(@"m\:ss");
    public string DurationText => TimeSpan.FromSeconds(Duration).ToString(@"m\:ss");
    public string DisplayMessage
    {
        get => _displayMessage;
        set => SetProperty(ref _displayMessage, value);
    }

    public bool ShowAudioControls
    {
        get => _showAudioControls;
        set => SetProperty(ref _showAudioControls, value);
    }

    public ICommand OkCommand { get; }
    public ICommand IgnoreLimitsCommand { get; }
    public ICommand PlayAudioCommand { get; }
    public ICommand PauseAudioCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand SeekCommand { get; }

    public MessagingViewModel(
        MotivationalMessage message,
        string timerWarning,
        string processName,
        string computerId,
        Dictionary<string, bool> ignoreStatusCache,
        AppRepository appRepo,
        MotivationalMessageRepository messageRepo,
        SettingsRepository settingsRepo,
        List<MotivationalMessage> messagesSent)
    {
        _currentMessage = message;
        _timerWarning = timerWarning;
        _processName = processName;
        _computerId = computerId;
        _ignoreStatusCache = ignoreStatusCache;
        _appRepo = appRepo;
        _messageRepo = messageRepo;
        _settingsRepo = settingsRepo;
        _messagesSent = messagesSent ?? new List<MotivationalMessage> { _currentMessage };
        _audioService = new AudioService();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;

        // Initialize commands with lambda expressions
        OkCommand = new AsyncRelayCommand(_ => OnOk(_));
        IgnoreLimitsCommand = new AsyncRelayCommand(_ => OnIgnoreLimits(_));
        PlayAudioCommand = new RelayCommand(_ => PlayAudio(), _ => _currentMessage?.TypeId == 2);
        PauseAudioCommand = new RelayCommand(_ => PauseAudio(), _ => _currentMessage?.TypeId == 2);

        PlayPauseCommand = new RelayCommand(_ => PlayPauseAudio());
        SeekCommand = new RelayCommand(param =>
        {
            if (param is double position)
            {
                _audioService.SeekToPosition(TimeSpan.FromSeconds(position));
            }
        });

        // Auto-play if it's an audio message
        if (_currentMessage.TypeId == 2)
        {
            PlayAudio();
        }


        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        switch (_currentMessage.TypeId)
        {
            case 1: // Text message
                DisplayMessage = $"{_currentMessage.Message}\n\n{_timerWarning}";
                ShowAudioControls = false;
                break;
            case 2: // Audio message
                DisplayMessage = $"Give this audio a listen before you decide to ignore your limits: \n\n{_currentMessage.FileName}";
                ShowAudioControls = true;
                break;
            case 3: // Goal message
                DisplayMessage = $"You have goals to achieve! Did you make progress on this today?: \n\n- {_currentMessage.Message}\n\n{_timerWarning}";
                ShowAudioControls = false;
                break;
        }
    }

    private async Task OnOk(object _)
    {
        try
        {
            await _appRepo.UpdateIgnoreStatus(_processName, false);
            _ignoreStatusCache.Clear();
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OnIgnoreLimits(object _)
    {
        try
        {
            int messageLimit = await _settingsRepo.GetMessageLimit();
            var messages = await _messageRepo.GetMessagesForComputer(_computerId);

            if (_messagesSent.Count < messageLimit - 1 && messages.Count > _messagesSent.Count)
            {
                Random r = new Random();
                MotivationalMessage message = new MotivationalMessage();
                int index = -1;
                while (_messagesSent.Contains(message) || index == -1)
                {
                    index = r.Next(0, messages.Count);
                    message = messages[index];
                }

                _messagesSent.Add(message);
                var newWindow = new MessagingWindow(message, _timerWarning, _processName, _computerId,
                    _ignoreStatusCache, _appRepo, _messageRepo, _settingsRepo, _messagesSent);
                newWindow.ShowDialog();
            }
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing ignore limits: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Timer_Tick(object sender, EventArgs e)
    {
        if (_audioService.AudioFile != null)
        {
            CurrentPosition = _audioService.CurrentTime.TotalSeconds;
            Duration = _audioService.TotalTime.TotalSeconds;
            OnPropertyChanged(nameof(CurrentPositionText));
            OnPropertyChanged(nameof(DurationText));
        }
    }

    private void PlayPauseAudio()
    {
        if (_isPlaying)
        {
            PauseAudio();
        }
        else
        {
            PlayAudio();
        }
    }

    private void PlayAudio()
    {
        try
        {
            if (_currentMessage.TypeId == 2 && !string.IsNullOrEmpty(_currentMessage.FilePath))
            {
                _audioService.PlayAudio(_currentMessage.FilePath);
                IsPlaying = true;
                _timer.Start();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error playing audio: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PauseAudio()
    {
        _audioService.PauseAudio();
        IsPlaying = false;
        _timer.Stop();
    }

    public event Action RequestClose;

    public void Dispose()
    {
        _timer?.Stop();
        _audioService?.Dispose();
    }
}