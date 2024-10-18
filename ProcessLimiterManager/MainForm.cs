using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using System.IO;
using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;

namespace ProcessLimiterManager
{
    public partial class MainForm : Form
    {
        private HashSet<string> addedExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<ProcessInfo> applications = new List<ProcessInfo>();
        private ListView listViewApplications;
        private Button btnRefresh;
        private Button btnSetLimits;
        private Button btnAddApplication;
        private Button btnRemoveApplication;
        private Button btnOpenSettings;
        private AppRepository _appRepository;
        private string _computerId;

        public MainForm()
        {
            _appRepository = new AppRepository();
            _computerId = ComputerIdentifier.GetUniqueIdentifier();
            SetupComponent();
            _ = LoadApplications();
        }

        private void SetupComponent()
        {
            this.Text = "Application Limiter Manager";
            this.Size = new System.Drawing.Size(800, 600);

            listViewApplications = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listViewApplications.Columns.Add("Application Name", 300);
            listViewApplications.Columns.Add("Executable", 200);
            listViewApplications.Columns.Add("Warning Time", 100);
            listViewApplications.Columns.Add("Kill Time", 100);

            btnRefresh = new Button
            {
                Text = "Refresh",
                Dock = DockStyle.Bottom
            };
            btnRefresh.Click += btnRefresh_Click;

            btnSetLimits = new Button
            {
                Text = "Set Limits",
                Dock = DockStyle.Bottom
            };
            btnSetLimits.Click += btnSetLimits_Click;

            btnAddApplication = new Button
            {
                Text = "Add Application",
                Dock = DockStyle.Bottom
            };
            btnAddApplication.Click += btnAddApplication_Click;

            btnRemoveApplication = new Button
            {
                Text = "Remove Application",
                Dock = DockStyle.Bottom
            };
            btnRemoveApplication.Click += btnRemoveApplication_Click;

            btnOpenSettings = new Button
            {
                Text = "Settings",
                Dock = DockStyle.Bottom
            };
            btnOpenSettings.Click += BtnOpenSettings_Click;
           

            this.Controls.Add(listViewApplications);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnSetLimits);
            this.Controls.Add(btnAddApplication);
            this.Controls.Add(btnRemoveApplication);
            this.Controls.Add(btnOpenSettings);
        }

        private async Task LoadApplications()
        {
            var computerId = ComputerIdentifier.GetUniqueIdentifier();

            listViewApplications.Items.Clear();
            applications.Clear();
            addedExecutables.Clear();

            applications = await _appRepository.LoadAllLimits(computerId);

            foreach (var app in applications)
            {
                addedExecutables.Add(app.Executable);
                var item = new ListViewItem(app.Name);
                item.SubItems.Add(app.Executable);
                item.SubItems.Add(app.WarningTime);
                item.SubItems.Add(app.KillTime);
                listViewApplications.Items.Add(item);
            }
        }

        private async Task AddApplication(string name, string executable)
        {
            if (addedExecutables.Add(executable))
            {
                var newApp = new ProcessInfo
                {
                    Name = name,
                    Executable = executable,
                    WarningTime = "00:00:00",
                    KillTime = "00:00:00",
                    ComputerId = ComputerIdentifier.GetUniqueIdentifier(),
                };
                await _appRepository.SaveLimits(newApp);
                applications.Add(newApp);
            }
        }

        private async Task RemoveApplication(string executable)
        {
            await _appRepository.DeleteApp(executable);
            applications.RemoveAll(a => a.Executable == executable);
            addedExecutables.Remove(executable);
        }

        private async void btnAddApplication_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe";
                openFileDialog.Title = "Select an Application";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string name = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    await AddApplication(name, openFileDialog.FileName);
                    await LoadApplications();
                }
            }
        }

        private void BtnOpenSettings_Click(object sender, EventArgs e)
        {
            OpenSettingsForm();
        }

        private void OpenSettingsForm()
        {
            using (var settingsForm = new LimiterSettingsForm(_computerId))
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Refresh the main form if necessary
                    RefreshMainForm();
                }
            }
        }

        private void RefreshMainForm()
        {
            // Reload any data or update UI elements that might have changed in settings
            // For example:
            // LoadAppLimits();
            // UpdateUI();
        }
        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadApplications();
        }

        private async void btnSetLimits_Click(object sender, EventArgs e)
        {
            if (listViewApplications.SelectedItems.Count > 0)
            {
                var selectedApp = applications[listViewApplications.SelectedIndices[0]];
                using (var limitForm = new SetLimitForm(selectedApp.Name, selectedApp.WarningTime, selectedApp.KillTime))
                {
                    if (limitForm.ShowDialog() == DialogResult.OK)
                    {
                        selectedApp.WarningTime = limitForm.WarningTime;
                        selectedApp.KillTime = limitForm.KillTime;

                        await _appRepository.SaveLimits(selectedApp);

                        listViewApplications.SelectedItems[0].SubItems[2].Text = selectedApp.WarningTime;
                        listViewApplications.SelectedItems[0].SubItems[3].Text = selectedApp.KillTime;

                        MessageBox.Show("Limits saved successfully.");
                    }
                }
            }
        }

        private async void btnRemoveApplication_Click(object sender, EventArgs e)
        {
            if (listViewApplications.SelectedItems.Count > 0)
            {
                var selectedApp = applications[listViewApplications.SelectedIndices[0]];
                await RemoveApplication(selectedApp.Executable);
                await LoadApplications();
            }
        }
    }
}