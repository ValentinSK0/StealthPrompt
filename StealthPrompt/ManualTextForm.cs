namespace StealthPrompt;

public sealed class ManualTextForm : Form
{
    private readonly TextBox _text = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Dock = DockStyle.Fill
    };

    public ManualTextForm(string detail)
    {
        Text = "Paste Text Manually";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        ClientSize = new Size(700, 420);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        root.Controls.Add(new Label
        {
            Text = detail + "\r\nPress Ctrl+C yourself in the source app, then paste here and click Send.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        root.Controls.Add(_text, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        var send = new Button { Text = "Send", DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(send);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 2);

        AcceptButton = send;
        CancelButton = cancel;
        Controls.Add(root);
    }

    public string EnteredText => _text.Text.Trim();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(EnteredText))
        {
            MessageBox.Show(this, "Paste or type text first.", "Missing text", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }

        base.OnFormClosing(e);
    }
}
