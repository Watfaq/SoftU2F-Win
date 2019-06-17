/*++

Module Name:

    device.h

Abstract:

    This file contains the device definitions.

Environment:

    Kernel-mode Driver Framework

--*/
#pragma once

#include <ntddk.h>
#include <hidport.h>
#include <wdf.h>
#include <vhf.h>
#include <ntstrsafe.h>
#include <ntstatus.h>
#include "U2F.h"
#include "Public.h"
#include "trace.h"



EXTERN_C_START

EVT_WDF_TIMER  TimeoutMessagesCleanup;

EVT_WDF_DEVICE_SELF_MANAGED_IO_INIT EvtDeviceSelfManagedIoInit;
EVT_WDF_DEVICE_SELF_MANAGED_IO_CLEANUP EvtDeviceSelfManagedIoCleanup;

EVT_WDF_DEVICE_SELF_MANAGED_IO_INIT RAWPDO_EvtDeviceSelfManagedIoInit;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL EvtIoDeviceControlForRawPdo;
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL EvtIoDeviceControlForMainPdo;

EVT_VHF_CLEANUP VhfSourceDeviceCleanup;
EVT_VHF_ASYNC_OPERATION HidWriteInputReport;

//
// The device context performs the same job as
// a WDM device extension in the driver frameworks
//
typedef struct _DEVICE_CONTEXT
{
	VHFHANDLE				VhfHandle;

	UINT32					cid;
	PU2FHID_MESSAGE			MessageList;
	KSPIN_LOCK				MessageProcessLock;
	WDFTIMER				TimeoutMessageCleanupTimer;

	WDFDEVICE				RawPdo;
	WDFQUEUE				RawQueue;

	WDFQUEUE				ManualQueue;

} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

//
// This macro will generate an inline function called DeviceGetContext
// which will be used to get a pointer to the device context memory
// in a type safe manner.
//
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, GetDeviceContext)

typedef struct _QUEUE_CONTEXT
{
	WDFQUEUE                Queue;
	PDEVICE_CONTEXT         DeviceContext;

} RAW_QUEUE_CONTEXT, *PRAW_QUEUE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(RAW_QUEUE_CONTEXT, GetRawQueueContext);

typedef struct _MANUAL_QUEUE_CONTEXT
{
	WDFQUEUE                Queue;
	PDEVICE_CONTEXT         DeviceContext;

} MANUAL_QUEUE_CONTEXT, * PMANUAL_QUEUE_CONTEXT;
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(MANUAL_QUEUE_CONTEXT, GetManualQueueContext);

// Raw PDO context.  
typedef struct _RAWPDO_DEVICE_CONTEXT
{

	// TODO; is this used
	ULONG InstanceNo;

	//
	// Queue of the parent device we will forward requests to
	//
	WDFQUEUE ParentQueue;

} RAWPDO_DEVICE_CONTEXT, * PRAWPDO_DEVICE_CONTEXT;
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(RAWPDO_DEVICE_CONTEXT, GetRawPdoDeviceContext)


NTSTATUS
CreateRawQueue(
	_In_  WDFDEVICE         Device,
	_Out_ WDFQUEUE          *Queue
);

NTSTATUS
CreateManualQueue(
	_In_  WDFDEVICE         Device,
	_Out_ WDFQUEUE* Queue
);

NTSTATUS
CreateTimer(
	_In_ WDFDEVICE	Device,
	_Out_ WDFTIMER* Timer
);

NTSTATUS
CreateRawPdo(
	_In_  WDFDEVICE         Device
);

//
// Function to initialize the device and its callbacks
//
NTSTATUS
SoftU2FCreateDevice(
	_Inout_ PWDFDEVICE_INIT DeviceInit
);


#pragma region U2FHID

NTSTATUS
HidErrorMessageSend(
	_In_
	VHFHANDLE           VhfHandle,
	_In_
	UINT32			    cid,
	UINT8				code
);

VOID
HidMessageRead(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PHID_XFER_PACKET HidTransferPacket
);

VOID
HidMessageHandle(
	_In_ PDEVICE_CONTEXT deviceContext
);

NTSTATUS
HidMessageSend(
	_In_ VHFHANDLE VhfHandle,
	_In_ PU2FHID_MESSAGE message
);

BOOLEAN
HidMessageIsTimeout(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
);

#pragma endregion


#pragma region MessageList

PU2FHID_MESSAGE
MessageListFind(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ UINT32 cid
);

UINT32
MessageListCount(
	_In_ PDEVICE_CONTEXT deviceContext
);

VOID
MessageListRemove(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
);

PU2FHID_MESSAGE
MessageListCreate(
	_In_ PDEVICE_CONTEXT deviceContext
);

VOID
MessageFree(
	_In_ PU2FHID_MESSAGE message
);

PU2FHID_MESSAGE
MessageAlloc
(
	_In_ PDEVICE_CONTEXT deviceContext
);

VOID
HidMessageFinalize(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
);

BOOLEAN
HidMessageIsComplete(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
);

#pragma endregion

#pragma region Message Handlers

typedef VOID U2FMessageHandler(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
);

U2FMessageHandler U2FHandleMessageInit;

U2FMessageHandler U2FHandleMessagePing;

U2FMessageHandler U2FHandleMessageWink;

U2FMessageHandler U2FHandleMessageSync;

U2FMessageHandler U2FHandleMessageMsg;

#pragma endregion

EXTERN_C_END
