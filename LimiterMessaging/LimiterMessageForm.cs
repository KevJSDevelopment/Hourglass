using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using NAudio.Wave;

namespace LimiterMessaging
{
    public partial class LimiterMessagingForm : Form
    {
        private readonly string _processName;
        private readonly AppRepository _appRepo;
        private readonly MotivationalMessageRepository _messageRepo;
        private readonly SettingsRepository _settingsRepo;
        private readonly MotivationalMessage _currentMessage;
        private readonly Dictionary<string, bool> _ignoreStatusCache = new Dictionary<string, bool>();
        private readonly string _computerId;
        private readonly string _timerWarning;
        private readonly int _messageNumber;
        private WaveOutEvent _outputDevice;
        private AudioFileReader _audioFile;
        private Button PlayAudioBtn;
        private Button PauseAudioBtn;
        private Button okBtn;
        private Button ignoreLimitsBtn;
        private Label lblMessage;
        private bool _isPlaying = false;
        public LimiterMessagingForm(MotivationalMessage message, string timerWarning, string processName, string computerId, Dictionary<string, bool> ignoreStatusCache, AppRepository appRepo, MotivationalMessageRepository messageRepo, SettingsRepository settingsRepo, int messageNumber = 0)
        {
            _currentMessage = message;
            _timerWarning = timerWarning;
            _processName = processName;
            _computerId = computerId;
            _ignoreStatusCache = ignoreStatusCache;
            _appRepo = appRepo;
            _messageRepo = messageRepo;
            _settingsRepo = settingsRepo;
            _messageNumber = messageNumber;
            InitializeComponent();
            DisplayMessage();
        }

        private void InitializeComponent()
        {
            lblMessage = new Label();
            okBtn = new Button();
            ignoreLimitsBtn = new Button();
            PlayAudioBtn = new Button();
            PauseAudioBtn = new Button();
            SuspendLayout();
            // 
            // lblMessage
            // 
            lblMessage.Location = new Point(12, 10);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(279, 87);
            lblMessage.TabIndex = 0;
            lblMessage.Text = _currentMessage.Message;
            // 
            // okBtn
            // 
            okBtn.Location = new Point(241, 100);
            okBtn.Name = "okBtn";
            okBtn.Size = new Size(50, 25);
            okBtn.TabIndex = 1;
            okBtn.Text = "OK";
            okBtn.UseVisualStyleBackColor = true;
            okBtn.Click += OkBtn_Click;
            // 
            // ignoreLimitsBtn
            // 
            ignoreLimitsBtn.Location = new Point(10, 100);
            ignoreLimitsBtn.Name = "ignoreLimitsBtn";
            ignoreLimitsBtn.Size = new Size(100, 25);
            ignoreLimitsBtn.TabIndex = 2;
            ignoreLimitsBtn.Text = "Ignore Limits";
            ignoreLimitsBtn.UseVisualStyleBackColor = true;
            ignoreLimitsBtn.Click += IgnoreLimitsBtn_Click;
            // 
            // PlayAudioBtn
            // 
            PlayAudioBtn.Location = new Point(116, 100);
            PlayAudioBtn.Name = "PlayAudioBtn";
            PlayAudioBtn.Size = new Size(55, 23);
            PlayAudioBtn.TabIndex = 3;
            PlayAudioBtn.Text = "Play";
            PlayAudioBtn.UseVisualStyleBackColor = true;
            PlayAudioBtn.Visible = false;
            PlayAudioBtn.Click += PlayAudioBtn_Click;
            // 
            // PauseAudioBtn
            // 
            PauseAudioBtn.Location = new Point(177, 100);
            PauseAudioBtn.Name = "PauseAudioBtn";
            PauseAudioBtn.Size = new Size(55, 23);
            PauseAudioBtn.TabIndex = 4;
            PauseAudioBtn.Text = "Pause";
            PauseAudioBtn.UseVisualStyleBackColor = true;
            PauseAudioBtn.Click += PauseAudioBtn_Click;
            // 
            // LimiterMessagingForm
            // 
            ClientSize = new Size(303, 137);
            Controls.Add(PauseAudioBtn);
            Controls.Add(PlayAudioBtn);
            Controls.Add(ignoreLimitsBtn);
            Controls.Add(okBtn);
            Controls.Add(lblMessage);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LimiterMessagingForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Usage Warning";
            ResumeLayout(false);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            DisposeAudioResources();
        }

        private void DisposeAudioResources()
        {
            if (_outputDevice != null)
            {
                _outputDevice.Stop();
                _outputDevice.Dispose();
                _outputDevice = null;
            }
            if (_audioFile != null)
            {
                _audioFile.Dispose();
                _audioFile = null;
            }
        }

        private void DisplayMessage()
        {
            switch (_currentMessage.TypeId)
            {
                case 1: // Text message
                    lblMessage.Text = _currentMessage.Message + "\n" + _timerWarning;
                    PlayAudioBtn.Visible = false;
                    PauseAudioBtn.Visible = false;
                    break;
                case 2: // Audio message
                    lblMessage.Text = "Give this audio a listen before you decide to ignore your limits: \n" + _currentMessage.FileName;
                    PlayAudioBtn.Visible = true;
                    PauseAudioBtn.Visible = true;
                    break;
                case 3: // Goal message
                    lblMessage.Text = "You have goals to achieve! Did you make progress on this today?: \n" +
                        "- " + _currentMessage.Message + "\n" + _timerWarning;
                    PlayAudioBtn.Visible = false;
                    PauseAudioBtn.Visible = false;
                    break;
            }
        }

        private async void IgnoreLimitsBtn_Click(object sender, EventArgs e)
        {
            int messageLimit = await _settingsRepo.GetMessageLimit();

            if (_messageNumber < messageLimit - 1)
            {
                var messages = await _messageRepo.GetMessagesForComputer(_computerId);

                if (messages.Count > 0)
                {
                    Random r = new Random();
                    var message = messages[r.Next(0, messages.Count)];

                    var newForm = new LimiterMessagingForm(message, _timerWarning, _processName, _computerId, _ignoreStatusCache, _appRepo, _messageRepo, _settingsRepo, _messageNumber + 1);
                    newForm.ShowDialog();
                }
            }
            this.Close();
        }

        private async void OkBtn_Click(object sender, EventArgs e)
        {
            await _appRepo.UpdateIgnoreStatus(_processName, false);
            _ignoreStatusCache.Clear();
            this.Close();
        }

        private void PlayAudioBtn_Click(object sender, EventArgs e)
        {
            if (_currentMessage.TypeId == 2 && !string.IsNullOrEmpty(_currentMessage.FilePath))
            {
                try
                {
                    if (!_isPlaying)
                    {
                        // If we're starting fresh, create new instances
                        if (_outputDevice == null)
                        {
                            _outputDevice = new WaveOutEvent();
                            _audioFile = new AudioFileReader(_currentMessage.FilePath);
                            _outputDevice.Init(_audioFile);
                        }

                        _outputDevice.Play();
                        _isPlaying = true;
                        PlayAudioBtn.Text = "Resume";
                    }
                    else if (_outputDevice.PlaybackState == PlaybackState.Paused)
                    {
                        _outputDevice.Play();
                        _isPlaying = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing audio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DisposeAudioResources();
                }
            }
        }

        private void PauseAudioBtn_Click(object sender, EventArgs e)
        {
            if (_outputDevice != null && _isPlaying)
            {
                _outputDevice.Pause();
                _isPlaying = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeAudioResources();
            }
            base.Dispose(disposing);
        }

    }
}