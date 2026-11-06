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

internal sealed class Attachment : IDisposable {
    MemoryMappedFile map; MemoryMappedViewAccessor view;
    IntPtr library;
    public readonly int Pid;
    public int Active { get { return view.ReadInt32(8); } }
    public int Error { get { return view.ReadInt32(12); } }
    public int Blocked { get { return view.ReadInt32(16); } }
    public bool Enabled { set { view.Write(4, value ? 1 : 0); } }
    public void Heartbeat() { view.Write(24, unchecked((uint)Environment.TickCount)); }
    public Attachment(BrowserItem browser) {
        Pid = browser.Pid;
        try {
            using (var process = Process.GetProcessById(Pid)) {
                bool wow;
                if (!Api.IsWow64Process(process.Handle, out wow)) throw new System.ComponentModel.Win32Exception();
                if (wow || !Environment.Is64BitProcess)
                    throw UiText.Error("Only x64 browsers are supported. Use the 64-bit version of your browser.", "仅支持 x64 浏览器，请使用 64 位版本的浏览器。");
            }
            map = MemoryMappedFile.CreateOrOpen("Local\\FullscreenPopupBlocker.v2." + Pid, 28, MemoryMappedFileAccess.ReadWrite);
            view = map.CreateViewAccessor(0, 28, MemoryMappedFileAccess.ReadWrite);
            view.Write(0, 2); view.Write(20, Process.GetCurrentProcess().Id);
            Heartbeat(); Enabled = true;
            if (Error != 0) throw UiText.Error("Previous hook setup failed (code " + Error + "). Restart this browser before retrying.", "上次拦截初始化失败（错误码 " + Error + "），请重启浏览器后重试。");
            if (Active == 1) return;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PopupHook-x64.dll");
            library = Api.LoadLibrary(path);
            if (library == IntPtr.Zero) throw UiText.Error(Marshal.GetLastWin32Error(), "Could not load the companion DLL. Keep it beside the EXE.", "无法加载配套 DLL，请将它与 EXE 放在同一文件夹中。");
            IntPtr entry = Api.GetProcAddress(library, "Start");
            if (entry == IntPtr.Zero) throw UiText.Error("The companion DLL has no hook entry point.", "配套 DLL 缺少拦截入口，请使用匹配的 DLL 文件。");
            Inject(path, entry.ToInt64() - library.ToInt64());
        } catch { Dispose(); throw; }
    }
    public void ReleaseLocalLibrary() {
        if (library != IntPtr.Zero) { Api.FreeLibrary(library); library = IntPtr.Zero; }
    }
    static IntPtr Add(IntPtr pointer, long offset) { return new IntPtr(pointer.ToInt64() + offset); }
    void Inject(string path, long startOffset) {
        IntPtr handle = Api.OpenProcess(0x043A, false, Pid);
        if (handle == IntPtr.Zero) throw UiText.Error(Marshal.GetLastWin32Error(), "Cannot attach to this browser. Check that it is not running as administrator.", "无法连接此浏览器，请检查它是否以管理员身份运行。");
        IntPtr memory = IntPtr.Zero;
        try {
            IntPtr load = Api.GetProcAddress(Api.GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            string ownerName = null; long offset = 0;
            using (var local = Process.GetCurrentProcess()) foreach (ProcessModule module in local.Modules) {
                long address = load.ToInt64(), start = module.BaseAddress.ToInt64();
                if (address >= start && address < start + module.ModuleMemorySize) { ownerName = module.ModuleName; offset = address - start; break; }
            }
            IntPtr remoteLoad = IntPtr.Zero;
            using (var remote = Process.GetProcessById(Pid)) foreach (ProcessModule module in remote.Modules)
                if (string.Equals(module.ModuleName, ownerName, StringComparison.OrdinalIgnoreCase)) { remoteLoad = Add(module.BaseAddress, offset); break; }
            if (remoteLoad == IntPtr.Zero) throw UiText.Error("Could not resolve the browser's Windows loader.", "无法定位浏览器进程中的 Windows 加载函数。");
            byte[] bytes = Encoding.Unicode.GetBytes(path + "\0");
            memory = Api.VirtualAllocEx(handle, IntPtr.Zero, (UIntPtr)bytes.Length, 0x3000, 4);
            if (memory == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            UIntPtr written;
            if (!Api.WriteProcessMemory(handle, memory, bytes, (UIntPtr)bytes.Length, out written) || written.ToUInt64() != (ulong)bytes.Length)
                throw new System.ComponentModel.Win32Exception();
            IntPtr thread = Api.CreateRemoteThread(handle, IntPtr.Zero, UIntPtr.Zero, remoteLoad, memory, 0, IntPtr.Zero);
            if (thread == IntPtr.Zero) throw UiText.Error(Marshal.GetLastWin32Error(), "Windows blocked loading the hook into the browser.", "Windows 阻止了向浏览器加载拦截模块。");
            try {
                if (Api.WaitForSingleObject(thread, 10000) != 0) { memory = IntPtr.Zero; throw UiText.Error("Browser did not finish loading the hook. Restart it before retrying.", "浏览器未能完成拦截模块的加载，请重启浏览器后重试。"); }
            } finally { Api.CloseHandle(thread); }
            IntPtr remoteModule = IntPtr.Zero;
            using (var remote = Process.GetProcessById(Pid)) foreach (ProcessModule module in remote.Modules)
                if (string.Equals(module.FileName, path, StringComparison.OrdinalIgnoreCase)) { remoteModule = module.BaseAddress; break; }
            if (remoteModule == IntPtr.Zero) throw UiText.Error("Browser security blocked the companion DLL. The hook is not active.", "浏览器安全机制阻止了配套 DLL，拦截未生效。");
            Heartbeat();
            thread = Api.CreateRemoteThread(handle, IntPtr.Zero, UIntPtr.Zero, Add(remoteModule, startOffset), IntPtr.Zero, 0, IntPtr.Zero);
            if (thread == IntPtr.Zero) throw UiText.Error(Marshal.GetLastWin32Error(), "Could not initialize the hook.", "无法初始化拦截模块。");
            try {
                if (Api.WaitForSingleObject(thread, 10000) != 0) throw UiText.Error("Hook initialization timed out. Restart the browser before retrying.", "拦截模块初始化超时，请重启浏览器后重试。");
            } finally { Api.CloseHandle(thread); }
        } finally { if (memory != IntPtr.Zero) Api.VirtualFreeEx(handle, memory, UIntPtr.Zero, 0x8000); Api.CloseHandle(handle); }
    }
    public void Dispose() {
        if (view != null) view.Write(4, 0);
        ReleaseLocalLibrary();
        if (view != null) { view.Dispose(); view = null; }
        if (map != null) { map.Dispose(); map = null; }
    }
}

