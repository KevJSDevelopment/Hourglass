public class LimiterSettingsForm : Form
{
    private TabControl tabControl;
    private TabPage messagesTab;
    private TabPage warningTab;
    private TabPage generalTab;
    private TabPage advancedTab;

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

    private CheckBox enableSoundCheckBox;
    private ComboBox themeComboBox;
    private CheckBox startWithWindowsCheckBox;

    private Button saveButton;
    private Button cancelButton;

    public LimiterSettingsForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        // Initialize and set up all the controls
        // ...
    }

    private void LoadSettings()
    {
        // Load current settings from database or config file
        // Populate the form controls with the loaded settings
    }

    private void SaveSettings()
    {
        // Save the settings from the form controls to database or config file
    }

    // Event handlers for various controls
    private void AddMessageButton_Click(object sender, EventArgs e)
    {
        // Add new message logic
    }

    private void AddAudioButton_Click(object sender, EventArgs e)
    {
        // Add new audio file logic
    }

    private void AddGoalButton_Click(object sender, EventArgs e)
    {
        // Add new goal logic
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
        SaveSettings();
        this.Close();
    }

    private void CancelButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    // ... Other event handlers and helper methods
}