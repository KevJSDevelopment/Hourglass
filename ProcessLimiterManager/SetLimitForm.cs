namespace ProcessLimiterManager
{
    public partial class MainForm
    {
        public class SetLimitForm : Form
        {
            private TextBox txtWarningTime;
            private TextBox txtKillTime;

            public string WarningTime { get; private set; }
            public string KillTime { get; private set; }

            public SetLimitForm(string processName, string currentWarningTime, string currentKillTime)
            {
                Text = $"Set Limits for {processName}";

                var lblWarning = new Label { Text = "Warning Time (hh:mm:ss):", Left = 10, Top = 10 };
                txtWarningTime = new TextBox { Text = currentWarningTime, Left = 10, Top = 30, Width = 100 };

                var lblKill = new Label { Text = "Kill Time (hh:mm:ss):", Left = 10, Top = 60 };
                txtKillTime = new TextBox { Text = currentKillTime, Left = 10, Top = 80, Width = 100 };

                var btnOk = new Button { Text = "OK", Left = 10, Top = 110, DialogResult = DialogResult.OK };
                btnOk.Click += BtnOk_Click;

                var btnCancel = new Button { Text = "Cancel", Left = 100, Top = 110, DialogResult = DialogResult.Cancel };

                Controls.AddRange(new Control[] { lblWarning, txtWarningTime, lblKill, txtKillTime, btnOk, btnCancel });

                AcceptButton = btnOk;
                CancelButton = btnCancel;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                Size = new System.Drawing.Size(250, 200);
            }

            private void BtnOk_Click(object sender, EventArgs e)
            {
                if (txtWarningTime.Text != null && txtKillTime.Text != null)
                {
                    WarningTime = txtWarningTime.Text;
                    KillTime = txtKillTime.Text;
                }
                else
                {
                    MessageBox.Show("Invalid time format. Please use hh:mm:ss.");
                    DialogResult = DialogResult.None;
                }
            }
        }
    }
}