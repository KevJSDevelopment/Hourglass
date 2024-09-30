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
            Size = new System.Drawing.Size(300, 250);  // Increased form size

            var lblWarning = new Label { Text = "Warning Time:", Left = 10, Top = 10, Width = 100 };

            var lblWarningHours = new Label { Text = "Hours:", Left = 10, Top = 35, Width = 40 };
            txtWarningTimeHours = new TextBox { Left = 55, Top = 32, Width = 30 };

            var lblWarningMinutes = new Label { Text = "Minutes:", Left = 95, Top = 35, Width = 50 };
            txtWarningTimeMinutes = new TextBox { Left = 150, Top = 32, Width = 30 };

            var lblWarningSeconds = new Label { Text = "Seconds:", Left = 190, Top = 35, Width = 50 };
            txtWarningTimeSeconds = new TextBox { Left = 245, Top = 32, Width = 30 };

            var lblKill = new Label { Text = "Kill Time:", Left = 10, Top = 70, Width = 100 };

            var lblKillHours = new Label { Text = "Hours:", Left = 10, Top = 95, Width = 40 };
            txtKillTimeHours = new TextBox { Left = 55, Top = 92, Width = 30 };

            var lblKillMinutes = new Label { Text = "Minutes:", Left = 95, Top = 95, Width = 50 };
            txtKillTimeMinutes = new TextBox { Left = 150, Top = 92, Width = 30 };

            var lblKillSeconds = new Label { Text = "Seconds:", Left = 190, Top = 95, Width = 50 };
            txtKillTimeSeconds = new TextBox { Left = 245, Top = 92, Width = 30 };

            var btnOk = new Button { Text = "OK", Left = 10, Top = 140, Width = 75, DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button { Text = "Cancel", Left = 100, Top = 140, Width = 75, DialogResult = DialogResult.Cancel };

            var btnReset = new Button { Text = "Reset", Left = 190, Top = 140, Width = 75 };
            btnReset.Click += BtnReset_Click;

            Controls.AddRange(new Control[] {
                lblWarning, lblWarningHours, txtWarningTimeHours, lblWarningMinutes, txtWarningTimeMinutes, lblWarningSeconds, txtWarningTimeSeconds,
                lblKill, lblKillHours, txtKillTimeHours, lblKillMinutes, txtKillTimeMinutes, lblKillSeconds, txtKillTimeSeconds,
                btnOk, btnCancel, btnReset
            });

            // Set initial values
            var warningTimeParts = currentWarningTime.Split(':');
            var killTimeParts = currentKillTime.Split(':');

            if (warningTimeParts.Length == 3)
            {
                txtWarningTimeHours.Text = warningTimeParts[0];
                txtWarningTimeMinutes.Text = warningTimeParts[1];
                txtWarningTimeSeconds.Text = warningTimeParts[2];
            }

            if (killTimeParts.Length == 3)
            {
                txtKillTimeHours.Text = killTimeParts[0];
                txtKillTimeMinutes.Text = killTimeParts[1];
                txtKillTimeSeconds.Text = killTimeParts[2];
            }

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtWarningTimeHours.Text, out int wh) &&
                int.TryParse(txtWarningTimeMinutes.Text, out int wm) &&
                int.TryParse(txtWarningTimeSeconds.Text, out int ws) &&
                int.TryParse(txtKillTimeHours.Text, out int kh) &&
                int.TryParse(txtKillTimeMinutes.Text, out int km) &&
                int.TryParse(txtKillTimeSeconds.Text, out int ks))
            {
                WarningTime = $"{wh:D2}:{wm:D2}:{ws:D2}";
                KillTime = $"{kh:D2}:{km:D2}:{ks:D2}";
            }
            else
            {
                MessageBox.Show("Invalid time format. Please enter valid numbers for hours, minutes, and seconds.");
                DialogResult = DialogResult.None;
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtWarningTimeHours.Text = "00";
            txtWarningTimeMinutes.Text = "00";
            txtWarningTimeSeconds.Text = "00";
            txtKillTimeHours.Text = "00";
            txtKillTimeMinutes.Text = "00";
            txtKillTimeSeconds.Text = "00";
            WarningTime = "00:00:00";
            KillTime = "00:00:00";
        }
    }
}
