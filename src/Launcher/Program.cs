using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class Program {
    [STAThread] static int Main(string[] args) {
        UiText.LoadLanguage();
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        bool created;
        using (var mutex = new Mutex(true, "Local\\FullscreenPopupBlocker.Launcher.v2", out created)) {
            if (!created) { MessageBox.Show(UiText.AppName + UiText.Get(" is already running. Look for its tray icon.", "已在运行，请查看系统托盘图标。")); return 0; }
            using (var form = new MainForm()) Application.Run(form);
        }
        return 0;
    }
}

