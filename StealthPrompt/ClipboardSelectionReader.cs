namespace StealthPrompt;

public sealed class ClipboardSelectionReader
{
    public async Task<SelectionCaptureResult> ReadSelectedTextAsync(bool preserveClipboard, CancellationToken cancellationToken)
    {
        IDataObject? original = null;
        var previousText = TryGetClipboardText();
        if (preserveClipboard)
        {
            try
            {
                original = Clipboard.GetDataObject();
            }
            catch
            {
                original = null;
            }
        }

        try
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                KeyboardSender.SendCtrlC();

                for (var i = 0; i < 12; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(80, cancellationToken);

                    var text = TryGetClipboardText();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return new SelectionCaptureResult(text, attempt, "Captured via Ctrl+C.");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(previousText))
            {
                return new SelectionCaptureResult(previousText, 3, "Ctrl+C capture failed; using text already in clipboard.");
            }

            return new SelectionCaptureResult(null, 3, "Ctrl+C fired, but clipboard stayed empty.");
        }
        finally
        {
            if (preserveClipboard && original is not null)
            {
                try
                {
                    Clipboard.SetDataObject(original, true);
                }
                catch
                {
                    // Clipboard may be locked by another process; output step will retry separately.
                }
            }
        }
    }

    public async Task SetClipboardTextAsync(string text, CancellationToken cancellationToken)
    {
        Exception? lastError = null;

        for (var i = 0; i < 8; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                await Task.Delay(80, cancellationToken);
            }
        }

        throw new InvalidOperationException("Could not write response to clipboard.", lastError);
    }

    private static string? TryGetClipboardText()
    {
        try
        {
            return Clipboard.ContainsText(TextDataFormat.UnicodeText)
                ? Clipboard.GetText(TextDataFormat.UnicodeText)
                : null;
        }
        catch
        {
            return null;
        }
    }

}

public sealed record SelectionCaptureResult(string? Text, int Attempts, string Detail);
