using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using NAudio.Wave;

namespace LimiterMessaging
{
    public partial class LimiterMessagingForm : Form
    {
        private string _processName;
        private Label lblMessage;
        private Button okBtn;
        private Button ignoreLimitsBtn;
        private AppRepository _appRepo;
        private MotivationalMessageRepository _messageRepo;
        private SettingsRepository _settingsRepo;
        private MotivationalMessage _currentMessage;
        private string _computerId;
        private Button PlayAudioBtn;
        private int _messageNumber;
        public LimiterMessagingForm(MotivationalMessage message, string processName, int messageNumber = 0)
        {
            _computerId = ComputerIdentifier.GetUniqueIdentifier();
            _appRepo = new AppRepository();
            _messageRepo = new MotivationalMessageRepository();
            _settingsRepo = new SettingsRepository(_computerId);
            _processName = processName;
            _currentMessage = message;  
            _messageNumber = messageNumber;
            InitializeComponent();
            lblMessage.Text = message.Message;
            DisplayMessage(message);
        }

        private void InitializeComponent()
        {
            lblMessage = new Label();
            okBtn = new Button();
            ignoreLimitsBtn = new Button();
            PlayAudioBtn = new Button();
            SuspendLayout();
            // 
            // lblMessage
            // 
            lblMessage.Location = new Point(12, 10);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(279, 87);
            lblMessage.TabIndex = 0;
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
            PlayAudioBtn.Location = new Point(136, 102);
            PlayAudioBtn.Name = "PlayAudioBtn";
            PlayAudioBtn.Size = new Size(75, 23);
            PlayAudioBtn.TabIndex = 3;
            PlayAudioBtn.Text = "Play Audio";
            PlayAudioBtn.UseVisualStyleBackColor = true;
            PlayAudioBtn.Visible = false;
            PlayAudioBtn.Click += PlayAudioBtn_Click;
            // 
            // LimiterMessagingForm
            // 
            ClientSize = new Size(303, 137);
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

        private void DisplayMessage(MotivationalMessage message)
        {
            switch (message.TypeId)
            {
                case 1: // Text message
                case 3: // Goal message
                    lblMessage.Text = message.Message;
                    break;
                case 2: // Audio message
                    lblMessage.Text = "Audio message available";
                    Controls["playAudioBtn"].Visible = true;
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

                    var newForm = new LimiterMessagingForm(message, _processName, _messageNumber + 1);
                    newForm.ShowDialog();
                }
            }
            else
            {
                await _appRepo.UpdateIgnoreStatus(_processName, true);
            }

            this.Close();
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void PlayAudioBtn_Click(object sender, EventArgs e)
        {
            if (_currentMessage.TypeId == 2 && !string.IsNullOrEmpty(_currentMessage.FilePath))
            {
                try
                {
                    using (var audioFile = new AudioFileReader(_currentMessage.FilePath))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing audio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}