using System.Text.Json;

namespace StealthPrompt;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly HashSet<string> GroqModels = new(StringComparer.OrdinalIgnoreCase)
    {
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
    };
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

        if (settings.Provider.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = CredentialStore.HasApiKey("gemini") ? "gemini" : "groq";
        }

        if (!settings.Provider.Equals("groq", StringComparison.OrdinalIgnoreCase) &&
            !settings.Provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
        {
            settings.Provider = "groq";
        }

        if (settings.Provider.Equals("groq", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(settings.Model) ||
             settings.Model.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) ||
             settings.Model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase) ||
             !GroqModels.Contains(settings.Model)))
        {
            settings.Model = "llama-3.3-70b-versatile";
        }

        if (settings.Provider.Equals("gemini", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(settings.Model) ||
             settings.Model.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) ||
             settings.Model.StartsWith("llama-", StringComparison.OrdinalIgnoreCase)))
        {
            settings.Model = "gemini-2.5-flash";
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
