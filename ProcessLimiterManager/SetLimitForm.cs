namespace ProcessLimiterManager
{
    public class SetLimitForm : Form
    {
        private TextBox txtWarningTimeHours;
        private TextBox txtWarningTimeMinutes;
        private TextBox txtWarningTimeSeconds;
        private TextBox txtKillTimeHours;
        private TextBox txtKillTimeMinutes;
        private TextBox txtKillTimeSeconds;

        public string WarningTime { get; private set; }
        public string KillTime { get; private set; }

        public SetLimitForm(string processName, string currentWarningTime, string currentKillTime)
        {
            Text = $"Set Limits for {processName}";

            var lblWarning = new Label { Text = "Warning Time (hh:mm:ss):", Left = 10, Top = 10 };
            txtWarningTimeHours = new TextBox { Text = currentWarningTime, Left = 10, Top = 30, Width = 100 };
            txtWarningTimeMinutes = new TextBox { Text = currentWarningTime, Left = 10, Top = 30, Width = 100 };
            txtWarningTimeSeconds = new TextBox { Text = currentWarningTime, Left = 10, Top = 30, Width = 100 };

            var lblKill = new Label { Text = "Kill Time (hh:mm:ss):", Left = 10, Top = 60 };
            txtKillTimeHours = new TextBox { Text = currentKillTime, Left = 10, Top = 80, Width = 100 };
            txtKillTimeMinutes = new TextBox { Text = currentKillTime, Left = 10, Top = 80, Width = 100 };
            txtKillTimeSeconds = new TextBox { Text = currentKillTime, Left = 10, Top = 80, Width = 100 };

            var btnOk = new Button { Text = "OK", Left = 10, Top = 110, DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button { Text = "Cancel", Left = 100, Top = 110, DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { lblWarning, txtWarningTimeHours, txtWarningTimeMinutes, txtWarningTimeSeconds, lblKill, txtKillTimeHours, txtKillTimeMinutes, txtKillTimeSeconds, btnOk, btnCancel });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Size = new System.Drawing.Size(250, 200);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (txtWarningTimeHours.Text != null || txtWarningTimeMinutes.Text != null || txtWarningTimeSeconds.Text != null
                && txtKillTimeHours.Text + ":" + txtKillTimeMinutes.Text + ":" + txtKillTimeSeconds.Text != null)
            {
                WarningTime = txtWarningTimeHours.Text + ":" + txtWarningTimeMinutes.Text + ":" + txtWarningTimeSeconds.Text;
                KillTime = txtKillTimeHours.Text + ":" + txtKillTimeMinutes.Text + ":" + txtKillTimeSeconds.Text;
            }
            else
            {
                MessageBox.Show("Invalid time format. Please use hh:mm:ss.");
                DialogResult = DialogResult.None;
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if(txtWarningTimeHours.Text != null || txtWarningTimeMinutes.Text != null || txtWarningTimeSeconds.Text != null 
                && txtKillTimeHours.Text + ":" + txtKillTimeMinutes.Text + ":" + txtKillTimeSeconds.Text != null)
            {
               WarningTime = "00:00:00";
               KillTime = "00:00:00"; 
            }
        }
    }
}
