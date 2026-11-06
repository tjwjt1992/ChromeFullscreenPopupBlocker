using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

internal sealed class MainForm : Form {
    readonly ComboBox language = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    readonly ComboBox browsers = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    readonly Label status = new Label { AutoSize = false, Dock = DockStyle.Fill };
    readonly Button attach = new Button { AutoSize = true };
    readonly Button refresh = new Button { AutoSize = true };
    readonly CheckBox pause = new CheckBox { Checked = true, AutoSize = true };
    readonly Label hint = new Label { Dock = DockStyle.Fill };
    readonly Font englishFont = new Font("Segoe UI", 10);
    readonly Font chineseFont = new Font("Microsoft YaHei UI", 10);
    readonly NotifyIcon tray;
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 300 };
    Func<string> statusText = delegate { return UiText.Get("Open your browser, select it, then click Attach.", "打开浏览器，在列表中选择它，然后点击“连接”。"); };
    Attachment current; DateTime started;
    public MainForm() {
        ClientSize = new Size(550, 251); MinimumSize = new Size(480, 276);
        Font = UiText.IsChinese ? chineseFont : englishFont;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 5, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        var languageRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        languageRow.Controls.Add(new Label { Text = "Language / 语言", AutoSize = true, Margin = new Padding(3, 6, 8, 0) });
        language.Items.AddRange(new object[] { "English", "中文" });
        language.SelectedIndex = UiText.IsChinese ? 1 : 0;
        languageRow.Controls.Add(language);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        buttons.Controls.Add(attach); buttons.Controls.Add(refresh); buttons.Controls.Add(pause);
        layout.Controls.Add(languageRow, 0, 0); layout.Controls.Add(browsers, 0, 1);
        layout.Controls.Add(buttons, 0, 2); layout.Controls.Add(status, 0, 3); layout.Controls.Add(hint, 0, 4);
        Controls.Add(layout);
        refresh.Click += delegate { RefreshBrowsers(); };
        attach.Click += delegate {
            if (current != null) { Detach(delegate { return UiText.Get("Detached. Normal popup behavior restored.", "已断开连接，弹窗已恢复正常。"); }); return; }
            var item = browsers.SelectedItem as BrowserItem;
            if (item == null) return;
            try {
                current = new Attachment(item); started = DateTime.UtcNow; browsers.Enabled = false; pause.Checked = true;
                SetStatus(delegate { return UiText.Get("Attaching — wait for Ready before entering fullscreen.", "正在连接，请等待“就绪”后再进入全屏。"); });
                ApplyLanguage();
            } catch (Exception ex) { SetStatus(delegate { return ex.Message; }); }
        };
        pause.CheckedChanged += delegate { if (current != null) current.Enabled = pause.Checked; };
        var menu = new ContextMenuStrip();
        menu.Items.Add("", null, delegate { Show(); WindowState = FormWindowState.Normal; Activate(); });
        menu.Items.Add("", null, delegate { Close(); });
        tray = new NotifyIcon { Icon = SystemIcons.Application, ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += delegate { Show(); WindowState = FormWindowState.Normal; Activate(); };
        language.SelectedIndexChanged += delegate {
            bool saved = UiText.SelectLanguage(language.SelectedIndex == 1);
            ApplyLanguage();
            if (!saved) MessageBox.Show(this, UiText.Get("Could not save your language preference. It will apply for this session only.", "无法保存语言设置，本次运行仍会使用所选语言。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        ApplyLanguage();
        Resize += delegate { if (WindowState == FormWindowState.Minimized) Hide(); };
        timer.Tick += delegate { TickStatus(); };
        timer.Start(); RefreshBrowsers();
    }
    void ApplyLanguage() {
        SuspendLayout();
        Font = UiText.IsChinese ? chineseFont : englishFont;
        Text = UiText.Get("Fullscreen Popup Blocker (x64)", "全屏提示拦截器 (x64)");
        attach.Text = current == null ? UiText.Get("Attach", "连接") : UiText.Get("Detach", "断开");
        refresh.Text = UiText.Get("Refresh", "刷新");
        pause.Text = UiText.Get("Enabled", "启用拦截");
        hint.Text = UiText.Get("Minimize to tray. Closing this window stops blocking.", "最小化后驻留托盘，关闭窗口将停止拦截。");
        tray.Text = UiText.Get("Fullscreen Popup Blocker", "全屏提示拦截器");
        tray.ContextMenuStrip.Items[0].Text = UiText.Get("Open", "打开");
        tray.ContextMenuStrip.Items[1].Text = UiText.Get("Exit", "退出");
        status.Text = statusText();
        ResumeLayout(true);
    }
    void SetStatus(Func<string> text) { statusText = text; status.Text = text(); }
    void RefreshBrowsers() {
        if (current != null) return;
        browsers.Items.Clear();
        foreach (BrowserItem item in Api.Browsers()) browsers.Items.Add(item);
        if (browsers.Items.Count > 0) browsers.SelectedIndex = 0;
    }
    void TickStatus() {
        if (current == null) return;
        try {
            using (var p = Process.GetProcessById(current.Pid)) if (p.HasExited) throw new ArgumentException();
            current.Heartbeat();
            int error = current.Error;
            if (error != 0) { Detach(delegate { return string.Format(UiText.Get("Hook setup failed (code {0}). Restart the browser before retrying.", "拦截初始化失败（错误码 {0}），请重启浏览器后重试。"), error); }); return; }
            if (current.Active == 1) {
                current.ReleaseLocalLibrary();
                SetStatus(delegate { return (pause.Checked ? UiText.Get("Ready — in-process hook active. You can enter fullscreen.", "就绪 — 拦截已生效，可以进入全屏。") : UiText.Get("Paused — normal popup behavior restored.", "已暂停 — 弹窗已恢复正常。")) + "\r\n" + UiText.Get("Blocked show calls: ", "已拦截的显示调用次数：") + current.Blocked; });
            } else if ((DateTime.UtcNow - started).TotalSeconds > 12)
                Detach(delegate { return UiText.Get("Could not confirm the hook. Browser security may have blocked it; no protection is active.", "无法确认拦截是否生效，可能被浏览器安全机制阻止；当前未启用拦截。"); });
        } catch (ArgumentException) { Detach(delegate { return UiText.Get("Browser closed. Open it and attach again.", "浏览器已关闭，请打开后重新连接。"); }); RefreshBrowsers(); }
        catch (InvalidOperationException) { Detach(delegate { return UiText.Get("Browser is no longer available.", "浏览器已不可用。"); }); }
    }
    void Detach(Func<string> message) {
        if (current != null) { current.Dispose(); current = null; }
        browsers.Enabled = true;
        SetStatus(message);
        ApplyLanguage();
    }
    protected override void Dispose(bool disposing) {
        if (disposing) { timer.Dispose(); if (current != null) current.Dispose(); tray.Visible = false; tray.ContextMenuStrip.Dispose(); tray.Dispose(); }
        base.Dispose(disposing);
        if (disposing) { englishFont.Dispose(); chineseFont.Dispose(); }
    }
}
