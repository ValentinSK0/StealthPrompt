namespace StealthPrompt;

public sealed class DebugPromptForm : Form
{
    public DebugPromptForm(string selectedText, string prompt)
    {
        Text = "Stealth Prompt Debug";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        ClientSize = new Size(760, 560);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        root.Controls.Add(new Label { Text = "Captured selected text", Dock = DockStyle.Fill }, 0, 0);
        root.Controls.Add(ReadOnlyBox(selectedText), 0, 1);
        root.Controls.Add(new Label { Text = "Prompt sent to OpenAI", Dock = DockStyle.Fill }, 0, 2);
        root.Controls.Add(ReadOnlyBox(prompt), 0, 3);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill
        };
        var send = new Button { Text = "Send to GPT", DialogResult = DialogResult.OK, Width = 110 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        buttons.Controls.Add(send);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 4);

        AcceptButton = send;
        CancelButton = cancel;
        Controls.Add(root);
    }

    private static TextBox ReadOnlyBox(string text) => new()
    {
        Text = text,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Dock = DockStyle.Fill
    };
}
