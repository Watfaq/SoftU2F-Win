#pragma once

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>
#include <initguid.h>


EXTERN_C_START

// {DC3DE777-BC7E-4063-86C9-69422E319AF7}
DEFINE_GUID(GUID_DEVINTERFACE_SOFTU2F_FILTER,
	0xdc3de777, 0xbc7e, 0x4063, 0x86, 0xc9, 0x69, 0x42, 0x2e, 0x31, 0x9a, 0xf7);

__declspec(dllexport) PWCHAR GetInterfaceDevicePath();

EXTERN_C_END