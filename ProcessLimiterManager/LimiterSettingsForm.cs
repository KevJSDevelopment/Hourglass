using System;
using System.Windows.Forms;
using System.IO;
using NAudio.Wave;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using System.Collections.Generic;

namespace ProcessLimiterManager
{
    partial class LimiterSettingsForm : Form
    {
        private TabControl tabControl;
        private TabPage messagesTab, audioTab, goalsTab, generalTab;

        private TextBox newMessageTextBox;
        private Button addMessageButton;
        private ListBox messageListBox;
        private Button editMessageButton;
        private Button deleteMessageButton;

        private Button addAudioButton;
        private ListBox audioListBox;
        private Button playAudioButton;
        private Button deleteAudioButton;

        private TextBox newGoalTextBox;
        private Button addGoalButton;
        private ListBox goalListBox;
        private Button editGoalButton;
        private Button deleteGoalButton;

        private NumericUpDown warningCountNumeric;

        private Button saveButton;
        private Button cancelButton;

        private string _computerId;
        private MotivationalMessageRepository _messageRepo;
        private SettingsRepository _settingsRepo;

        public LimiterSettingsForm(string computerId)
        {
            _computerId = computerId;
            _messageRepo = new MotivationalMessageRepository();
            _settingsRepo = new SettingsRepository(_computerId);

            SetupComponent();
            LoadSettings();
        }

        private void SetupComponent()
        {
            this.Text = "Limiter Settings";
            this.ClientSize = new System.Drawing.Size(600, 400);

            tabControl = new TabControl { Dock = DockStyle.Fill };

            // Messages Tab
            messagesTab = new TabPage("Messages");
            newMessageTextBox = new TextBox { Width = 300, Location = new System.Drawing.Point(10, 10) };
            addMessageButton = new Button { Text = "Add Message", Location = new System.Drawing.Point(320, 10) };
            messageListBox = new ListBox { Width = 300, Height = 200, Location = new System.Drawing.Point(10, 40) };
            editMessageButton = new Button { Text = "Edit", Location = new System.Drawing.Point(320, 40) };
            deleteMessageButton = new Button { Text = "Delete", Location = new System.Drawing.Point(320, 70) };

            addMessageButton.Click += AddMessageButton_Click;
            editMessageButton.Click += EditMessageButton_Click;
            deleteMessageButton.Click += DeleteMessageButton_Click;

            messagesTab.Controls.AddRange(new Control[] { newMessageTextBox, addMessageButton, messageListBox, editMessageButton, deleteMessageButton });

            // Audio Tab
            audioTab = new TabPage("Audio");
            addAudioButton = new Button { Text = "Add Audio", Location = new System.Drawing.Point(10, 10) };
            audioListBox = new ListBox { Width = 300, Height = 200, Location = new System.Drawing.Point(10, 40) };
            playAudioButton = new Button { Text = "Play", Location = new System.Drawing.Point(320, 40) };
            deleteAudioButton = new Button { Text = "Delete", Location = new System.Drawing.Point(320, 70) };

            addAudioButton.Click += AddAudioButton_Click;
            playAudioButton.Click += PlayAudioButton_Click;
            deleteAudioButton.Click += DeleteAudioButton_Click;

            audioTab.Controls.AddRange(new Control[] { addAudioButton, audioListBox, playAudioButton, deleteAudioButton });

            // Goals Tab
            goalsTab = new TabPage("Goals");
            newGoalTextBox = new TextBox { Width = 300, Location = new System.Drawing.Point(10, 10) };
            addGoalButton = new Button { Text = "Add Goal", Location = new System.Drawing.Point(320, 10) };
            goalListBox = new ListBox { Width = 300, Height = 200, Location = new System.Drawing.Point(10, 40) };
            editGoalButton = new Button { Text = "Edit", Location = new System.Drawing.Point(320, 40) };
            deleteGoalButton = new Button { Text = "Delete", Location = new System.Drawing.Point(320, 70) };

            addGoalButton.Click += AddGoalButton_Click;
            editGoalButton.Click += EditGoalButton_Click;
            deleteGoalButton.Click += DeleteGoalButton_Click;

            goalsTab.Controls.AddRange(new Control[] { newGoalTextBox, addGoalButton, goalListBox, editGoalButton, deleteGoalButton });

            // General Tab
            generalTab = new TabPage("General");
            warningCountNumeric = new NumericUpDown { Minimum = 0, Maximum = 10, Value = 3, Location = new System.Drawing.Point(150, 10) };
            Label warningCountLabel = new Label { Text = "Warning Count:", Location = new System.Drawing.Point(10, 12) };
            saveButton = new Button { Text = "Save", Location = new System.Drawing.Point(420, 320) };
            cancelButton = new Button { Text = "Cancel", Location = new System.Drawing.Point(500, 320) };

            saveButton.Click += SaveButton_Click;
            cancelButton.Click += CancelButton_Click;

            generalTab.Controls.AddRange(new Control[] { warningCountLabel, warningCountNumeric, saveButton, cancelButton });

            tabControl.TabPages.AddRange(new TabPage[] { messagesTab, audioTab, goalsTab, generalTab });

            this.Controls.AddRange(new Control[] { tabControl });
        }

        private async void LoadSettings()
        {
            var messages = await _messageRepo.GetMessagesForComputer(_computerId);
            foreach (var message in messages)
            {
                switch (message.TypeId)
                {
                    case 1:
                        messageListBox.Items.Add(message);
                        break;
                    case 2:
                        audioListBox.Items.Add(message);
                        break;
                    case 3:
                        goalListBox.Items.Add(message);
                        break;
                }
            }

            // Set the display member for each ListBox
            messageListBox.DisplayMember = "Message";
            audioListBox.DisplayMember = "FileName";
            goalListBox.DisplayMember = "Message";

            int warningCount = await _settingsRepo.GetMessageLimit();
            warningCountNumeric.Value = warningCount > 0 ? warningCount : 3;
        }

        private async void SaveSettings()
        {
            await _settingsRepo.SaveMessageLimit((int)warningCountNumeric.Value);
        }

        private async void AddMessageButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(newMessageTextBox.Text))
            {
                int messageId = await _messageRepo.AddMessage(_computerId, newMessageTextBox.Text);
                if (messageId > 0)
                {
                    messageListBox.Items.Add(new MotivationalMessage { Id = messageId, Message = newMessageTextBox.Text, TypeId = 1 });
                    newMessageTextBox.Clear();
                }
            }
        }

        private async void AddAudioButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    using (var stream = File.OpenRead(openFileDialog.FileName))
                    {
                        MotivationalMessage newAudioMessage = await _messageRepo.AddAudioMessage(_computerId, stream, Path.GetFileNameWithoutExtension(openFileDialog.FileName), Path.GetExtension(openFileDialog.FileName));
                        if (newAudioMessage != null)
                        {
                            audioListBox.Items.Add(newAudioMessage);
                        }
                        else
                        {
                            MessageBox.Show("Failed to add audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private async void AddGoalButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(newGoalTextBox.Text))
            {
                int messageId = await _messageRepo.AddGoalMessage(_computerId, newGoalTextBox.Text);
                if (messageId > 0)
                {
                    goalListBox.Items.Add(new MotivationalMessage { Id = messageId, Message = newGoalTextBox.Text, TypeId = 3 });
                    newGoalTextBox.Clear();
                }
            }
        }

        private async void EditMessageButton_Click(object sender, EventArgs e)
        {
            if (messageListBox.SelectedItem is MotivationalMessage selectedMessage)
            {
                using (var editForm = new EditMessageForm(selectedMessage.Message))
                {
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        selectedMessage.Message = editForm.UpdatedMessage;
                        int index = messageListBox.SelectedIndex;
                        messageListBox.Items.RemoveAt(index);
                        messageListBox.Items.Insert(index, selectedMessage);
                        await _messageRepo.UpdateMessage(selectedMessage);
                        messageListBox.Refresh();
                    }
                }
            }
        }

        private async void DeleteMessageButton_Click(object sender, EventArgs e)
        {
            if (messageListBox.SelectedItem is MotivationalMessage selectedMessage)
            {
                if (await _messageRepo.DeleteMessage(selectedMessage.Id))
                {
                    messageListBox.Items.Remove(selectedMessage);
                }
            }
        }

        private void PlayAudioButton_Click(object sender, EventArgs e)
        {
            if (audioListBox.SelectedItem is MotivationalMessage selectedAudio)
            {
                try
                {
                    using (var audioFile = new AudioFileReader(selectedAudio.FilePath))
                    using (var outputDevice = new WaveOutEvent())
                    {
                        outputDevice.Init(audioFile);
                        outputDevice.Play();
                        while (outputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            Application.DoEvents();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing audio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void DeleteAudioButton_Click(object sender, EventArgs e)
        {
            if (audioListBox.SelectedItem is MotivationalMessage selectedAudio)
            {
                if (await _messageRepo.DeleteMessage(selectedAudio.Id))
                {
                    audioListBox.Items.Remove(selectedAudio);
                }
            }
        }

        private async void EditGoalButton_Click(object sender, EventArgs e)
        {
            if (goalListBox.SelectedItem is MotivationalMessage selectedGoal)
            {
                using (var editForm = new EditMessageForm(selectedGoal.Message))
                {
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        selectedGoal.Message = editForm.UpdatedMessage;
                        int index = goalListBox.SelectedIndex;
                        goalListBox.Items.RemoveAt(index);
                        goalListBox.Items.Insert(index, selectedGoal);
                        await _messageRepo.UpdateMessage(selectedGoal);
                        goalListBox.Refresh(); // Refresh the ListBox to update the display
                    }
                }
            }
        }

        private async void DeleteGoalButton_Click(object sender, EventArgs e)
        {
            if (goalListBox.SelectedItem is MotivationalMessage selectedGoal)
            {
                if (await _messageRepo.DeleteMessage(selectedGoal.Id))
                {
                    goalListBox.Items.Remove(selectedGoal);
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

   
}