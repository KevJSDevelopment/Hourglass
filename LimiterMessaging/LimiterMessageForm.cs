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
        private Button button1;
        private Button button2;
        private string _message;
        public LimiterMessagingForm(string message, string processName)
        {
            _processName = processName;
            _message = message;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            lblMessage = new Label();
            button1 = new Button();
            button2 = new Button();
            SuspendLayout();
            // 
            // lblMessage
            // 
            lblMessage.Location = new Point(10, 10);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(280, 0);
            lblMessage.TabIndex = 0;
            lblMessage.Text = _message;
            // 
            // button1
            // 
            button1.Location = new Point(263, 113);
            button1.Name = "button1";
            button1.Size = new Size(50, 25);
            button1.TabIndex = 1;
            button1.Text = "OK";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(10, 113);
            button2.Name = "button2";
            button2.Size = new Size(100, 25);
            button2.TabIndex = 2;
            button2.Text = "Ignore Limits";
            button2.UseVisualStyleBackColor = true;
            // 
            // LimiterMessagingForm
            // 
            ClientSize = new Size(325, 150);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(lblMessage);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LimiterMessagingForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Usage Warning";
            ResumeLayout(false);
        }

        private void IgnoreLimits_Click(object sender, EventArgs e)
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
    }
}