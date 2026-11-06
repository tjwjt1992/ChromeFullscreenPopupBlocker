# Chrome Fullscreen Popup Blocker

**[Download latest release / 下载最新版本](https://github.com/tjwjt1992/ChromeFullscreenPopupBlocker/releases/latest)**

A lightweight, open-source utility that blocks Chromium's recurring “Press
Esc/F11 to exit fullscreen” popup. Requires Windows 10/11 x64, .NET Framework
4.8, and an x64 Chromium-based browser.

轻量开源工具，拦截 Chrome 等 Chromium 内核浏览器进入全屏时没完没了的“按 Esc/F11 退出全屏”提示。
需要 Windows 10/11 x64、.NET Framework 4.8 和 x64 版本的浏览器。

## Usage

Keep `FullscreenPopupBlocker-x64.exe` and `PopupHook-x64.dll` together. Run
the EXE, select your browser, click **Attach**, and wait for **Ready** before
entering fullscreen. Minimize to tray; pause, detach, or exit to stop blocking.
Attach again after restarting the browser.

The utility loads a DLL into the selected browser. Other popups with the same
window properties may also be blocked, and browser security or updates may
prevent it from working. Close the browser before replacing the DLL.

## 使用说明

将 EXE 和 DLL 放在同一文件夹中，运行 `FullscreenPopupBlocker-x64.exe`，
选择浏览器并点击 **连接**，等待 **就绪** 后再进入全屏。
最小化后驻留托盘；取消 **启用拦截**、点击 **断开** 或退出程序可停止拦截。
浏览器重启后需要重新连接，更换 DLL 前请先关闭浏览器。

程序会向浏览器加载 DLL；浏览器安全机制或更新可能导致拦截失效，
具有相同窗口属性的其他弹窗也可能被拦截。

## Build

Install Visual Studio / Build Tools 2022 or newer with **Desktop development
with C++**, a Windows SDK, and .NET Framework 4.8. Run from the repository root:

```powershell
.\scripts\build.ps1
.\scripts\package.ps1
```

Binaries go to `build/x64/`; the portable ZIP and checksum go to `dist/`.

## License

Project code: [WTFPL v2](LICENSE).
[MinHook v1.3.4](https://github.com/TsudaKageyu/minhook/tree/v1.3.4)
(`c3fcafdc10146beb5919319d0683e44e3c30d537`) and its bundled HDE components
retain their [own license notices](third_party/minhook/LICENSE.txt).
Inspired by [remove-chrome-fullscreen-popup](https://github.com/lorenzoc01/remove-chrome-fullscreen-popup).
