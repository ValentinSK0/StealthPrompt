namespace StealthPrompt;

public sealed class SettingsForm : Form
{
    private readonly TextBox _apiKey = new() { UseSystemPasswordChar = true };
    private readonly Button _saveApiKey = new() { Text = "Save API key" };
    private readonly Label _apiKeyStatus = new() { TextAlign = ContentAlignment.MiddleLeft };
    private readonly ComboBox _provider = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _hotkey = new();
    private readonly TextBox _hrdbHotkey = new();
    private readonly TextBox _hrdbPath = new();
    private readonly TextBox _model = new();
    private readonly NumericUpDown _timeout = new() { Minimum = 5000, Maximum = 120000, Increment = 1000 };
    private readonly CheckBox _preserveClipboard = new() { Text = "Preserve clipboard during capture" };
    private readonly CheckBox _showToast = new() { Text = "Show status notifications" };
    private readonly CheckBox _debugMode = new() { Text = "Debug Alt+K before sending" };

    public SettingsForm(AppSettings settings)
    {
        Settings = settings;
        Text = "Stealth Prompt Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 430);

        _provider.Items.AddRange(["groq", "openai"]);
        LoadSettings(settings);
        BuildLayout();
    }

    public AppSettings Settings { get; }

    private void LoadSettings(AppSettings settings)
    {
        _provider.SelectedItem = settings.Provider;
        if (_provider.SelectedIndex < 0)
        {
            _provider.SelectedIndex = 0;
        }
        _provider.SelectedIndexChanged += (_, _) => ApplyProviderDefaults();

        _apiKey.PlaceholderText = CredentialStore.HasApiKey(settings.Provider) ? "API key already saved; paste new key to replace" : "gsk_...";
        _apiKeyStatus.Text = CredentialStore.HasApiKey(settings.Provider) ? "Saved" : "Missing";
        _hotkey.Text = settings.Hotkey;
        _hrdbHotkey.Text = settings.HrdbHotkey;
        _hrdbPath.Text = settings.HrdbPath;
        _model.Text = settings.Model;

        _timeout.Value = Math.Clamp(settings.TimeoutMs, (int)_timeout.Minimum, (int)_timeout.Maximum);
        _preserveClipboard.Checked = settings.PreserveClipboard;
        _showToast.Checked = settings.ShowToast;
        _debugMode.Checked = settings.DebugMode;
    }

    private void ApplyProviderDefaults()
    {
        var provider = _provider.Text;
        _apiKey.PlaceholderText = provider == "groq" ? "gsk_..." : "sk-...";
        _apiKeyStatus.Text = CredentialStore.HasApiKey(provider) ? "Saved" : "Missing";

        if (provider == "groq" && _model.Text.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase))
        {
            _model.Text = "llama-3.3-70b-versatile";
        }
        else if (provider == "openai" && _model.Text.StartsWith("llama-", StringComparison.OrdinalIgnoreCase))
        {
            _model.Text = "gpt-5.5";
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 11,
            Padding = new Padding(12),
            AutoSize = false
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddApiKeyRow(root, 0);
        AddRow(root, 1, "Provider", _provider, 32);
        AddRow(root, 2, "Hotkey", _hotkey, 32);
        AddRow(root, 3, "HRDB hotkey", _hrdbHotkey, 32);
        AddRow(root, 4, "HRDB path", _hrdbPath, 32);
        AddRow(root, 5, "Model", _model, 32);
        AddRow(root, 6, "Timeout ms", _timeout, 32);
        AddRow(root, 7, "", _preserveClipboard, 32);
        AddRow(root, 8, "", _showToast, 32);
        AddRow(root, 9, "", _debugMode, 32);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 0),
            WrapContents = false
        };
        var save = StyleButton(new Button { Text = "Save", DialogResult = DialogResult.OK }, 104);
        var cancel = StyleButton(new Button { Text = "Cancel", DialogResult = DialogResult.Cancel }, 104);
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.Controls.Add(buttons, 0, 10);
        root.SetColumnSpan(buttons, 2);

        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(root);
    }

    private void SaveApiKeyFromField()
    {
        if (string.IsNullOrWhiteSpace(_apiKey.Text))
        {
            MessageBox.Show(this, "Paste API key first.", "Missing API key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            CredentialStore.SaveApiKey(_apiKey.Text.Trim(), _provider.Text);
            _apiKey.Clear();
            _apiKeyStatus.Text = "Saved";
            MessageBox.Show(this, "API key saved.", "Stealth Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save API key", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            try
            {
                HotkeyParser.Parse(_hotkey.Text);
                HotkeyParser.Parse(_hrdbHotkey.Text);
                Settings.Provider = _provider.Text.Trim();
                Settings.Hotkey = _hotkey.Text.Trim();
                Settings.HrdbHotkey = _hrdbHotkey.Text.Trim();
                Settings.HrdbPath = _hrdbPath.Text.Trim();
                Settings.Model = _model.Text.Trim();
                Settings.TimeoutMs = (int)_timeout.Value;
                Settings.PreserveClipboard = _preserveClipboard.Checked;
                Settings.ShowToast = _showToast.Checked;
                Settings.DebugMode = _debugMode.Checked;

                if (string.IsNullOrWhiteSpace(Settings.Model))
                {
                    throw new InvalidOperationException("Model cannot be empty.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Invalid settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }

    private void AddApiKeyRow(TableLayoutPanel root, int row)
    {
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.Controls.Add(new Label
        {
            Text = "API key",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

        _apiKey.Dock = DockStyle.Fill;
        StyleButton(_saveApiKey, 118);
        _saveApiKey.Dock = DockStyle.Fill;
        _apiKeyStatus.Dock = DockStyle.Fill;
        _saveApiKey.Click += (_, _) => SaveApiKeyFromField();

        panel.Controls.Add(_apiKey, 0, 0);
        panel.Controls.Add(_saveApiKey, 1, 0);
        panel.Controls.Add(_apiKeyStatus, 2, 0);
        root.Controls.Add(panel, 1, row);
    }

    private static Button StyleButton(Button button, int width)
    {
        button.Width = width;
        button.Height = 32;
        button.Margin = new Padding(8, 0, 0, 0);
        button.Padding = new Padding(8, 2, 8, 2);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(70, 120, 190);
        button.BackColor = Color.White;
        button.ForeColor = Color.Black;
        button.UseVisualStyleBackColor = false;
        return button;
    }

    private static void AddRow(TableLayoutPanel root, int row, string label, Control control, int height)
    {
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        root.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        control.Dock = DockStyle.Fill;
        root.Controls.Add(control, 1, row);
    }
}
