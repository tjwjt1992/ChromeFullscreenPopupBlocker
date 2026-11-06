using System;
using System.ComponentModel;
using System.IO;
using System.Security;

internal static class UiText {
    static readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FullscreenPopupBlocker", "language.txt");
    public static bool IsChinese { get; private set; }

    public static void LoadLanguage() {
        IsChinese = false;
        try { IsChinese = File.ReadAllText(settingsPath).Trim() == "zh-CN"; }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (SecurityException) { }
    }

    public static bool SelectLanguage(bool chinese) {
        IsChinese = chinese;
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllText(settingsPath, chinese ? "zh-CN" : "en");
            return true;
        } catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (SecurityException) { return false; }
    }

    public static string Get(string english, string chinese) {
        return IsChinese ? chinese : english;
    }

    public static Exception Error(string english, string chinese) {
        return new TranslatedException(english, chinese);
    }

    public static Exception Error(int code, string english, string chinese) {
        return new TranslatedWin32Exception(code, english, chinese);
    }

    sealed class TranslatedException : InvalidOperationException {
        readonly string english, chinese;
        public TranslatedException(string english, string chinese) { this.english = english; this.chinese = chinese; }
        public override string Message { get { return Get(english, chinese); } }
    }

    sealed class TranslatedWin32Exception : Win32Exception {
        readonly string english, chinese;
        public TranslatedWin32Exception(int code, string english, string chinese) : base(code) { this.english = english; this.chinese = chinese; }
        public override string Message { get { return Get(english, chinese); } }
    }
}
