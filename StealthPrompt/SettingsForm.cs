namespace StealthPrompt;

public sealed class SettingsForm : Form
{
    private readonly TextBox _groqApiKey = new() { UseSystemPasswordChar = true, PlaceholderText = "gsk_..." };
    private readonly TextBox _geminiApiKey = new() { UseSystemPasswordChar = true, PlaceholderText = "AIza..." };
    private readonly Button _saveGroqApiKey = new() { Text = "Save" };
    private readonly Button _saveGeminiApiKey = new() { Text = "Save" };
    private readonly Label _groqApiKeyStatus = new() { TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _geminiApiKeyStatus = new() { TextAlign = ContentAlignment.MiddleLeft };
    private readonly ComboBox _provider = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _hotkey = new();
    private readonly TextBox _hrdbHotkey = new();
    private readonly TextBox _hrdbPath = new();
    private readonly ComboBox _model = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _timeout = new() { Minimum = 5000, Maximum = 120000, Increment = 1000 };
    private readonly CheckBox _preserveClipboard = new() { Text = "Preserve clipboard during capture" };
    private readonly CheckBox _showToast = new() { Text = "Show status notifications" };
    private readonly CheckBox _debugMode = new() { Text = "Debug Alt+K before sending" };
    private static readonly string[] GroqModels =
    [
        "allam-2-7b",
        "groq/compound",
        "groq/compound-mini",
        "llama-3.1-8b-instant",
        "llama-3.3-70b-versatile",
        "meta-llama/llama-4-scout-17b-16e-instruct",
        "meta-llama/llama-prompt-guard-2-22m",
        "meta-llama/llama-prompt-guard-2-86m",
        "openai/gpt-oss-120b",
        "openai/gpt-oss-20b",
        "openai/gpt-oss-safeguard-20b",
        "qwen/qwen3-32b",
    ];

    public SettingsForm(AppSettings settings)
    {
        Settings = settings;
        Text = "Stealth Prompt Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 460);

        _provider.Items.AddRange(["groq", "gemini"]);
        _model.Items.AddRange(GroqModels);
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

        UpdateApiKeyStatus("groq", _groqApiKey, _groqApiKeyStatus);
        UpdateApiKeyStatus("gemini", _geminiApiKey, _geminiApiKeyStatus);
        _hotkey.Text = settings.Hotkey;
        _hrdbHotkey.Text = settings.HrdbHotkey;
        _hrdbPath.Text = settings.HrdbPath;
        SetSelectedModel(settings.Model);
        ApplyProviderDefaults();

        _timeout.Value = Math.Clamp(settings.TimeoutMs, (int)_timeout.Minimum, (int)_timeout.Maximum);
        _preserveClipboard.Checked = settings.PreserveClipboard;
        _showToast.Checked = settings.ShowToast;
        _debugMode.Checked = settings.DebugMode;
    }

    private void ApplyProviderDefaults()
    {
        var provider = _provider.Text;

        if (provider == "groq")
        {
            _model.Enabled = true;
            if (_model.Items.Count != GroqModels.Length)
            {
                _model.Items.Clear();
                _model.Items.AddRange(GroqModels);
            }

            if (_model.SelectedIndex < 0 || _model.Text.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase))
            {
                SetSelectedModel("llama-3.3-70b-versatile");
            }
        }
        else if (provider == "gemini")
        {
            _model.Items.Clear();
            _model.Items.Add("gemini-2.5-flash");
            _model.SelectedIndex = 0;
            _model.Enabled = false;
        }
    }

    private void SetSelectedModel(string model)
    {
        _model.SelectedItem = model;
        if (_model.SelectedIndex < 0)
        {
            _model.SelectedItem = "llama-3.3-70b-versatile";
        }
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 12,
            Padding = new Padding(12),
            AutoSize = false
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddApiKeyRow(root, 0, "Groq API key", "groq", _groqApiKey, _saveGroqApiKey, _groqApiKeyStatus);
        AddApiKeyRow(root, 1, "Gemini API key", "gemini", _geminiApiKey, _saveGeminiApiKey, _geminiApiKeyStatus);
        AddRow(root, 2, "Provider", _provider, 32);
        AddRow(root, 3, "Hotkey", _hotkey, 32);
        AddRow(root, 4, "HRDB hotkey", _hrdbHotkey, 32);
        AddRow(root, 5, "HRDB path", _hrdbPath, 32);
        AddRow(root, 6, "Model", _model, 32);
        AddRow(root, 7, "Timeout ms", _timeout, 32);
        AddRow(root, 8, "", _preserveClipboard, 32);
        AddRow(root, 9, "", _showToast, 32);
        AddRow(root, 10, "", _debugMode, 32);

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
        root.Controls.Add(buttons, 0, 11);
        root.SetColumnSpan(buttons, 2);

        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(root);
    }

    private static void UpdateApiKeyStatus(string provider, TextBox apiKey, Label status)
    {
        var saved = CredentialStore.HasApiKey(provider);
        apiKey.PlaceholderText = saved ? "API key already saved; paste new key to replace" : apiKey.PlaceholderText;
        status.Text = saved ? "Saved" : "Missing";
    }

    private void SaveApiKeyFromField(string provider, TextBox apiKey, Label status)
    {
        if (string.IsNullOrWhiteSpace(apiKey.Text))
        {
            MessageBox.Show(this, "Paste API key first.", "Missing API key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            CredentialStore.SaveApiKey(apiKey.Text.Trim(), provider);
            apiKey.Clear();
            status.Text = "Saved";
            apiKey.PlaceholderText = "API key already saved; paste new key to replace";
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
                Settings.Model = Settings.Provider.Equals("gemini", StringComparison.OrdinalIgnoreCase)
                    ? "gemini-2.5-flash"
                    : _model.Text.Trim();
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

    private void AddApiKeyRow(TableLayoutPanel root, int row, string label, string provider, TextBox apiKey, Button saveApiKey, Label apiKeyStatus)
    {
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        root.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));

        apiKey.Dock = DockStyle.Fill;
        StyleButton(saveApiKey, 68);
        saveApiKey.Dock = DockStyle.Fill;
        apiKeyStatus.Dock = DockStyle.Fill;
        saveApiKey.Click += (_, _) => SaveApiKeyFromField(provider, apiKey, apiKeyStatus);

        panel.Controls.Add(apiKey, 0, 0);
        panel.Controls.Add(saveApiKey, 1, 0);
        panel.Controls.Add(apiKeyStatus, 2, 0);
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
