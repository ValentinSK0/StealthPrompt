using System.Runtime.InteropServices;
using System.Text;

namespace StealthPrompt;

public static class CredentialStore
{
    private const string DefaultProvider = "groq";
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;

    public static void SaveApiKey(string apiKey, string provider = DefaultProvider)
    {
        var target = GetTarget(provider);
        var bytes = Encoding.Unicode.GetBytes(apiKey);
        var credential = new NativeCredential
        {
            Type = CredTypeGeneric,
            TargetName = target,
            CredentialBlobSize = bytes.Length,
            CredentialBlob = Marshal.StringToCoTaskMemUni(apiKey),
            Persist = CredPersistLocalMachine,
            UserName = Environment.UserName
        };

        try
        {
            if (!CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException($"CredWrite failed: {Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.ZeroFreeCoTaskMemUnicode(credential.CredentialBlob);
        }
    }

    public static string? LoadApiKey(string provider = DefaultProvider)
    {
        var target = GetTarget(provider);
        if (!CredRead(target, CredTypeGeneric, 0, out var credentialPtr))
        {
            if (provider.Equals("groq", StringComparison.OrdinalIgnoreCase))
            {
                return Environment.GetEnvironmentVariable("GROQ_API_KEY");
            }

            if (provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
            {
                return Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            }

            return null;
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(credentialPtr);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize <= 0)
            {
                return null;
            }

            return Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
        }
        finally
        {
            CredFree(credentialPtr);
        }
    }

    public static bool HasApiKey(string provider = DefaultProvider) => !string.IsNullOrWhiteSpace(LoadApiKey(provider));

    private static string GetTarget(string provider)
    {
        return provider.Equals("gemini", StringComparison.OrdinalIgnoreCase)
            ? "StealthPrompt.Gemini.ApiKey"
            : "StealthPrompt.Groq.ApiKey";
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref NativeCredential userCredential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public int Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }
}
