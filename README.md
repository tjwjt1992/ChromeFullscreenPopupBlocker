# Fullscreen Popup Blocker

A portable utility that blocks Chromium's initial “Press Esc/F11 to exit
fullscreen” popup. Requires Windows 10/11 x64, .NET Framework 4.8, and an x64
browser.

Choose **English** or **中文** from the **Language / 语言** selector at the top.
Changes take effect immediately, and your choice is remembered for next time.
The default is English.

## Use

Keep `FullscreenPopupBlocker-x64.exe` and `PopupHook-x64.dll` together. Run
the EXE, select your browser, click **Attach**, and wait for **Ready** before
entering fullscreen. Minimize to tray; pause, detach, or exit to stop blocking.
Attach again after restarting the browser.

The utility loads a DLL into the selected browser. Other popups with the same
window properties may also be blocked, and browser security or updates may
prevent it from working. Close the browser before replacing the DLL.

## Build

Install Visual Studio / Build Tools 2022 or newer with **Desktop development
with C++**, a Windows SDK, and .NET Framework 4.8. Run from the repository root:

```powershell
.\scripts\build.ps1
.\scripts\package.ps1
```

Binaries go to `build/x64/`; the portable ZIP and checksum go to `dist/`.

To publish a release, commit the desired version in `VERSION`, then push a
matching tag (for example, `git tag v1.0.0` and `git push origin v1.0.0`).
GitHub Actions builds x64 and publishes the ZIP, checksum, and generated
release notes. Tags with a suffix such as `v1.1.0-beta.1` create prereleases.

## 中文说明

拦截 Chromium 浏览器进入全屏时的“按 Esc/F11 退出全屏”提示。
需要 Windows 10/11 x64、.NET Framework 4.8 和 x64 浏览器。
在顶部的 **Language / 语言** 下拉框中选择 **中文** 或 **English**，
切换立即生效，下次启动时会记住所选语言。默认使用英文。

将 EXE 和 DLL 放在同一文件夹中，运行 `FullscreenPopupBlocker-x64.exe`，
选择浏览器并点击**连接**，等待**就绪**后再进入全屏。
最小化后驻留托盘；取消**启用拦截**、点击**断开**或退出程序可停止拦截。
浏览器重启后需要重新连接，更换 DLL 前请先关闭浏览器。
程序会向浏览器加载 DLL；浏览器安全机制或更新可能导致拦截失效，
具有相同窗口属性的其他弹窗也可能被拦截。

编译需要 Visual Studio / Build Tools 2022 或更新版本（安装“使用 C++ 的桌面开发”、
Windows SDK）及 .NET Framework 4.8，依次运行上方两条命令即可生成程序和压缩包。

发布时先提交 `VERSION` 中的版本号，再推送对应标签（如 `v1.0.0`）。
GitHub Actions 会自动编译 x64 版本并发布压缩包、校验和及更新说明；
带后缀的标签（如 `v1.1.0-beta.1`）会发布为预发布版本。

## License

Project code: [WTFPL v2](LICENSE).
[MinHook v1.3.4](https://github.com/TsudaKageyu/minhook/tree/v1.3.4)
(`c3fcafdc10146beb5919319d0683e44e3c30d537`) and its bundled HDE components
retain their [own license notices](third_party/minhook/LICENSE.txt).
Inspired by [remove-chrome-fullscreen-popup](https://github.com/lorenzoc01/remove-chrome-fullscreen-popup).
