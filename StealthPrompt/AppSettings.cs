namespace StealthPrompt;

public sealed class AppSettings
{
    public string Hotkey { get; set; } = "Alt+K";
    public string HrdbHotkey { get; set; } = "Alt+L";
    public string HrdbPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StealthPrompt", "agent.txt");
    public string Provider { get; set; } = "groq";
    public string Model { get; set; } = "llama-3.3-70b-versatile";
    public bool PreserveClipboard { get; set; } = true;
    public bool ShowToast { get; set; }
    public bool DebugMode { get; set; }
    public bool TrayIcon { get; set; } = true;
    public int TimeoutMs { get; set; } = 20000;
}
