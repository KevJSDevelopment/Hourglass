using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LimiterMessaging
{
    public partial class LimiterMessagingForm : Form
    {
        private string _processName;
        private Label lblMessage;
        private Button okBtn;
        private Button ignoreLimitsBtn;

        public LimiterMessagingForm(string message, string processName)
        {
            _processName = processName;
            InitializeComponent();
            lblMessage.Text = message;
        }

        private void InitializeComponent()
        {
            lblMessage = new Label();
            okBtn = new Button();
            ignoreLimitsBtn = new Button();
            SuspendLayout();
            // 
            // lblMessage
            // 
            lblMessage.Location = new Point(10, 10);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(281, 87);
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
            // LimiterMessagingForm
            // 
            ClientSize = new Size(303, 137);
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

        private void IgnoreLimitsBtn_Click(object sender, EventArgs e)
        {
            using (var client = new NamedPipeClientStream(".", "AppLimiterPipe", PipeDirection.Out))
            {
                try
                {
                    client.Connect(1000); // Wait up to 1 second
                    using (var writer = new StreamWriter(client))
                    {
                        writer.WriteLine($"IGNORE:{_processName}");
                        writer.Flush();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to communicate with AppLimiter: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            this.Close();
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}