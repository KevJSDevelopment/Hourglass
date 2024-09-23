using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LimiterMessaging
{
    public partial class LimiterMessagingForm : Form
    {
        public LimiterMessagingForm(string message)
        {
            // Set up the form
            this.Text = "Usage Warning";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create and set up a label to display the message
            Label lblMessage = new Label
            {
                Text = message,
                AutoSize = true,
                Location = new System.Drawing.Point(10, 10),
                MaximumSize = new System.Drawing.Size(280, 0)
            };

            // Create and set up an OK button
            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(100, lblMessage.Bottom + 20)
            };
            btnOk.Click += (sender, e) => this.Close();

            // Add controls to the form
            this.Controls.Add(lblMessage);
            this.Controls.Add(btnOk);

            // Set the form size
            this.ClientSize = new System.Drawing.Size(300, btnOk.Bottom + 10);

            // Show the form
            this.ShowDialog();
        }
    }
}