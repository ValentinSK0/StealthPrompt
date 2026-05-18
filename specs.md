# Stealth Prompt - Product Specs

## Goal

Build a small Windows desktop helper that takes currently selected text, sends it to GPT after a global hotkey, and places the GPT response into the clipboard. The app should stay quiet and low-friction during normal work.

## Core Workflow

1. User selects text in any normal Windows app.
2. User presses a global hotkey, default `Alt + K`.
3. App reads selected text.
4. App sends selected text to GPT with a configurable prompt.
5. App copies GPT response to Windows clipboard.
6. User pastes response wherever needed with `Ctrl + V`.

## Feasibility On Windows

Yes, this is feasible on Windows.

Recommended implementation:

- Global hotkey via Windows API, AutoHotkey, Tauri, Electron, .NET, or native Rust/C#.
- Selected text capture via simulated `Ctrl + C`, then reading clipboard.
- Clipboard restore option so original clipboard can be preserved after input capture.
- GPT request via OpenAI API.
- Output via Windows clipboard API.

Most reliable MVP path:

- Use AutoHotkey or C#/.NET for hotkey + clipboard.
- Use a tiny local background process.
- Call OpenAI API over HTTPS.
- Store API key in Windows Credential Manager or encrypted user config.

## MVP Features

- Global hotkey: default `Alt + K`.
- Capture selected text using temporary clipboard copy.
- Send text to GPT.
- Copy response to clipboard.
- Config file for:
  - API key reference
  - model
  - system prompt
  - hotkey
  - response style
- Basic tray icon with:
  - enable/disable
  - settings
  - quit
- Short toast or no notification mode.
- Error copied to clipboard only when request fails and user enables debug mode.

## Prompt Behavior

Default system prompt:

```text
You are a concise assistant. Answer the user's selected text directly. Keep output ready to paste. Do not mention that text was selected or copied.
```

Example user prompt template:

```text
Process this selected text:

{{selected_text}}
```

Configurable modes:

- Explain
- Rewrite
- Translate
- Summarize
- Answer
- Fix grammar
- Custom prompt

## Privacy And Low-Visibility Requirements

The app should be discreet for normal personal workflow:

- No large window during normal use.
- No visible GPT chat UI.
- No automatic opening browser tabs.
- No persistent conversation history by default.
- No selected text logs by default.
- No cloud storage except the GPT API request.
- Optional tray icon can be hidden by user setting, but app must still have a clear quit/disable path.
- Config and API key stored locally with OS-level protection.

Security boundary:

- Do not bypass antivirus, endpoint monitoring, employer policy, admin controls, or app security restrictions.
- Do not disguise the app as another trusted process.
- Do not capture text without explicit hotkey action.
- Do not run as malware-style hidden persistence.

## UX Requirements

- Fast: target under 3 seconds for short text.
- Non-disruptive: no focus stealing.
- Predictable: response always ends in clipboard.
- Reversible: preserve original clipboard if enabled.
- Safe failure: if no text is selected, clipboard remains unchanged and optional small status appears.

## Settings

Suggested config fields:

```json
{
  "hotkey": "Alt+K",
  "hrdbHotkey": "Alt+L",
  "hrdbPath": "C:\\Users\\Valentin\\Desktop\\Projekt\\Stealth-prompt\\HRDB.txt",
  "provider": "groq",
  "model": "llama-3.3-70b-versatile",
  "preserveClipboard": true,
  "showToast": false,
  "debugMode": false,
  "trayIcon": true,
  "timeoutMs": 20000
}
```

Model policy:

- Default provider is `groq` when user supplies a Groq API key.
- Groq uses OpenAI-compatible `chat/completions` endpoint.
- OpenAI provider can still be configured later with an OpenAI API key.

## Technical Architecture

Components:

- Hotkey listener
- Selection capture module
- Clipboard manager
- OpenAI client
- Prompt template manager
- Settings storage
- Tray/settings UI

Flow:

```text
Hotkey -> Save Clipboard -> Ctrl+C -> Read Clipboard -> Restore Clipboard -> GPT Request -> Copy Response
```

If `preserveClipboard` is false:

```text
Hotkey -> Ctrl+C -> Read Clipboard -> GPT Request -> Copy Response
```

## Recommended Stack Options

### Option A: AutoHotkey + Local Helper

Pros:

- Fastest prototype.
- Great global hotkey support.
- Easy clipboard automation.

Cons:

- Packaging and API key handling less clean.
- Some environments may flag automation scripts.

### Option B: C#/.NET Windows App

Pros:

- Native Windows feel.
- Good tray app support.
- Strong Credential Manager integration.
- Good installer story.

Cons:

- More code than AutoHotkey.

### Option C: Rust + Tauri

Pros:

- Small binary.
- Good performance.
- Cross-platform possible later.

Cons:

- More setup.
- Windows clipboard/hotkey details need careful implementation.

## MVP Recommendation

Build v1 in C#/.NET as a tray app.

Reason:

- Windows-native.
- Easier secure key storage.
- Reliable hotkey and clipboard control.
- Cleaner than a script for daily use.

## Non-Goals For MVP

- Full chat UI.
- Message history.
- Screen OCR.
- Automatic monitoring of clipboard.
- Background capture without hotkey.
- Hidden persistence.
- Multi-user sync.

## Acceptance Criteria

- Pressing `Alt + K` with selected text sends that exact text to GPT.
- GPT response appears in clipboard.
- Original clipboard is restored during input capture when enabled.
- App does not show a main window during normal use.
- User can disable or quit app.
- API key is not stored in plain text.
- No text history is written to disk by default.

## Open Questions

- Which first mode: answer, rewrite, or translate?
- Should output include Markdown or plain text only?
- Should there be multiple hotkeys for different modes?
- Should errors be silent, tray-only, or copied to clipboard?
- Should app run on startup?
