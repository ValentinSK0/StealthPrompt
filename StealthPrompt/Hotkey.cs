using System.Runtime.InteropServices;

namespace StealthPrompt;

public sealed class Hotkey : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly int _hotkeyId;
    private bool _registered;

    public event EventHandler? Pressed;

    public Hotkey(int hotkeyId)
    {
        _hotkeyId = hotkeyId;
        CreateHandle(new CreateParams());
    }

    public void Register(string hotkey)
    {
        Unregister();
        var parsed = HotkeyParser.Parse(hotkey);
        if (!RegisterHotKey(Handle, _hotkeyId, parsed.Modifiers, parsed.Key))
        {
            throw new InvalidOperationException($"Could not register hotkey '{hotkey}'. It may already be in use.");
        }

        _registered = true;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, _hotkeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == _hotkeyId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

public static class HotkeyParser
{
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    public static (uint Modifiers, uint Key) Parse(string text)
    {
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new FormatException("Hotkey cannot be empty.");
        }

        uint modifiers = ModNoRepeat;
        Keys? key = null;

        foreach (var part in parts)
        {
            switch (part.ToLowerInvariant())
            {
                case "alt":
                    modifiers |= ModAlt;
                    break;
                case "ctrl":
                case "control":
                    modifiers |= ModControl;
                    break;
                case "shift":
                    modifiers |= ModShift;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModWin;
                    break;
                default:
                    if (!Enum.TryParse<Keys>(part, true, out var parsed))
                    {
                        throw new FormatException($"Unknown hotkey key '{part}'.");
                    }

                    key = parsed;
                    break;
            }
        }

        if (key is null)
        {
            throw new FormatException("Hotkey needs a key, for example Alt+K.");
        }

        return (modifiers, (uint)key.Value);
    }
}
