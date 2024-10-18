public class EditMessageForm : Form
{
    private TextBox messageTextBox;
    private Button okButton;
    private Button cancelButton;

    public string UpdatedMessage { get; private set; }

    public EditMessageForm(string currentMessage)
    {
        InitializeComponent();
        messageTextBox.Text = currentMessage;
    }

    private void InitializeComponent()
    {
        this.Size = new System.Drawing.Size(300, 150);
        this.Text = "Edit Message";

        messageTextBox = new TextBox { Multiline = true, Size = new System.Drawing.Size(260, 60), Location = new System.Drawing.Point(10, 10) };
        okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(120, 80) };
        cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(200, 80) };

        okButton.Click += OkButton_Click;

        this.Controls.AddRange(new Control[] { messageTextBox, okButton, cancelButton });
        this.AcceptButton = okButton;
        this.CancelButton = cancelButton;
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
        UpdatedMessage = messageTextBox.Text;
    }
}