using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Wpf.Ui.Appearance;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;
using Localization = global::LiveCaptionsTranslator.utils.Localization;
using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        private static SettingWindow? SettingWindow;

        private bool recordingHotkey = false;

        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            Loaded += (s, e) =>
            {
                (App.Current.MainWindow as MainWindow)?.AutoHeightAdjust(minHeight: 240, maxHeight: 240);
                CheckForFirstUse();
                Localization.LanguageChanged += OnLanguageChanged;
                ApplyLiveCaptionsButtonText();
            };
            Unloaded += (s, e) => Localization.LanguageChanged -= OnLanguageChanged;

            TranslateAPIBox.ItemsSource = Translator.Setting?.Configs.Keys;
            TranslateAPIBox.SelectedIndex = 0;

            VoiceInputModeBox.SelectedIndex = (int)Translator.Setting.VoiceInputMode;
            LanguageBox.SelectedIndex = Translator.Setting.Language == "zh-CN" ? 1 : 0;
            UpdateHotkeyButton();

            LoadAPISetting();
        }

        private void OnLanguageChanged()
        {
            ApplyLiveCaptionsButtonText();
        }

        private void ApplyLiveCaptionsButtonText()
        {
            bool hidden = Translator.Window == null ||
                Translator.Window.Current.BoundingRectangle == Rect.Empty;
            ButtonText.Text = Localization.T(hidden ? "SP.Show" : "SP.Hide");
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageBox.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                if (Translator.Setting!.Language != lang)
                {
                    Translator.Setting.Language = lang;
                    Translator.Setting.Save();
                }
                Localization.Apply(lang);
            }
        }

        private void LiveCaptionsButton_click(object sender, RoutedEventArgs e)
        {
            if (Translator.Window == null)
                return;

            var button = sender as Wpf.Ui.Controls.Button;
            var text = ButtonText.Text;

            bool isHide = Translator.Window.Current.BoundingRectangle == Rect.Empty;
            if (isHide)
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
            else
                LiveCaptionsHandler.HideLiveCaptions(Translator.Window);
            ApplyLiveCaptionsButtonText();
        }

        private void TranslateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAPISetting();
        }

        private void TargetLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetLangBox.SelectedItem != null)
                Translator.Setting.TargetLanguage = TargetLangBox.SelectedItem.ToString();
        }

        private void TargetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Translator.Setting.TargetLanguage = TargetLangBox.Text;
        }

        private void APISettingButton_click(object sender, RoutedEventArgs e)
        {
            if (SettingWindow != null && SettingWindow.IsLoaded)
                SettingWindow.Activate();
            else
            {
                SettingWindow = new SettingWindow();
                SettingWindow.Closed += (sender, args) => SettingWindow = null;
                SettingWindow.Show();
            }
        }

        private void Contexts_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            if (Translator.Setting.DisplaySentences > Translator.Setting.NumContexts)
                Translator.Setting.DisplaySentences = Translator.Setting.NumContexts;
        }

        private void DisplaySentences_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            if (Translator.Setting.DisplaySentences > Translator.Setting.NumContexts)
                Translator.Setting.NumContexts = Translator.Setting.DisplaySentences;
            Translator.Caption.OnPropertyChanged("DisplayLogCards");
            Translator.Caption.OnPropertyChanged("OverlayPreviousTranslation");
        }

        private void LiveCaptionsInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Show();
        }

        private void LiveCaptionsInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Hide();
        }

        private void FrequencyInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Show();
        }

        private void FrequencyInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Hide();
        }

        private void TranslateAPIInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            TranslateAPIInfoFlyout.Show();
        }

        private void TranslateAPIInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            TranslateAPIInfoFlyout.Hide();
        }

        private void TargetLangInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Show();
        }

        private void TargetLangInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Hide();
        }

        private void CaptionLogMaxInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            CaptionLogMaxInfoFlyout.Show();
        }

        private void CaptionLogMaxInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            CaptionLogMaxInfoFlyout.Hide();
        }

        private void ContextAwareInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            ContextAwareInfoFlyout.Show();
        }

        private void ContextAwareInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            ContextAwareInfoFlyout.Hide();
        }

        private void VoiceInputModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VoiceInputModeBox.SelectedItem is ComboBoxItem item &&
                item.Tag is string tag && int.TryParse(tag, out int mode))
            {
                Translator.Setting!.VoiceInputMode = (VoiceInputMode)mode;
                Translator.Setting.Save();
                VoiceInput.Reset();
            }
        }

        private void VoiceInputHalfSentence_Changed(object sender, RoutedEventArgs e)
        {
            Translator.Setting!.Save();
        }

        private void VoiceInputHotkeyButton_Click(object sender, RoutedEventArgs e)
        {
            recordingHotkey = true;
            VoiceInputHotkeyText.Text = Localization.T("SP.PressKeys");
            Keyboard.Focus(VoiceInputHotkeyButton);
        }

        private void VoiceInputHotkeyButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!recordingHotkey)
                return;
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                recordingHotkey = false;
                UpdateHotkeyButton();
                return;
            }

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin)
            {
                var parts = new List<string>();
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    parts.Add("Ctrl");
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                    parts.Add("Alt");
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    parts.Add("Shift");
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
                    parts.Add("Win");
                VoiceInputHotkeyText.Text = parts.Count > 0 ?
                    string.Join("+", parts) + "+" : Localization.T("SP.PressKeys");
                return;
            }

            var mods = Keyboard.Modifiers;
            if (!mods.HasFlag(ModifierKeys.Control) && !mods.HasFlag(ModifierKeys.Alt) &&
                !mods.HasFlag(ModifierKeys.Windows))
            {
                SnackbarHost.Show(Localization.T("VI.InvalidHotkey"),
                    Localization.T("VI.InvalidHotkeyMsg"),
                    SnackbarType.Warning, timeout: 2);
                recordingHotkey = false;
                UpdateHotkeyButton();
                return;
            }

            string combo = VoiceInput.FormatCombo(mods, key);
            Translator.Setting!.VoiceInputHotkey = combo;
            Translator.Setting.Save();
            recordingHotkey = false;
            UpdateHotkeyButton();

            var main = App.Current.MainWindow as MainWindow;
            if (main != null)
            {
                var hwnd = new WindowInteropHelper(main).Handle;
                if (VoiceInput.RegisterHotkey(hwnd, combo))
                    SnackbarHost.Show(Localization.T("VI.HotkeyUpdated"), combo,
                        SnackbarType.Success, timeout: 1);
                else
                    SnackbarHost.Show(Localization.T("VI.RegisterFailed"),
                        string.Format(Localization.T("VI.RegisterFailedMsg"), combo),
                        SnackbarType.Error, timeout: 2, closeButton: true);
            }
        }

        private void VoiceInputHotkeyButton_LostFocus(object sender, RoutedEventArgs e)
        {
            if (recordingHotkey)
            {
                recordingHotkey = false;
                UpdateHotkeyButton();
            }
        }

        private void UpdateHotkeyButton()
        {
            VoiceInputHotkeyText.Text = Translator.Setting!.VoiceInputHotkey;
        }

        private void CheckForFirstUse()
        {
            if (Translator.FirstUseFlag)
                ApplyLiveCaptionsButtonText();
        }

        public void LoadAPISetting()
        {
            var configType = Translator.Setting[Translator.Setting.ApiName].GetType();
            var languagesProp = configType.GetProperty(
                "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);

            // Traverse base classes to find `SupportedLanguages`
            while (configType != null && languagesProp == null)
            {
                configType = configType.BaseType;
                languagesProp = configType.GetProperty(
                    "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);
            }
            if (languagesProp == null)
                languagesProp = typeof(TranslateAPIConfig).GetProperty(
                    "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);

            var supportedLanguages = (Dictionary<string, string>)languagesProp.GetValue(null);
            TargetLangBox.ItemsSource = supportedLanguages.Keys;

            string targetLang = Translator.Setting.TargetLanguage;
            if (!supportedLanguages.ContainsKey(targetLang))
                supportedLanguages[targetLang] = targetLang;    // add custom language to supported languages
            TargetLangBox.SelectedItem = targetLang;
        }
    }
}