using System.Windows;

namespace LiveCaptionsTranslator.utils
{
    /// <summary>
    /// Simple runtime localization: builds a string resource dictionary for the
    /// selected language and merges it into application resources so that
    /// {DynamicResource} references update immediately.
    /// </summary>
    public static class Localization
    {
        private const string MARKER_KEY = "__lang__";

        private static readonly object langLock = new();

        public static event Action? LanguageChanged;

        public static string Current { get; private set; } = "en";

        public static string T(string key)
        {
            lock (langLock)
            {
                return (Application.Current?.TryFindResource(key) as string) ?? key;
            }
        }

        public static void Apply(string? lang)
        {
            if (lang != "zh-CN")
                lang = "en";

            lock (langLock)
            {
                Current = lang;
                var merged = Application.Current.Resources.MergedDictionaries;
                for (int i = merged.Count - 1; i >= 0; i--)
                {
                    if (merged[i].Contains(MARKER_KEY))
                        merged.RemoveAt(i);
                }
                merged.Add(Build(lang));
            }

            LanguageChanged?.Invoke();
        }

        private static ResourceDictionary Build(string lang)
        {
            var d = new ResourceDictionary
            {
                [MARKER_KEY] = lang
            };

            void Add(string key, string en, string zh) =>
                d[key] = lang == "zh-CN" ? zh : en;

            // ---- Main window ----
            Add("Nav.Caption", "Caption", "字幕");
            Add("Nav.Setting", "Setting", "设置");
            Add("Nav.History", "History", "历史记录");
            Add("Nav.Info", "Info", "信息");
            Add("Main.Tooltip.VoiceInput", "Voice Input (type into focused window)", "语音输入（输入到焦点窗口）");
            Add("Main.Tooltip.LogCards", "Log Cards of Captions", "字幕日志卡片");
            Add("Main.Tooltip.Pause", "Pause Translation (Log Only)", "暂停翻译（仅记录）");
            Add("Main.Tooltip.Overlay", "Overlay Window", "悬浮窗");
            Add("Main.Tooltip.Topmost", "Always on Top", "窗口置顶");
            Add("Main.HotkeyWarning.Title", "[WARNING] Failed to register hotkey.", "[WARNING] 快捷键注册失败。");
            Add("Main.HotkeyWarning.Msg",
                "Voice Input hotkey ({0}) may be taken by another app.",
                "语音输入快捷键（{0}）可能被其他程序占用。");
            Add("Main.NewVersion.Title", "New Version Available", "发现新版本");
            Add("Main.NewVersion.Content",
                "A new version has been detected: {0}\nCurrent version: {1}\nPlease visit GitHub to download the latest release.",
                "检测到新版本：{0}\n当前版本：{1}\n请访问 GitHub 下载最新版本。");
            Add("Main.NewVersion.Update", "Update", "更新");
            Add("Main.NewVersion.Ignore", "Ignore this version", "忽略此版本");
            Add("Main.UpdateCheckFailed.Title", "[ERROR] Update Check Failed.", "[ERROR] 检查更新失败。");
            Add("Main.OpenBrowserFailed.Title", "[ERROR] Open Browser Failed.", "[ERROR] 打开浏览器失败。");

            // ---- Setting page ----
            Add("SP.LiveCaptions", "LiveCaptions", "实时字幕");
            Add("SP.ApiInterval", "API Interval", "API 调用间隔");
            Add("SP.TranslateApi", "Translate API", "翻译 API");
            Add("SP.TargetLanguage", "Target Language", "目标语言");
            Add("SP.ApiSetting", "API Setting", "API 设置");
            Add("SP.Open", "Open", "打开");
            Add("SP.ShowLatency", "Show Latency", "显示延迟");
            Add("SP.Contexts", "Contexts", "上下文");
            Add("SP.DisplaySentences", "Display Sentences", "显示句数");
            Add("SP.ContextAware", "Context Aware", "上下文感知");
            Add("SP.Show", "Show", "显示");
            Add("SP.Hide", "Hide", "隐藏");
            Add("SP.VoiceInputMode", "Voice Input Mode", "语音输入模式");
            Add("SP.TypeIncomplete", "Type Incomplete Sentences", "输入未完成句");
            Add("SP.ToggleHotkey", "Toggle Hotkey", "切换快捷键");
            Add("SP.Language", "Language", "语言");
            Add("SP.PressKeys", "Press keys...", "请按下快捷键...");
            Add("SP.Mode.Off", "Off", "关闭");
            Add("SP.Mode.Translated", "Translated", "翻译输入");
            Add("SP.Mode.Original", "Original", "原文直输");

            Add("SP.Fly.LiveCaptions.After",
                "After Windows 11 version 24H2, you can only change the",
                "Windows 11 24H2 之后，你只能在实时字幕中修改");
            Add("SP.Fly.LiveCaptions.SourceLanguage", "Source Language", "源语言");
            Add("SP.Fly.LiveCaptions.In", "in LiveCaptions.", "。");
            Add("SP.Fly.LiveCaptions.Note", "\n\nNote:", "\n\n注意：");
            Add("SP.Fly.LiveCaptions.PleaseClick", "Please click", "请点击");
            Add("SP.Fly.LiveCaptions.HideQuoted", "Hide", "隐藏");
            Add("SP.Fly.LiveCaptions.ToHide",
                "to hide LiveCaptions instead of closing it directly.",
                "来隐藏实时字幕，而不是直接关闭它。");
            Add("SP.Fly.Frequency.Determines",
                "Determines the frequency of translate API calls. The smaller it is, the more frequent API calls.",
                "决定翻译 API 的调用频率。数值越小，调用越频繁。");
            Add("SP.Fly.Frequency.Called",
                "\nThe translate API is called once after the caption changes",
                "\n字幕每变化");
            Add("SP.Fly.Frequency.ApiInterval", "API Interval", "API 调用间隔");
            Add("SP.Fly.Frequency.Times", "times.", "次后调用一次翻译 API。");
            Add("SP.Fly.TranslateApi.Text",
                "Except for Google and Google2, all other APIs require configuring before they can be used.",
                "除 Google 和 Google2 外，其他 API 都需要先配置才能使用。");
            Add("SP.Fly.TargetLang.Quote", "\"There isn't the target language I expect!\"", "\"没有我想要的目标语言！\"");
            Add("SP.Fly.TargetLang.Edit",
                "\nYou can directly edit the content of this combobox to customize the language, and it is recommended to follow the",
                "\n你可以直接编辑此下拉框的内容来自定义语言，建议遵循");
            Add("SP.Fly.TargetLang.Bcp", "BCP 47 language tag.", "BCP 47 语言标签。");
            Add("SP.Fly.TargetLang.Note", "\n\nNote:", "\n\n注意：");
            Add("SP.Fly.TargetLang.NoteText",
                "Some of APIs (such as DeepL) needs another way to define target language, see their official docs for more details.",
                "部分 API（如 DeepL）需要用另一种方式定义目标语言，详见其官方文档。");
            Add("SP.Fly.TargetLang.NoNeed",
                "\nNo need to consider this for included target languages, since we've built in tag mappings. But if your expected language isn't in the list, keep this in mind.",
                "\n内置的目标语言无需考虑此问题，因为我们已内置标签映射。但如果列表中没有你期望的语言，请注意这一点。");
            Add("SP.Fly.Contexts.Contexts", "Contexts:", "上下文：");
            Add("SP.Fly.Contexts.Determines",
                "Determines the number of context sentences when",
                "决定启用");
            Add("SP.Fly.Contexts.ContextAware", "Context Aware", "上下文感知");
            Add("SP.Fly.Contexts.Enabled", "is enabled.", "时的上下文句子数量。");
            Add("SP.Fly.Contexts.Overlay", "\nOverlay Sentences:", "\n悬浮窗句数：");
            Add("SP.Fly.Contexts.Cards",
                "Determines the number of displayed cards when",
                "决定启用");
            Add("SP.Fly.Contexts.LogCards", "Log Cards", "日志卡片");
            Add("SP.Fly.Contexts.OverlayEnabled",
                "is enabled, as well as the max number of sentences displayed in the Overlay Window.",
                "时显示的卡片数量，以及悬浮窗中最多显示的句子数量。");
            Add("SP.Fly.Contexts.Note", "\n\nNote:", "\n\n注意：");
            Add("SP.Fly.Contexts.MustBe", "Contexts must be", "上下文必须");
            Add("SP.Fly.Contexts.Gte", "greater than or equal", "大于或等于");
            Add("SP.Fly.Contexts.Display",
                "Display Sentences. If not met, the program will automatically adjust them.",
                "显示句数，否则程序会自动调整。");
            Add("SP.Fly.ContextAware.Translate", "Translate in context.", "结合上下文翻译。");
            Add("SP.Fly.ContextAware.Accuracy",
                "\nIt can improve translation accuracy, but will consume more tokens.",
                "\n可以提高翻译准确性，但会消耗更多 Token。");

            // ---- API setting window ----
            Add("SW.APISetting", "API Setting", "API 设置");
            Add("SW.Prompt", "Prompt", "提示词");
            Add("SW.CurrentConfig", "Current Config: ", "当前配置：");
            Add("SW.New", "New", "新建");
            Add("SW.Delete", "Delete", "删除");
            Add("SW.KeepOneConfig", "You must keep at least one config.", "必须保留至少一个配置。");
            Add("SW.ModelName", "Model Name", "模型名称");
            Add("SW.Temperature", "Temperature", "温度");
            Add("SW.ApiUrl", "API Url", "API 地址");
            Add("SW.ApiUrlBase", "API Url (Base)", "API 地址（Base）");
            Add("SW.ApiKey", "API Key", "API 密钥");
            Add("SW.AppKey", "APP Key", "应用 Key");
            Add("SW.AppId", "APP ID", "应用 ID");
            Add("SW.AppSecret", "APP Secret", "应用密钥");
            Add("SW.SourceLanguage", "Source Language", "源语言");
            Add("SW.LoadModels", "Load Models", "加载模型");
            Add("SW.PromptNote1", "Note 1:", "注 1：");
            Add("SW.PromptNote1Text",
                "The {0} in the prompt indicates the target language, so make sure your prompt includes {0}.",
                "提示词中的 {0} 表示目标语言，请确保提示词中包含 {0}。");
            Add("SW.PromptNote2", "\nNote 2:", "\n注 2：");
            Add("SW.PromptNote2Text", "The source text is enclosed with 🔤.", "源文本用 🔤 括起来。");
            Add("SW.Fly.Ollama.NoNeed", "No need to explicitly add", "无需显式添加");
            Add("SW.Fly.Ollama.Suffix", "`api/chat`", "`api/chat`");
            Add("SW.Fly.Ollama.End", "suffix.", "后缀。");
            Add("SW.Fly.LMStudio.Base", "Base URL ending with", "Base URL 以");
            Add("SW.Fly.LMStudio.ApiV1", "`api/v1`", "`api/v1`");
            Add("SW.Fly.LMStudio.Appended",
                ". Chat endpoint and models are appended automatically.",
                " 结尾，对话端点和模型路径会自动追加。");
            Add("SW.Fly.OpenAI.UseFull", "Use Full Url (typically ending with", "请使用完整 URL（通常以");
            Add("SW.Fly.OpenAI.V1Chat", "`v1/chat/completions`", "`v1/chat/completions`");
            Add("SW.Fly.OpenAI.Instead", ") instead of Base Url (typically ending with just", "结尾），而不是 Base URL（通常只以");
            Add("SW.Fly.OpenAI.V1", "`v1`", "`v1`");
            Add("SW.Fly.OpenAI.End", ").", "结尾）。");
            Add("SW.LoadModels.Title", "Load Models", "加载模型");
            Add("SW.LoadModels.SetUrlFirst", "Please set the API URL first.", "请先设置 API 地址。");
            Add("SW.LoadModels.Loaded", "Loaded {0} model(s).", "已加载 {0} 个模型。");
            Add("SW.LoadModels.NoModels",
                "No models found or unable to connect. Check that the server is running.",
                "未找到模型或无法连接。请检查服务器是否正在运行。");

            // ---- History page ----
            Add("Hist.Previous", "Previous", "上一页");
            Add("Hist.Next", "Next Page", "下一页");
            Add("Hist.Search", "Search", "搜索");
            Add("Hist.Export", "Export", "导出");
            Add("Hist.DeleteAll", "Delete All", "删除全部");
            Add("Hist.Refresh", "Refresh", "刷新");
            Add("Hist.Col.Time", "Time", "时间");
            Add("Hist.Col.Caption", "Caption", "字幕原文");
            Add("Hist.Col.Translated", "Translated", "翻译");
            Add("Hist.Col.Api", "API", "API");
            Add("Hist.Page20", "20/page", "20/页");
            Add("Hist.Page30", "30/page", "30/页");
            Add("Hist.Page50", "50/page", "50/页");
            Add("Hist.Page100", "100/page", "100/页");
            Add("Hist.DelTitle", "Do you want to delete all history?", "确定删除全部历史记录吗？");
            Add("Hist.DelContent", "This operation cannot be undone!", "此操作不可撤销！");
            Add("Hist.Saved", "Saved Success.", "保存成功。");
            Add("Hist.SavedTo", "File saved to: {0}", "文件已保存到：{0}");
            Add("Hist.SaveFailed", "Save Failed.", "保存失败。");
            Add("Hist.SaveFailedMsg", "File saved failed: {0}", "保存失败：{0}");

            // ---- Caption page / common ----
            Add("Common.CopyTip", "Click to Copy, Ctrl+Scroll to Resize Font", "点击复制，Ctrl+滚轮调整字号");
            Add("Common.CopyTipShort", "Click To Copy", "点击复制");
            Add("Common.Copied", "Copied.", "已复制。");
            Add("Common.CopyFailed", "Copy Failed.", "复制失败。");
            Add("Common.On", "On", "开");
            Add("Common.Off", "Off", "关");
            Add("Common.Yes", "Yes", "是");
            Add("Common.No", "No", "否");

            // ---- Overlay window ----
            Add("Ov.Title", "Overlay Window", "悬浮窗");
            Add("Ov.FontInc", "Font Size Increase", "字体增大");
            Add("Ov.FontDec", "Font Size Decrease", "字体减小");
            Add("Ov.StrokeInc", "Font Stroke Increase", "描边增强");
            Add("Ov.StrokeDec", "Font Stroke Decrease", "描边减弱");
            Add("Ov.Bold", "Font Bold", "字体加粗");
            Add("Ov.FontColor", "Font Color", "字体颜色");
            Add("Ov.OpacityInc", "Background Opacity Increase", "背景不透明度增加");
            Add("Ov.OpacityDec", "Background Opacity Decrease", "背景不透明度降低");
            Add("Ov.BgColor", "Background Color", "背景颜色");
            Add("Ov.OnlyMode", "Show only subtitles or translations", "仅显示字幕或翻译");
            Add("Ov.Switch", "Switch the order of subtitles and translations", "切换字幕与翻译的顺序");
            Add("Ov.ClickThrough", "Click Through", "点击穿透");

            // ---- Info page ----
            Add("Info.Contribute",
                "We welcome any form of contribution! You can provide suggestions or report bugs via GitHub issues, or contribute code directly by submitting Pull Requests.",
                "欢迎任何形式的贡献！你可以通过 GitHub Issues 提出建议或报告 Bug，也可以直接提交 Pull Request 贡献代码。");
            Add("Info.Star",
                "If this program is helpful to you, please consider giving us a star ✨!",
                "如果这个程序对你有帮助，欢迎给我们点一个 Star ✨！");
            Add("Info.Repository", "Repository:", "仓库：");
            Add("Info.Wiki", "Wiki:", "Wiki：");
            Add("Info.Maintainer", "Maintainer:", "维护者：");
            Add("Info.AuthorAnd", "(Author) and", "（作者）与");
            Add("Info.Version", "Version:", "版本：");

            // ---- Welcome window ----
            Add("Wel.Title", "Getting Started", "开始使用");
            Add("Wel.Welcome", "Welcome to LiveCaptions Translator!", "欢迎使用 LiveCaptions Translator！");
            Add("Wel.R1", "LiveCaptions Translator is based on Windows LiveCaptions.", "LiveCaptions Translator 基于 Windows 实时字幕。");
            Add("Wel.R2",
                "Before your first running, you need to configure it according to the following steps:\n",
                "首次运行前，请按照以下步骤进行配置：\n");
            Add("Wel.R3",
                "\n1. Open Windows LiveCaptions and follow the guide to download the necessary files for on-device speech recognition and language packs.",
                "\n1. 打开 Windows 实时字幕，按指引下载设备端语音识别所需的文件和语言包。");
            Add("Wel.R4", "\n2. Click the", "\n2. 点击");
            Add("Wel.R5", "⚙️ gear", "⚙️ 齿轮");
            Add("Wel.R6", "icon in Windows LiveCaptions to open the settings menu, and select", "图标打开设置菜单，然后选择");
            Add("Wel.R7", "\"Position > Overlaid on screen\"", "\"位置 > 覆盖在屏幕上\"");
            Add("Wel.R8", ".\n3. Click the", "。\n3. 点击");
            Add("Wel.R9", "\"Hide\"", "\"隐藏\"");
            Add("Wel.R10",
                "button located in the setting page of LiveCaptions Translator to hide Windows LiveCaptions.\n",
                "按钮（位于 LiveCaptions Translator 的设置页）以隐藏 Windows 实时字幕。\n");
            Add("Wel.R11",
                "\nThe program has automatically navigated to the setting page and opened Windows LiveCaptions.",
                "\n程序已自动跳转到设置页并打开了 Windows 实时字幕。");
            Add("Wel.R12",
                "After completing the configuration, you can switch back to the main page and enjoy real-time audio translation.\n",
                "完成配置后，切回主页即可享受实时音频翻译。\n");
            Add("Wel.R13", "\nFor more details, visit our wiki: ", "\n更多信息请访问我们的 Wiki：");
            Add("Wel.Tip", "\n\nTip:", "\n\n提示：");
            Add("Wel.R14", "To change the", "要修改");
            Add("Wel.R15", "Source Language", "源语言");
            Add("Wel.R16",
                ", please [Show] LiveCaptions on the setting page and do it in Windows LiveCaptions.",
                "，请在设置页 [显示] 实时字幕并在其中修改。");
            Add("Wel.Button", "I have fully understood and configured.", "我已了解并完成配置。");

            // ---- Translator loop texts ----
            Add("TR.Paused", "[Paused]", "[已暂停]");
            Add("TR.LiveCaptionsClosed",
                "[WARNING] LiveCaptions was unexpectedly closed, restarting...",
                "[WARNING] 实时字幕意外关闭，正在重启...");
            Add("TR.LogFailed", "[ERROR] Logging history failed.", "[ERROR] 记录历史失败。");

            // ---- Voice input ----
            Add("VI.OffTitle", "Voice Input is Off", "语音输入未启用");
            Add("VI.OffMsg",
                "Select Translated or Original mode in Settings first.",
                "请先在设置中选择翻译输入或原文直输模式。");
            Add("VI.EnabledTitle", "Voice Input Enabled", "语音输入已开启");
            Add("VI.EnabledMsg", "{0} | Hotkey: {1}", "{0} | 快捷键：{1}");
            Add("VI.DisabledTitle", "Voice Input Disabled", "语音输入已关闭");
            Add("VI.InvalidHotkey", "Invalid Hotkey", "快捷键无效");
            Add("VI.InvalidHotkeyMsg", "Please include Ctrl, Alt or Win.", "请包含 Ctrl、Alt 或 Win 修饰键。");
            Add("VI.HotkeyUpdated", "Hotkey Updated", "快捷键已更新");
            Add("VI.RegisterFailed", "[ERROR] Failed to register hotkey.", "[ERROR] 快捷键注册失败。");
            Add("VI.InputFailed", "[ERROR] Voice Input Failed", "[ERROR] 语音输入失败");
            Add("VI.InputFailedMsg",
                "SendInput error {0}. If the target app runs as administrator, run this app as administrator too.",
                "SendInput 错误码 {0}。如果目标程序以管理员身份运行，请以管理员身份运行本程序。");
            Add("VI.RegisterFailedMsg", "{0} may be taken by another app.", "{0} 可能被其他程序占用。");
            Add("VI.OverlayPrefix", "[Voice Input] ", "[语音输入] ");

            return d;
        }
    }
}
