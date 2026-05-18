# Stealth Prompt

Windows tray app that sends selected text to AI via hotkeys and copies the response to clipboard. Supports Groq or Gemini, local `agent.txt` context with `Alt+L`, secure key storage, and latest-run logs.

## What It Does

1. Select text in any app.
2. Press `Alt+K`.
3. Selected text is sent to the configured AI provider.
4. Response is copied to clipboard.
5. Paste with `Ctrl+V`.

`Alt+L` does the same thing, but also adds `%APPDATA%\StealthPrompt\agent.txt` as context/instructions.

## Features

- Global hotkeys: `Alt+K` and `Alt+L`
- Groq and Google AI Studio Gemini API support
- API key stored in Windows Credential Manager
- Local agent context file for `Alt+L`
- Latest-run log with prompt, response, or error
- Tray settings UI

## Build

```powershell
dotnet build .\StealthPrompt\StealthPrompt.csproj
```

## Run

```powershell
dotnet run --project .\StealthPrompt\StealthPrompt.csproj
```

The app starts in the system tray. Right-click tray icon for Settings or Quit.

## API Key

Set Groq and/or Gemini API key in Settings. Choose `groq` or `gemini` as provider. Keys are stored separately in Windows Credential Manager.

Alternative:

```powershell
$env:GROQ_API_KEY = "gsk_..."
$env:GEMINI_API_KEY = "AIza..."
dotnet run --project .\StealthPrompt\StealthPrompt.csproj
```

## Default Config

Config file:

```text
%APPDATA%\StealthPrompt\settings.json
```

Defaults:

```json
{
  "hotkey": "Alt+K",
  "hrdbHotkey": "Alt+L",
  "hrdbPath": "%APPDATA%\\StealthPrompt\\agent.txt",
  "provider": "groq",
  "model": "llama-3.3-70b-versatile",
  "preserveClipboard": true,
  "showToast": false,
  "debugMode": false,
  "trayIcon": true,
  "timeoutMs": 20000
}
```

## Debug Alt+K

Right-click tray icon and choose `Toggle debug mode`.

When debug is on, `Alt+K` shows:

- captured selected text
- exact prompt sent to AI provider
- final copied response or error message

## Logs

Right-click the tray icon and choose `Logs`.

The app overwrites the log on every run:

```text
%APPDATA%\StealthPrompt\last-log.txt
```

## Publish

```powershell
dotnet publish .\StealthPrompt\StealthPrompt.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output:

```text
StealthPrompt\bin\Release\net10.0-windows\win-x64\publish\StealthPrompt.exe
```
