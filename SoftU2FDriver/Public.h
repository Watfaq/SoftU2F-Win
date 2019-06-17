/*++

Module Name:

    public.h

Abstract:

    This module contains the common declarations shared by driver
    and user applications.

Environment:

    user and kernel

--*/

//
// Define an Interface Guid so that apps can find the device and talk to it.
//
#pragma once

#include <initguid.h>

// {DC3DE777-BC7E-4063-86C9-69422E319AF7}
DEFINE_GUID(GUID_DEVINTERFACE_SOFTU2F_FILTER,
	0xdc3de777, 0xbc7e, 0x4063, 0x86, 0xc9, 0x69, 0x42, 0x2e, 0x31, 0x9a, 0xf7);

// {600C7C9E-3DD0-4521-B931-4398F48A3C5D}
DEFINE_GUID (GUID_BUS_SoftU2F,
	0x600c7c9e, 0x3dd0, 0x4521, 0xb9, 0x31, 0x43, 0x98, 0xf4, 0x8a, 0x3c, 0x5d);

// {0CA1D3ED-8607-4C22-8AAF-3ECDDA1255DA}
DEFINE_GUID (GUID_DEVCLASS_SoftU2F,
	0x0ca1d3ed, 0x8607, 0x4c22, 0x8a, 0xaf, 0x3e, 0xcd, 0xda, 0x12, 0x55, 0xda);

#define MILLISECOND 10000
#define RELATIVE_MILLISECOND (-MILLISECOND)

#define SoftU2F_DEVICE_ID L"{600C7C9E-3DD0-4521-B931-4398F48A3C5D}\\SoftU2F\0"
#define MAX_ID_LEN 128

#define IOCTL_INDEX 0x800
#define IOCTL_SOFTU2F_FILTER_INIT CTL_CODE(FILE_DEVICE_KEYBOARD, IOCTL_INDEX, METHOD_BUFFERED, FILE_READ_DATA | FILE_WRITE_DATA)
#define IOCTL_SOFTU2F_FILTER_WRITE_DATA CTL_CODE(FILE_DEVICE_KEYBOARD, (IOCTL_INDEX + 1), METHOD_BUFFERED, FILE_READ_DATA | FILE_WRITE_DATA)

#define MAX_BCNT 7609