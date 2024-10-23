namespace ProcessLimiterManager
{
    public class SetLimitForm : Form
    {
        private ComboBox cmbWarningTimeHours;
        private ComboBox cmbWarningTimeMinutes;
        private ComboBox cmbWarningTimeSeconds;
        private ComboBox cmbKillTimeHours;
        private ComboBox cmbKillTimeMinutes;
        private ComboBox cmbKillTimeSeconds;
        private CheckBox checkBoxIgnore;

        public string WarningTime { get; private set; }
        public string KillTime { get; private set; }
        public bool Ignore {  get; private set; }

        public SetLimitForm(string processName, string currentWarningTime, string currentKillTime, bool ignore)
        {
            Text = $"Set Limits for {processName}";
            Size = new System.Drawing.Size(400, 350);

            var lblWarning = new Label { Text = "Warning Time:", Left = 10, Top = 15, Width = 100 };

            var lblWarningHours = new Label { Text = "Hours:", Left = 10, Top = 35, Width = 40 };
            cmbWarningTimeHours = new ComboBox { Left = 55, Top = 32, Width = 50, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblWarningMinutes = new Label { Text = "Minutes:", Left = 115, Top = 35, Width = 50 };
            cmbWarningTimeMinutes = new ComboBox { Left = 170, Top = 32, Width = 50, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblWarningSeconds = new Label { Text = "Seconds:", Left = 230, Top = 35, Width = 50 };
            cmbWarningTimeSeconds = new ComboBox { Left = 285, Top = 32, Width = 50, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblKill = new Label { Text = "Kill Time:", Left = 10, Top = 70, Width = 100 };

            var lblKillHours = new Label { Text = "Hours:", Left = 10, Top = 95, Width = 40 };
            cmbKillTimeHours = new ComboBox { Left = 55, Top = 92, Width = 50, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblKillMinutes = new Label { Text = "Minutes:", Left = 115, Top = 95, Width = 50 };
            cmbKillTimeMinutes = new ComboBox { Left = 170, Top = 92, Width = 50, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblKillSeconds = new Label { Text = "Seconds:", Left = 230, Top = 95, Width = 50 };
            cmbKillTimeSeconds = new ComboBox { Left = 285, Top = 92, Width = 50, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblIgnore = new Label { Text = "Ignore Limit", Left = 10, Top = 120, Width = 100 };
            checkBoxIgnore = new CheckBox { Checked = ignore, Left = 115, Top = 120, Width = 40 };
            PopulateComboBoxes();

            var btnOk = new Button { Text = "OK", Left = 10, Top = 140, Width = 75, DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button { Text = "Cancel", Left = 100, Top = 140, Width = 75, DialogResult = DialogResult.Cancel };

            var btnReset = new Button { Text = "Reset", Left = 190, Top = 140, Width = 75 };
            btnReset.Click += BtnReset_Click;

            Controls.AddRange(new Control[] {
                lblWarning, lblWarningHours, cmbWarningTimeHours, lblWarningMinutes, cmbWarningTimeMinutes, lblWarningSeconds, cmbWarningTimeSeconds,
                lblKill, lblKillHours, cmbKillTimeHours, lblKillMinutes, cmbKillTimeMinutes, lblKillSeconds, cmbKillTimeSeconds, lblIgnore, checkBoxIgnore,
                btnOk, btnCancel, btnReset
            });

            SetInitialValues(currentWarningTime, currentKillTime);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
        }

        private void PopulateComboBoxes()
        {
            for (int i = 0; i <= 48; i++)
            {
                string value = i.ToString("D2");
                cmbWarningTimeHours.Items.Add(value);
                cmbKillTimeHours.Items.Add(value);
            }

            for (int i = 0; i <= 60; i++)
            {
                string value = i.ToString("D2");
                cmbWarningTimeMinutes.Items.Add(value);
                cmbWarningTimeSeconds.Items.Add(value);
                cmbKillTimeMinutes.Items.Add(value);
                cmbKillTimeSeconds.Items.Add(value);
            }
        }

        private void SetInitialValues(string warningTime, string killTime)
        {
            SetComboBoxValues(cmbWarningTimeHours, cmbWarningTimeMinutes, cmbWarningTimeSeconds, warningTime);
            SetComboBoxValues(cmbKillTimeHours, cmbKillTimeMinutes, cmbKillTimeSeconds, killTime);
        }

        private void SetComboBoxValues(ComboBox hours, ComboBox minutes, ComboBox seconds, string time)
        {
            var parts = time.Split(':');
            if (parts.Length == 3)
            {
                hours.SelectedItem = parts[0];
                minutes.SelectedItem = parts[1];
                seconds.SelectedItem = parts[2];
            }
            else
            {
                hours.SelectedIndex = 0;
                minutes.SelectedIndex = 0;
                seconds.SelectedIndex = 0;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            WarningTime = $"{cmbWarningTimeHours.SelectedItem}:{cmbWarningTimeMinutes.SelectedItem}:{cmbWarningTimeSeconds.SelectedItem}";
            KillTime = $"{cmbKillTimeHours.SelectedItem}:{cmbKillTimeMinutes.SelectedItem}:{cmbKillTimeSeconds.SelectedItem}";
            Ignore = checkBoxIgnore.Checked;
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            cmbWarningTimeHours.SelectedIndex = 0;
            cmbWarningTimeMinutes.SelectedIndex = 0;
            cmbWarningTimeSeconds.SelectedIndex = 0;
            cmbKillTimeHours.SelectedIndex = 0;
            cmbKillTimeMinutes.SelectedIndex = 0;
            cmbKillTimeSeconds.SelectedIndex = 0;
            checkBoxIgnore.Checked = false;
            WarningTime = "00:00:00";
            KillTime = "00:00:00";
            Ignore = false;
        }
    }
}