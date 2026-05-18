namespace StealthPrompt;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => HandleUnhandledException(e.Exception, showMessage: true);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleUnhandledException(ex, showMessage: false);
            }
        };

        Application.Run(new TrayAppContext());
    }

    private static void HandleUnhandledException(Exception exception, bool showMessage)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StealthPrompt");
            Directory.CreateDirectory(dir);
            File.WriteAllText(
                Path.Combine(dir, "unhandled-error.txt"),
                $"Time: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}\r\n\r\n{exception}");
        }
        catch
        {
            // Last-resort crash handling must not throw another exception.
        }

        if (showMessage)
        {
            MessageBox.Show(
                "Unexpected error was logged to %APPDATA%\\StealthPrompt\\unhandled-error.txt:\r\n\r\n" + exception.Message,
                "Stealth Prompt Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
