using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LimiterMessaging
{
    public partial class MainForm : Form
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Task.Run(ListenForMessages);
        }

        private async Task ListenForMessages()
        {
            while (true)
            {
                using (var server = new NamedPipeServerStream("LimiterMessagingPipe", PipeDirection.In))
                {
                    await server.WaitForConnectionAsync();

                    using (var reader = new StreamReader(server))
                    {
                        string message = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(message))
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                ShowWarningMessage(message);
                            });
                        }
                    }
                }
            }
        }

        private void ShowWarningMessage(string message)
        {
            MessageBox.Show(this, message, "Usage Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}