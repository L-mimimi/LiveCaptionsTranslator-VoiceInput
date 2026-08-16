using System.Runtime.InteropServices;
using System.Windows.Input;

namespace LiveCaptionsTranslator.utils
{
    public enum VoiceInputMode
    {
        Off = 0,
        Translated = 1,
        Original = 2
    }

    /// <summary>
    /// Types the live-caption / translation text into the currently focused window
    /// (voice input), and manages the global toggle hotkey.
    /// </summary>
    public static class VoiceInput
    {
        // ------------------------------------------------------------------
        // Runtime state
        // ------------------------------------------------------------------
        public static bool Enabled { get; private set; } = false;

        private static readonly object stateLock = new();

        private static string? lastTypedOriginal = null;
        private static string lastTypedText = string.Empty;
        private static long lastOriginalTypeTick = 0;

        private const long ORIGINAL_THROTTLE_MS = 300;
        private const int CHAR_DELAY_MS = 2;
        private const int REPLACE_SETTLE_MS = 30;

        // ------------------------------------------------------------------
        // Global hotkey
        // ------------------------------------------------------------------
        public const int WM_HOTKEY = 0x0312;
        public const int HOTKEY_ID = 0x4156; // "AV"

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        private static nint hotkeyWindow = nint.Zero;

        public static event Action? Toggled;

        private static VoiceInputMode CurrentMode =>
            Translator.Setting?.VoiceInputMode ?? VoiceInputMode.Off;

        private static bool HalfSentence =>
            Translator.Setting?.VoiceInputHalfSentence ?? true;

        // ------------------------------------------------------------------
        // P/Invoke: SendInput (keyboard injection)
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public nint dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(nint hWnd, int id);

        [DllImport("user32.dll")]
        private static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        // ------------------------------------------------------------------
        // Typing
        // ------------------------------------------------------------------
        private static bool inputErrorShown = false;

        public static void TypeText(string text)
        {
            foreach (char ch in text)
            {
                SendChar(ch);
                if (CHAR_DELAY_MS > 0)
                    Thread.Sleep(CHAR_DELAY_MS);
            }
        }

        public static void Backspace(int count)
        {
            for (int i = 0; i < count; i++)
                SendKeyDownUp(0x08);
            if (count > 0)
                Thread.Sleep(REPLACE_SETTLE_MS);
        }

        private static void SendChar(char ch)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = nint.Zero
                    }
                }
            };
            inputs[1] = inputs[0];
            inputs[1].U.ki.dwFlags |= KEYEVENTF_KEYUP;
            SendInputChecked(2, inputs);
        }

        private static void SendKeyDownUp(ushort vk)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = nint.Zero
                    }
                }
            };
            inputs[1] = inputs[0];
            inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;
            SendInputChecked(2, inputs);
        }

        private static void SendInputChecked(uint nInputs, INPUT[] inputs)
        {
            uint sent = SendInput(nInputs, inputs, Marshal.SizeOf<INPUT>());
            if (sent != nInputs && !inputErrorShown)
            {
                inputErrorShown = true;
                SnackbarHost.Show(Localization.T("VI.InputFailed"),
                    string.Format(Localization.T("VI.InputFailedMsg"),
                        Marshal.GetLastWin32Error()),
                    SnackbarType.Error, timeout: 3, closeButton: true);
            }
        }

        /// <summary>True when the foreground window belongs to this process.</summary>
        public static bool ForegroundIsOwnProcess()
        {
            nint fg = GetForegroundWindow();
            if (fg == nint.Zero)
                return false;
            GetWindowThreadProcessId(fg, out uint pid);
            return pid == (uint)Environment.ProcessId;
        }

        // ------------------------------------------------------------------
        // Hooks called from Translator loops
        // ------------------------------------------------------------------
        /// <summary>Called when a new translated sentence is available.</summary>
        public static void OnTranslated(string originalText, string translatedText, bool isChoke)
        {
            if (!Enabled || CurrentMode != VoiceInputMode.Translated)
                return;
            if (string.IsNullOrEmpty(translatedText) || string.IsNullOrEmpty(originalText))
                return;
            if (translatedText.Contains("[ERROR]") || translatedText.Contains("[WARNING]"))
                return;

            // Strip the latency / notice prefix like "[ 123 ms] ".
            var match = RegexPatterns.NoticePrefixAndTranslation().Match(translatedText);
            string text = match.Groups[2].Value.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            lock (stateLock)
            {
                // Duplicate retranslation of the same sentence.
                if (string.CompareOrdinal(originalText, lastTypedOriginal) == 0)
                    return;

                // Type incomplete sentences only when the half-sentence option is on.
                if (!isChoke && !HalfSentence)
                    return;

                // Never type into this app's own windows.
                if (ForegroundIsOwnProcess())
                    return;

                bool isExtension = lastTypedOriginal != null &&
                    originalText.StartsWith(lastTypedOriginal) && lastTypedText.Length > 0;
                if (isExtension)
                    Backspace(lastTypedText.Length);
                TypeText(text);

                lastTypedOriginal = originalText;
                lastTypedText = text;
            }
        }

        /// <summary>Called when the latest original caption sentence changes.</summary>
        public static void OnOriginalCaption(string originalText)
        {
            if (!Enabled || CurrentMode != VoiceInputMode.Original)
                return;
            if (string.IsNullOrEmpty(originalText))
                return;

            bool isComplete = Array.IndexOf(TextUtil.PUNC_EOS, originalText[^1]) != -1;

            lock (stateLock)
            {
                if (string.CompareOrdinal(originalText, lastTypedOriginal) == 0)
                    return;

                // Type incomplete sentences only when the half-sentence option is on.
                if (!isComplete && !HalfSentence)
                    return;

                // Throttle rapid partial updates, but always flush completed sentences.
                long now = Environment.TickCount64;
                if (!isComplete && now - lastOriginalTypeTick < ORIGINAL_THROTTLE_MS)
                    return;

                if (ForegroundIsOwnProcess())
                    return;

                bool isExtension = lastTypedOriginal != null &&
                    originalText.StartsWith(lastTypedOriginal) && lastTypedText.Length > 0;
                if (isExtension)
                    Backspace(lastTypedText.Length);
                TypeText(originalText);

                lastTypedOriginal = originalText;
                lastTypedText = originalText;
                lastOriginalTypeTick = now;
            }
        }

        public static void Reset()
        {
            lock (stateLock)
            {
                lastTypedOriginal = null;
                lastTypedText = string.Empty;
                lastOriginalTypeTick = 0;
                inputErrorShown = false;
            }
        }

        // ------------------------------------------------------------------
        // Toggle / hotkey
        // ------------------------------------------------------------------
        public static void Toggle()
        {
            if (CurrentMode == VoiceInputMode.Off)
            {
                SnackbarHost.Show(Localization.T("VI.OffTitle"),
                    Localization.T("VI.OffMsg"),
                    SnackbarType.Warning, timeout: 2);
                return;
            }

            Enabled = !Enabled;
            Reset();

            if (Enabled)
            {
                string modeName = Localization.T(CurrentMode == VoiceInputMode.Translated
                    ? "SP.Mode.Translated" : "SP.Mode.Original");
                SnackbarHost.Show(Localization.T("VI.EnabledTitle"),
                    string.Format(Localization.T("VI.EnabledMsg"), modeName,
                        Translator.Setting?.VoiceInputHotkey),
                    SnackbarType.Success, timeout: 2);
            }
            else
                SnackbarHost.Show(Localization.T("VI.DisabledTitle"), string.Empty,
                    SnackbarType.Info, timeout: 2);

            Toggled?.Invoke();
        }

        public static bool RegisterHotkey(nint hwnd, string combo)
        {
            Unregister();
            if (hwnd == nint.Zero || string.IsNullOrWhiteSpace(combo))
                return false;
            if (!TryParseCombo(combo, out uint mods, out uint vk))
                return false;
            hotkeyWindow = hwnd;
            return RegisterHotKey(hwnd, HOTKEY_ID, mods | MOD_NOREPEAT, vk);
        }

        public static void Unregister()
        {
            if (hotkeyWindow != nint.Zero)
                UnregisterHotKey(hotkeyWindow, HOTKEY_ID);
            hotkeyWindow = nint.Zero;
        }

        public static bool TryParseCombo(string combo, out uint mods, out uint vk)
        {
            mods = 0;
            vk = 0;
            var parts = combo.Split('+',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                return false;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                switch (parts[i].ToLowerInvariant())
                {
                    case "ctrl":
                    case "control":
                        mods |= MOD_CONTROL;
                        break;
                    case "alt":
                        mods |= MOD_ALT;
                        break;
                    case "shift":
                        mods |= MOD_SHIFT;
                        break;
                    case "win":
                    case "windows":
                        mods |= MOD_WIN;
                        break;
                    default:
                        return false;
                }
            }

            if (!Enum.TryParse(parts[^1], true, out Key key))
                return false;
            int code = KeyInterop.VirtualKeyFromKey(key);
            if (code <= 0)
                return false;
            vk = (uint)code;
            return true;
        }

        public static string FormatCombo(ModifierKeys mods, Key key)
        {
            var list = new List<string>();
            if (mods.HasFlag(ModifierKeys.Control))
                list.Add("Ctrl");
            if (mods.HasFlag(ModifierKeys.Alt))
                list.Add("Alt");
            if (mods.HasFlag(ModifierKeys.Shift))
                list.Add("Shift");
            if (mods.HasFlag(ModifierKeys.Windows))
                list.Add("Win");
            list.Add(key.ToString());
            return string.Join("+", list);
        }
    }
}
