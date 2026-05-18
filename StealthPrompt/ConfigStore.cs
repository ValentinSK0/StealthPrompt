using System.Text.Json;

namespace StealthPrompt;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _configPath;

    public ConfigStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StealthPrompt");
        Directory.CreateDirectory(dir);
        _configPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        AppSettings settings;
        if (!File.Exists(_configPath))
        {
            settings = new AppSettings();
            Save(settings);
            return settings;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            settings = new AppSettings();
        }

        Migrate(settings);
        Save(settings);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        File.WriteAllText(_configPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public string ConfigPath => _configPath;

    private static void Migrate(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            settings.Provider = "groq";
        }

        if (settings.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase) &&
            settings.Model.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) &&
            CredentialStore.HasApiKey("groq"))
        {
            settings.Provider = "groq";
        }

        if (settings.Provider.Equals("groq", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(settings.Model) ||
             settings.Model.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase)))
        {
            settings.Model = "llama-3.3-70b-versatile";
        }

        if (string.IsNullOrWhiteSpace(settings.HrdbHotkey))
        {
            settings.HrdbHotkey = "Alt+L";
        }

        if (string.IsNullOrWhiteSpace(settings.HrdbPath))
        {
            settings.HrdbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StealthPrompt",
                "agent.txt");
        }
    }
}
