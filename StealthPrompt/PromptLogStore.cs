using System.Text;

namespace StealthPrompt;

public sealed class PromptLogStore
{
    private readonly string _logPath;

    public PromptLogStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StealthPrompt");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "last-log.txt");
    }

    public string LogPath => _logPath;

    public void WritePending(string hotkey, string selectedText, string prompt)
    {
        Write("""
Status: Pending
""" + "\r\n" +
              $"Hotkey: {hotkey}\r\n" +
              $"Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}\r\n\r\n" +
              "Selected text:\r\n" +
              selectedText +
              "\r\n\r\nPrompt sent:\r\n" +
              prompt +
              "\r\n\r\nAI response:\r\n");
    }

    public void WriteResponse(string hotkey, string selectedText, string prompt, string response)
    {
        Write("""
Status: OK
""" + "\r\n" +
              $"Hotkey: {hotkey}\r\n" +
              $"Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}\r\n\r\n" +
              "Selected text:\r\n" +
              selectedText +
              "\r\n\r\nPrompt sent:\r\n" +
              prompt +
              "\r\n\r\nAI response:\r\n" +
              response);
    }

    public void WriteError(string hotkey, string selectedText, string prompt, Exception error)
    {
        Write("""
Status: ERROR
""" + "\r\n" +
              $"Hotkey: {hotkey}\r\n" +
              $"Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}\r\n\r\n" +
              "Selected text:\r\n" +
              selectedText +
              "\r\n\r\nPrompt sent:\r\n" +
              prompt +
              "\r\n\r\nError:\r\n" +
              error.GetType().Name +
              ": " +
              error.Message);
    }

    public void WriteCaptureError(string hotkey, string detail)
    {
        Write("""
Status: CAPTURE ERROR
""" + "\r\n" +
              $"Hotkey: {hotkey}\r\n" +
              $"Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}\r\n\r\n" +
              "Error:\r\n" +
              detail);
    }

    public void EnsureExists()
    {
        if (!File.Exists(_logPath))
        {
            Write("No prompt has been sent yet.");
        }
    }

    private void Write(string text)
    {
        File.WriteAllText(_logPath, text, Encoding.UTF8);
    }
}
