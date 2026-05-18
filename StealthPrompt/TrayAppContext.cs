namespace StealthPrompt;

public sealed class TrayAppContext : ApplicationContext
{
    private readonly ConfigStore _configStore = new();
    private readonly ClipboardSelectionReader _clipboard = new();
    private readonly OpenAiClient _openAi = new();
    private readonly PromptLogStore _logs = new();
    private readonly Hotkey _normalHotkey = new(0x534B);
    private readonly Hotkey _hrdbHotkey = new(0x534C);
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _statusItem;
    private AppSettings _settings;
    private bool _enabled = true;
    private bool _busy;

    public TrayAppContext()
    {
        _settings = _configStore.Load();
        _enabledItem = new ToolStripMenuItem("Enabled") { Checked = true };
        _enabledItem.Click += (_, _) => ToggleEnabled();
        _statusItem = new ToolStripMenuItem("Ready") { Enabled = false };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_enabledItem);
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Toggle debug mode", null, (_, _) => ToggleDebugMode());
        menu.Items.Add("Set API key", null, (_, _) => ShowApiKeySetup());
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add("Logs", null, (_, _) => OpenLogs());
        menu.Items.Add("Open config folder", null, (_, _) => OpenConfigFolder());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Stealth Prompt",
            ContextMenuStrip = menu,
            Visible = _settings.TrayIcon
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _normalHotkey.Pressed += async (_, _) => await HandleHotkeyAsync(false);
        _hrdbHotkey.Pressed += async (_, _) => await HandleHotkeyAsync(true);
        RegisterHotkeys();

        if (!CredentialStore.HasApiKey(_settings.Provider))
        {
            _notifyIcon.Visible = true;
            Application.Idle += ShowFirstRunApiKeySetup;
        }
    }

    private async Task HandleHotkeyAsync(bool includeHrdb)
    {
        if (!_enabled || _busy)
        {
            return;
        }

        _busy = true;
        SetStatus(includeHrdb ? "Working with HRDB..." : "Working...");
        var hotkeyName = includeHrdb ? _settings.HrdbHotkey : _settings.Hotkey;
        string? logSelectedText = null;
        string? logPrompt = null;

        try
        {
            using var cts = new CancellationTokenSource(Math.Max(5000, _settings.TimeoutMs + 3000));
            var capture = await _clipboard.ReadSelectedTextAsync(_settings.PreserveClipboard, cts.Token);
            var selectedText = capture.Text;
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                SetStatus("No selected text");
                if (_settings.DebugMode)
                {
                    using var manualForm = new ManualTextForm(
                        $"Alt+K fired. Capture attempts: {capture.Attempts}. {capture.Detail}");
                    if (manualForm.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    selectedText = manualForm.EnteredText;
                }
                else
                {
                    _notifyIcon.Visible = true;
                    _notifyIcon.ShowBalloonTip(5000, "Stealth Prompt", "No selected text captured. Turn on debug mode for details.", ToolTipIcon.Warning);
                    return;
                }
            }

            var hrdbContext = includeHrdb ? LoadHrdbContext() : null;
            var prompt = OpenAiClient.BuildPrompt(_settings, selectedText, hrdbContext);
            logSelectedText = selectedText;
            logPrompt = prompt;
            _logs.WritePending(hotkeyName, selectedText, prompt);
            if (_settings.DebugMode)
            {
                using var debugForm = new DebugPromptForm(selectedText, prompt);
                if (debugForm.ShowDialog() != DialogResult.OK)
                {
                    SetStatus("Debug cancelled");
                    return;
                }
            }

            var response = await _openAi.SendAsync(_settings, selectedText, cts.Token, hrdbContext);
            await _clipboard.SetClipboardTextAsync(response, cts.Token);
            _logs.WriteResponse(hotkeyName, selectedText, prompt, response);
            SetStatus("Response copied");
            if (_settings.DebugMode)
            {
                MessageBox.Show("Response copied to clipboard:\r\n\r\n" + response, "Stealth Prompt Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            SetStatus("Error");
            if (logSelectedText is not null && logPrompt is not null)
            {
                _logs.WriteError(hotkeyName, logSelectedText, logPrompt, ex);
            }
            else
            {
                _logs.WriteCaptureError(hotkeyName, ex.Message);
            }
            if (_settings.DebugMode)
            {
                MessageBox.Show(ex.Message, "Stealth Prompt Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (_settings.ShowToast)
            {
                _notifyIcon.ShowBalloonTip(5000, "Stealth Prompt", ex.Message, ToolTipIcon.Warning);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(CloneSettings(_settings));
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _settings = form.Settings;
        _configStore.Save(_settings);
        _notifyIcon.Visible = _settings.TrayIcon;
        RegisterHotkeys();
        SetStatus("Settings saved");
    }

    private void ShowFirstRunApiKeySetup(object? sender, EventArgs e)
    {
        Application.Idle -= ShowFirstRunApiKeySetup;
        ShowApiKeySetup();
    }

    private void ShowApiKeySetup()
    {
        using var form = new ApiKeyForm(_settings.Provider);
        if (form.ShowDialog() != DialogResult.OK)
        {
            SetStatus(CredentialStore.HasApiKey(_settings.Provider) ? $"Ready: {_settings.Hotkey}" : "Missing API key");
            return;
        }

        try
        {
            CredentialStore.SaveApiKey(form.ApiKey, _settings.Provider);
            SetStatus("API key saved");
            MessageBox.Show("API key saved. Select text and press Alt+K.", "Stealth Prompt", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus("API key error");
            MessageBox.Show(ex.Message, "Could not save API key", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RegisterHotkeys()
    {
        try
        {
            _normalHotkey.Register(_settings.Hotkey);
            _hrdbHotkey.Register(_settings.HrdbHotkey);
            SetStatus($"Ready: {_settings.Hotkey} / {_settings.HrdbHotkey}");
        }
        catch (Exception ex)
        {
            SetStatus("Hotkey error");
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(8000, "Stealth Prompt", ex.Message, ToolTipIcon.Warning);
        }
    }

    private void ToggleEnabled()
    {
        _enabled = !_enabled;
        _enabledItem.Checked = _enabled;
        SetStatus(_enabled ? $"Ready: {_settings.Hotkey} / {_settings.HrdbHotkey}" : "Disabled");
    }

    private void ToggleDebugMode()
    {
        _settings.DebugMode = !_settings.DebugMode;
        _configStore.Save(_settings);
        SetStatus(_settings.DebugMode ? "Debug mode on" : $"Ready: {_settings.Hotkey} / {_settings.HrdbHotkey}");
    }

    private string LoadHrdbContext()
    {
        if (!File.Exists(_settings.HrdbPath))
        {
            throw new FileNotFoundException("HRDB file not found.", _settings.HrdbPath);
        }

        return File.ReadAllText(_settings.HrdbPath);
    }

    private void OpenLogs()
    {
        _logs.EnsureExists();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_logs.LogPath) { UseShellExecute = true });
    }

    private void SetStatus(string text)
    {
        _statusItem.Text = text;
        if (_settings.ShowToast && _notifyIcon.Visible)
        {
            _notifyIcon.ShowBalloonTip(1200, "Stealth Prompt", text, ToolTipIcon.None);
        }
    }

    private void OpenConfigFolder()
    {
        var dir = Path.GetDirectoryName(_configStore.ConfigPath);
        if (dir is not null)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
        }
    }

    protected override void ExitThreadCore()
    {
        _normalHotkey.Dispose();
        _hrdbHotkey.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }

    private static AppSettings CloneSettings(AppSettings source) => new()
    {
        Hotkey = source.Hotkey,
        HrdbHotkey = source.HrdbHotkey,
        HrdbPath = source.HrdbPath,
        Provider = source.Provider,
        Model = source.Model,
        PreserveClipboard = source.PreserveClipboard,
        ShowToast = source.ShowToast,
        DebugMode = source.DebugMode,
        TrayIcon = source.TrayIcon,
        TimeoutMs = source.TimeoutMs,
    };
}
