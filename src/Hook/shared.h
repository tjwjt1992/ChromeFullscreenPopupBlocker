#pragma once
#include <windows.h>
#include <stdio.h>
// DWORD fields avoid cross-bitness alignment differences and torn heartbeat reads.
struct SharedState {
    volatile LONG version, enabled, active, error, blocked, owner;
    volatile DWORD heartbeat;
};
inline void MappingName(wchar_t* buffer, DWORD pid) {
    swprintf_s(buffer, 96, L"Local\\FullscreenPopupBlocker.v2.%lu", pid);
}

