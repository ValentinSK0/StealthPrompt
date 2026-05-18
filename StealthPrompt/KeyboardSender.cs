using System.Runtime.InteropServices;

namespace StealthPrompt;

public static class KeyboardSender
{
    private const byte VkControl = 0x11;
    private const byte VkC = 0x43;
    private const uint KeyEventFKeyUp = 0x0002;

    public static void SendCtrlC()
    {
        keybd_event(VkControl, 0, 0, UIntPtr.Zero);
        keybd_event(VkC, 0, 0, UIntPtr.Zero);
        keybd_event(VkC, 0, KeyEventFKeyUp, UIntPtr.Zero);
        keybd_event(VkControl, 0, KeyEventFKeyUp, UIntPtr.Zero);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
