namespace StealthPrompt;

public sealed class ApiKeyForm : Form
{
    private readonly TextBox _apiKey = new()
    {
        UseSystemPasswordChar = true,
        PlaceholderText = "sk-...",
        Dock = DockStyle.Fill
    };

    public ApiKeyForm(string provider)
    {
        Text = $"{provider.ToUpperInvariant()} API Key";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 150);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        root.Controls.Add(new Label
        {
            Text = $"Paste your {provider.ToUpperInvariant()} API key. It will be saved in Windows Credential Manager.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        root.Controls.Add(_apiKey, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        var save = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);

        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(root);
    }

    public string ApiKey => _apiKey.Text.Trim();

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _apiKey.Focus();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(ApiKey))
        {
            MessageBox.Show(this, "Paste API key first.", "Missing API key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }
}
