#include "shared.h"
#include "MinHook.h"

static SharedState* state;
static HANDLE mapping;
static INIT_ONCE once = INIT_ONCE_STATIC_INIT;
static decltype(&ShowWindow) originalShow;
static decltype(&ShowWindowAsync) originalAsync;
static decltype(&SetWindowPos) originalPosition;
static LPVOID targets[3];

static bool ShouldBlock(HWND window) {
    if (!state || !state->active || !state->enabled ||
        (DWORD)(GetTickCount() - state->heartbeat) > 5000) return false;
    // Only the selected browser process receives this hook. Match the reference
    // mod's class and flag mask; unrelated windows keep their normal behavior.
    const LONG exMask = WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST | WS_EX_TRANSPARENT;
    LONG ex = GetWindowLongW(window, GWL_EXSTYLE);
    if ((ex & exMask) != exMask) return false;
    LONG style = GetWindowLongW(window, GWL_STYLE);
    if (!(style & WS_POPUP) || (style & WS_SYSMENU)) return false;
    wchar_t name[256];
    return GetClassNameW(window, name, 256) && wcscmp(name, L"Chrome_WidgetWin_1") == 0;
}
static BOOL WINAPI BlockShow(HWND window, int command) {
    if (command != SW_HIDE && ShouldBlock(window)) {
        InterlockedIncrement(&state->blocked);
        return IsWindowVisible(window); // ShowWindow returns previous visibility.
    }
    return originalShow(window, command);
}
static BOOL WINAPI BlockAsync(HWND window, int command) {
    if (command != SW_HIDE && ShouldBlock(window)) {
        InterlockedIncrement(&state->blocked);
        return TRUE;
    }
    return originalAsync(window, command);
}
static BOOL WINAPI BlockPosition(HWND window, HWND after, int x, int y, int cx, int cy, UINT flags) {
    if ((flags & SWP_SHOWWINDOW) && ShouldBlock(window)) {
        InterlockedIncrement(&state->blocked);
        flags &= ~SWP_SHOWWINDOW; // Preserve sizing and positioning, without showing.
    }
    return originalPosition(window, after, x, y, cx, cy, flags);
}

static BOOL CALLBACK Initialize(PINIT_ONCE, PVOID, PVOID*) {
    wchar_t name[96]; MappingName(name, GetCurrentProcessId());
    mapping = OpenFileMappingW(FILE_MAP_ALL_ACCESS, FALSE, name);
    if (!mapping) return FALSE;
    state = (SharedState*)MapViewOfFile(mapping, FILE_MAP_ALL_ACCESS, 0, 0, sizeof(SharedState));
    if (!state) { CloseHandle(mapping); mapping = nullptr; return FALSE; }
    if (state->version != 2) { UnmapViewOfFile(state); state = nullptr; CloseHandle(mapping); mapping = nullptr; return FALSE; }
    // Pin before installing detours: an active trampoline must never point at an
    // unloaded DLL. On pause/exit detours pass through; closing the browser unloads it.
    HMODULE pinned;
    if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
        reinterpret_cast<LPCWSTR>(&Initialize), &pinned)) { state->error = -1; return TRUE; }
    MH_STATUS result = MH_Initialize();
    if (result != MH_OK) { state->error = result; return TRUE; }
    result = MH_CreateHookApiEx(L"user32.dll", "ShowWindow", (LPVOID)&BlockShow, (LPVOID*)&originalShow, &targets[0]);
    if (result == MH_OK) result = MH_CreateHookApiEx(L"user32.dll", "ShowWindowAsync", (LPVOID)&BlockAsync, (LPVOID*)&originalAsync, &targets[1]);
    if (result == MH_OK) result = MH_CreateHookApiEx(L"user32.dll", "SetWindowPos", (LPVOID)&BlockPosition, (LPVOID*)&originalPosition, &targets[2]);
    if (result == MH_OK) result = MH_EnableHook(MH_ALL_HOOKS);
    if (result != MH_OK) {
        state->error = result;
        // Retain initialized storage even on failure; never unload possible live code.
        return TRUE;
    }
    InterlockedExchange(&state->active, 1);
    return TRUE;
}

extern "C" __declspec(dllexport) DWORD WINAPI Start(LPVOID) {
    InitOnceExecuteOnce(&once, Initialize, nullptr, nullptr);
    return state && state->active == 1 ? 0 : 1;
}

