/*++

Module Name:

	device.c - Device handling events for example driver.

Abstract:

   This file contains the device entry points and callbacks.

Environment:

	Kernel-mode Driver Framework

--*/

#include "Device.h"
#include "U2F.h"
#include "device.tmh"

#ifdef ALLOC_PRAGMA
#pragma alloc_text (PAGE, SoftU2FCreateDevice)
#pragma alloc_text (PAGE, EvtDeviceSelfManagedIoInit)
#pragma alloc_text (PAGE, EvtDeviceSelfManagedIoCleanup)
#endif

#pragma warning(disable: 4100) // unreferenced formal parameter
#pragma warning(disable: 4244) // possible loss of data

ULONG InstanceNo = 0;

UCHAR SoftU2FHIDReportDescriptor[] = {
	0x06, 0xD0, 0xF1, // Usage Page (Reserved 0xF1D0)

	0x09, 0x01,       // Usage (0x01)

	0xA1, 0x01,       // Collection (Application)

	0x09, 0x20,       //   Usage (0x20)

	0x15, 0x00,       //   Logical Minimum (0)

	0x26, 0xFF, 0x00, //   Logical Maximum (255)

	0x75, 0x08,       //   Report Size (8)

	0x95, 0x40,       //   Report Count (64)

	0x81, 0x02,       //   Input (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null

					  //   Position)

	0x09, 0x21,       //   Usage (0x21)

	0x15, 0x00,       //   Logical Minimum (0)

	0x26, 0xFF, 0x00, //   Logical Maximum (255)

	0x75, 0x08,       //   Report Size (8)

	0x95, 0x40,       //   Report Count (64)

	0x91, 0x02,       //   Output (Data,Var,Abs,No Wrap,Linear,Preferred State,No Null
					  //   Position,Non-volatile)
	0xC0,             // End Collection

};

NTSTATUS
SoftU2FCreateDevice(
	_Inout_ PWDFDEVICE_INIT DeviceInit
)
/*++

Routine Description:

	Worker routine called to create a device and its software resources.

Arguments:

	DeviceInit - Pointer to an opaque init structure. Memory for this
					structure will be freed by the framework when the WdfDeviceCreate
					succeeds. So don't access the structure after that point.

Return Value:

	NTSTATUS

--*/
{
	WDF_OBJECT_ATTRIBUTES deviceAttributes;
	PDEVICE_CONTEXT deviceContext;
	VHF_CONFIG vhfConfig;
	WDFDEVICE device;
	NTSTATUS status;

	WDF_PNPPOWER_EVENT_CALLBACKS    wdfPnpPowerCallbacks;

	PAGED_CODE();

	WdfFdoInitSetFilter(DeviceInit);

	WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&wdfPnpPowerCallbacks);
	wdfPnpPowerCallbacks.EvtDeviceSelfManagedIoInit = EvtDeviceSelfManagedIoInit;
	wdfPnpPowerCallbacks.EvtDeviceSelfManagedIoCleanup = EvtDeviceSelfManagedIoCleanup;
	WdfDeviceInitSetPnpPowerEventCallbacks(DeviceInit, &wdfPnpPowerCallbacks);

	WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&deviceAttributes, DEVICE_CONTEXT);

	status = WdfDeviceCreate(&DeviceInit, &deviceAttributes, &device);

	if (!NT_SUCCESS(status)) {
		return status;
	}

	deviceContext = GetDeviceContext(device);

	VHF_CONFIG_INIT(&vhfConfig,
		WdfDeviceWdmGetDeviceObject(device),
		sizeof(SoftU2FHIDReportDescriptor),
		SoftU2FHIDReportDescriptor);

	vhfConfig.VendorID = 0x08;
	vhfConfig.ProductID = 0x09;
	vhfConfig.VersionNumber = 1;
	vhfConfig.VhfClientContext = deviceContext;
	vhfConfig.EvtVhfAsyncOperationWriteReport = HidWriteInputReport;

	status = VhfCreate(&vhfConfig, &deviceContext->VhfHandle);

	if (!NT_SUCCESS(status))
	{
		TraceEvents(TRACE_LEVEL_ERROR, TRACE_DEVICE, "VhfCreate failed %!STATUS!", status);
		return status;
	}

	KeInitializeSpinLock(&deviceContext->MessageProcessLock);

	status = CreateRawQueue(device, &deviceContext->RawQueue);
	if (!NT_SUCCESS(status))
	{
		return status;
	}

	status = CreateManualQueue(device, &deviceContext->ManualQueue);

	if (!NT_SUCCESS(status))
	{
		return status;
	}

	status = CreateTimer(device, &deviceContext->TimeoutMessageCleanupTimer);
	if (!NT_SUCCESS(status))
	{
		return status;
	}

	status = CreateRawPdo(device);
	if (!NT_SUCCESS(status))
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Failed to create Raw Pdo\n"));
		return status;
	}

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Driver installed and loaded\n"));
	return status;
}

NTSTATUS
EvtDeviceSelfManagedIoInit(
	WDFDEVICE WdfDevice
)
{
	PDEVICE_CONTEXT	deviceContext;
	NTSTATUS        status;

	PAGED_CODE();

	deviceContext = GetDeviceContext(WdfDevice);

	status = VhfStart(deviceContext->VhfHandle);

	if (!NT_SUCCESS(status))
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "VhfStart failed %d\n", status));
	}

	return status;
}

VOID
EvtDeviceSelfManagedIoCleanup(
	WDFDEVICE WdfDevice
)
{
	PDEVICE_CONTEXT	deviceContext;

	PAGED_CODE();

	deviceContext = GetDeviceContext(WdfDevice);

	if (deviceContext->VhfHandle)
	{
		VhfDelete(deviceContext->VhfHandle, FALSE);		
	}
}

NTSTATUS RAWPDO_EvtDeviceSelfManagedIoInit(
	_In_ WDFDEVICE Device
)
{
	NTSTATUS status = STATUS_SUCCESS;
	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "RawPdo started\n"));
	return status;
}

VOID
HidWriteInputReport(
	_In_ PVOID VhfClientContext,
	_In_ VHFOPERATIONHANDLE VhfOperationHandle,
	_In_opt_ PVOID VhfOperationContext,
	_In_ PHID_XFER_PACKET HidTransferPacket
)
{
	// this is where the application started to request U2F conversation
	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "HidWriteInputReport called\n"));

	NTSTATUS status;
	PDEVICE_CONTEXT deviceContext = (PDEVICE_CONTEXT)VhfClientContext;
	KIRQL OldIrql;


	KeAcquireSpinLock(&deviceContext->MessageProcessLock, &OldIrql);

	HidMessageRead(deviceContext, HidTransferPacket);

	HidMessageHandle(deviceContext);

	KeReleaseSpinLock(&deviceContext->MessageProcessLock, OldIrql);

	status = VhfAsyncOperationComplete(VhfOperationHandle, STATUS_SUCCESS);

	if (!NT_SUCCESS(status))
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "WriteInput report respond failed with status %x\n", status));
	}
	else {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "WriteInput report responded\n"));
	}
}

VOID
HidMessageRead(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PHID_XFER_PACKET HidTransferPacket
)
{
	PU2FHID_MESSAGE message;
	UINT32 nData;
	PU2FHID_FRAME frame;
	PUCHAR data;

	frame = (PU2FHID_FRAME)HidTransferPacket->reportBuffer;

	if (frame->cid == 0x0) {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Frame with cid 0.\n"));
		HidErrorMessageSend(deviceContext->VhfHandle, frame->cid, ERR_INVALID_CID);
		return;
	}

	message = MessageListFind(deviceContext, frame->cid);

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "MessageListFind result for cid: %d, %d. cmd: %d, MessateListCount: %d", frame->cid, !!message, frame->init.cmd, MessageListCount(deviceContext)));

	switch (FRAME_TYPE(*frame))
	{
	case TYPE_INIT:
		if (message) {
			if (frame->init.cmd == U2FHID_INIT) {
				KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "U2FHID_INIT while waiting for CONT. Resetting.\n"));
				MessageListRemove(deviceContext, message);
			}
			else {
				KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "INIT frame out of order.Bailing.\n"));
				HidErrorMessageSend(deviceContext->VhfHandle, frame->cid, ERR_INVALID_SEQ);
				MessageListRemove(deviceContext, message);
				return;
			}
		}
		else if (frame->init.cmd == U2FHID_SYNC) {
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "SYNC frame out of order. Bailing.\n"));
			HidErrorMessageSend(deviceContext->VhfHandle, frame->cid, ERR_INVALID_CMD);
			return;
		}
		else if (frame->init.cmd != U2FHID_INIT && MessageListCount(deviceContext) > 0)
		{
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "INIT frame while waiting for CONT on other CID.\n"));
			HidErrorMessageSend(deviceContext->VhfHandle, frame->cid, ERR_CHANNEL_BUSY);
			return;
		}

		if (frame->cid == CID_BROADCAST && frame->init.cmd != U2FHID_INIT)
		{
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Non U2FHID_INIT message on broadcast CID.\n"));
			HidErrorMessageSend(deviceContext->VhfHandle, frame->cid, ERR_INVALID_CID);
			return;
		}


		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "got frame from channel: %d\n", frame->cid));

		message = MessageListCreate(deviceContext);
		if (!message)
		{
			return;
		}

		message->cmd = frame->init.cmd;
		message->cid = frame->cid;
		message->bcnt = MESSAGE_LEN(*frame);

		// magic number explanation
		// see maximum message length
		// https://fidoalliance.org/specs/fido-u2f-v1.0-ps-20141009/fido-u2f-hid-protocol-ps-20141009.html
		if (message->bcnt > MAX_BCNT) {
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "BCNT too large (%u). Bailing.\n", message->bcnt));
			HidErrorMessageSend(deviceContext->VhfHandle, message->cid, ERR_INVALID_LEN);
			MessageListRemove(deviceContext, message);
			return;
		}

		message->buf = MmAllocateNonCachedMemory(message->bcnt);
		RtlZeroMemory(message->buf, message->bcnt);
		message->bufCap = message->bcnt;
		message->bufLen = 0;

		data = frame->init.data;

		if (message->bcnt > sizeof(frame->init.data))
		{
			nData = sizeof(frame->init.data);
		}
		else
		{
			nData = message->bcnt;
		}

		break;

	case TYPE_CONT:
		if (!message)
		{
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CONT frame out of order. Ignoring\n"));
			return;
		}

		if (FRAME_SEQ(*frame) != message->lastSeq++)
		{
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Bad SEQ in CONT frame (%d). Bailing\n", FRAME_SEQ(*frame)));
			MessageListRemove(deviceContext, message);
			HidErrorMessageSend(deviceContext->VhfHandle, frame->cid, ERR_INVALID_SEQ);
			return;
		}

		data = frame->cont.data;

		if (message->bufLen + sizeof(frame->cont.data) > message->bcnt) {
			nData = message->bcnt - (UINT16)message->bufLen;
		}
		else {
			nData = sizeof(frame->cont.data);
		}

		break;


	default:
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Unknow frame type: 0x%08x\n", FRAME_TYPE(*frame)));
		return;
	}


	for (UINT32 i = 0; i < nData; i++)
	{
		message->buf[message->bufLen + i] = data[i];
	}
	message->bufLen += nData;

}

VOID
HidMessageHandle(
	_In_ PDEVICE_CONTEXT deviceContext
)
{
	PU2FHID_MESSAGE message;
	PU2FHID_MESSAGE nextMessage = deviceContext->MessageList;

	while (nextMessage)
	{
		message = nextMessage;
		nextMessage = message->next;

		if (HidMessageIsComplete(deviceContext, message))
		{
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Message Complete, responding\n"));
			HidMessageFinalize(deviceContext, message);
			switch (message->cmd)
			{
			case U2FHID_INIT:
				U2FHandleMessageInit(deviceContext, message);
				break;
			case U2FHID_PING:
				U2FHandleMessagePing(deviceContext, message);
				break;
			case U2FHID_WINK:
				U2FHandleMessageWink(deviceContext, message);
				break;
			case U2FHID_SYNC:
				U2FHandleMessageSync(deviceContext, message);
				break;
			// all the above msg can be processed within kernel mode
			// the below msg, we need to forward to user space
			case U2FHID_MSG:
				U2FHandleMessageMsg(deviceContext, message);
				break;
			default:
				HidErrorMessageSend(deviceContext->VhfHandle, message->cid, ERR_INVALID_CMD);
			}
			MessageListRemove(deviceContext, message);
		}
		else if (HidMessageIsTimeout(deviceContext, message)) {
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Message Timeout, sending ERR_MSG_TIMEOUT.\n"));
			HidErrorMessageSend(deviceContext->VhfHandle, message->cid, ERR_MSG_TIMEOUT);
			MessageListRemove(deviceContext, message);
		}
		else {
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Message %d didn't complete, wainting for cont.\n", message->cid));
		}
	}
}

#pragma region Message Handlers

VOID
U2FHandleMessageInit(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	NTSTATUS status;

	U2FHID_MESSAGE resp;
	U2FHID_INIT_RESP respData;
	PU2FHID_INIT_REQ reqData;

	RtlZeroMemory(&resp, sizeof(resp));
	reqData = (PU2FHID_INIT_REQ)(message->data);

	resp.cmd = U2FHID_INIT;
	resp.bcnt = sizeof(U2FHID_INIT_RESP);

	if (message->cid == CID_BROADCAST)
	{
		resp.cid = CID_BROADCAST;
		respData.cid = ++deviceContext->cid;
	}
	else
	{
		resp.cid = message->cid;
		respData.cid = message->cid;
	}

	RtlCopyMemory(respData.nonce, reqData->nonce, INIT_NONCE_SIZE);
	respData.versionInterface = U2FHID_IF_VERSION;
	respData.versionMajor = 0;
	respData.versionMinor = 0;
	respData.versionBuild = 0;
	respData.capFlags = (0x0 | CAPFLAG_WINK);

	resp.data = message->data;
	RtlZeroMemory(resp.data, sizeof(respData));
	RtlCopyMemory(resp.data, &respData, sizeof(respData));

	status = HidMessageSend(deviceContext->VhfHandle, &resp);

	if (!NT_SUCCESS(status))
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Failed to submit report\n"));
		return;
	}
}

VOID
U2FHandleMessagePing(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	U2FHID_MESSAGE resp;
	resp.cid = message->cid;
	resp.cmd = U2FHID_PING;
	resp.bcnt = message->bcnt;
	resp.data = message->data;

	HidMessageSend(deviceContext->VhfHandle, &resp);
}


VOID
U2FHandleMessageWink(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	U2FHID_MESSAGE resp;

	resp.cid = message->cid;
	resp.cmd = U2FHID_WINK;
	resp.bcnt = message->bcnt;
	resp.data = message->data;

	HidMessageSend(deviceContext->VhfHandle, &resp);
}

VOID
U2FHandleMessageSync(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	U2FHID_MESSAGE resp;

	resp.cid = message->cid;
	resp.cmd = U2FHID_SYNC;
	resp.bcnt = message->bcnt;
	resp.data = message->data;

	HidMessageSend(deviceContext->VhfHandle, message);
}

VOID
U2FHandleMessageMsg(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	NTSTATUS status;
	WDFREQUEST notifyRequest;
	ULONG_PTR bytesTransferred = 0;
	PIO_CTL_XFER_MESSAGE xferMessage;

	// grap a pending inverted call request, so we use this to notify User Space
	status = WdfIoQueueRetrieveNextRequest(deviceContext->ManualQueue, &notifyRequest);

	if (!NT_SUCCESS(status))
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "No pending req found\n"));
		return;
	}

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Message bcnt %d\n", message->bcnt));

	status = WdfRequestRetrieveOutputBuffer(
		notifyRequest,
		sizeof(IO_CTL_XFER_MESSAGE) + message->bcnt,
		(PVOID*)&xferMessage,
		NULL);
	if (!NT_SUCCESS(status))
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Cant retrive memory for request\n"));
		status = STATUS_MEMORY_NOT_ALLOCATED;
		goto FINISH;
	}

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Buffer retrived with len %d\n", sizeof(IO_CTL_XFER_MESSAGE) + message->bcnt));

	xferMessage->cmd = message->cmd;
	xferMessage->cid = message->cid;
	xferMessage->bcnt = message->bcnt;
	RtlCopyMemory((PUCHAR)xferMessage + sizeof(IO_CTL_XFER_MESSAGE), message->data, message->bcnt);

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Respons built, sending\n"));

	bytesTransferred = sizeof(IO_CTL_XFER_MESSAGE) + message->bcnt;

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Sending response %d\n", bytesTransferred));

FINISH:
	WdfRequestCompleteWithInformation(notifyRequest, status, bytesTransferred);
}

#pragma endregion


NTSTATUS
CreateRawQueue(
	_In_ WDFDEVICE	Device,
	_Out_ WDFQUEUE* Queue
)
{
	NTSTATUS                status;
	WDF_IO_QUEUE_CONFIG     queueConfig;
	WDF_OBJECT_ATTRIBUTES   queueAttributes;
	WDFQUEUE                queue;
	PRAW_QUEUE_CONTEXT          queueContext;

	WDF_IO_QUEUE_CONFIG_INIT(
		&queueConfig,
		WdfIoQueueDispatchParallel);

	queueConfig.EvtIoDeviceControl = EvtIoDeviceControlForMainPdo;

	WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(
		&queueAttributes,
		RAW_QUEUE_CONTEXT);

	status = WdfIoQueueCreate(
		Device,
		&queueConfig,
		&queueAttributes,
		&queue);

	if (!NT_SUCCESS(status)) {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "WdfIoQueueCreate failed 0x%X\n", status));
		return status;
	}

	queueContext = GetRawQueueContext(queue);
	queueContext->Queue = queue;
	queueContext->DeviceContext = GetDeviceContext(Device);

	*Queue = queue;
	return status;
}

NTSTATUS
CreateManualQueue(
	_In_  WDFDEVICE         Device,
	_Out_ WDFQUEUE*			Queue
)
{
	NTSTATUS                status;
	WDF_IO_QUEUE_CONFIG     queueConfig;
	WDF_OBJECT_ATTRIBUTES   queueAttributes;
	WDFQUEUE                queue;
	PMANUAL_QUEUE_CONTEXT   queueContext;

	WDF_IO_QUEUE_CONFIG_INIT(
		&queueConfig,
		WdfIoQueueDispatchManual
	);

	WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(
		&queueAttributes,
		MANUAL_QUEUE_CONTEXT
	);

	status = WdfIoQueueCreate(
		Device,
		&queueConfig,
		&queueAttributes,
		&queue
	);

	if (!NT_SUCCESS(status)) {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "ManualQueue failed 0x%x\n", status));
		return status;
	}

	queueContext = GetManualQueueContext(queue);
	queueContext->Queue = queue;
	queueContext->DeviceContext = GetDeviceContext(Device);

	*Queue = queue;

	return status;
}

NTSTATUS
CreateTimer(
	_In_ WDFDEVICE	Device,
	_Out_ WDFTIMER* Timer
)
{
	WDF_TIMER_CONFIG  timerConfig;
	WDF_OBJECT_ATTRIBUTES  timerAttributes;
	NTSTATUS  status;

	WDF_TIMER_CONFIG_INIT(
		&timerConfig,
		TimeoutMessagesCleanup
	);

	timerConfig.AutomaticSerialization = TRUE;
	timerConfig.Period = 200;  // 200 ms
	timerConfig.UseHighResolutionTimer = WdfTrue;

	WDF_OBJECT_ATTRIBUTES_INIT(&timerAttributes);
	timerAttributes.ParentObject = Device;

	status = WdfTimerCreate(
		&timerConfig,
		&timerAttributes,
		Timer
	);

	if (!NT_SUCCESS(status))
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Create timer failed"));
		return status;
	}

	WdfTimerStart(*Timer, WDF_REL_TIMEOUT_IN_MS(100));
	return status;
}

NTSTATUS
CreateRawPdo(
	_In_  WDFDEVICE         Device
)
{
	NTSTATUS                    status;
	PWDFDEVICE_INIT             pDeviceInit = NULL;
	PRAWPDO_DEVICE_CONTEXT      pdoData = NULL;
	WDFDEVICE                   hChild = NULL;
	WDF_OBJECT_ATTRIBUTES       pdoAttributes;
	WDF_DEVICE_PNP_CAPABILITIES pnpCaps;
	WDF_IO_QUEUE_CONFIG         ioQueueConfig;
	WDFQUEUE                    queue;
	WDF_DEVICE_STATE            deviceState;
	PDEVICE_CONTEXT             deviceContext;
	WDF_PNPPOWER_EVENT_CALLBACKS  pnpPowerCallbacks;

	DECLARE_CONST_UNICODE_STRING(deviceId, SoftU2F_DEVICE_ID);
	DECLARE_CONST_UNICODE_STRING(deviceLocation, L"SoftU2F Communicator\0");
	DECLARE_CONST_UNICODE_STRING(SDDL_MY_PERMISSIONS, L"D:P(A;; GA;;; SY)(A;; GA;;; BA)(A;; GA;;; WD)");
	DECLARE_UNICODE_STRING_SIZE(buffer, MAX_ID_LEN);

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Creating RawPdo\n"));

	pDeviceInit = WdfPdoInitAllocate(Device);

	if (pDeviceInit == NULL) {
		status = STATUS_INSUFFICIENT_RESOURCES;
		goto Cleanup;
	}

	WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&pnpPowerCallbacks);
	pnpPowerCallbacks.EvtDeviceSelfManagedIoInit = RAWPDO_EvtDeviceSelfManagedIoInit;
	WdfDeviceInitSetPnpPowerEventCallbacks(
		pDeviceInit,
		&pnpPowerCallbacks
	);

	//
	// Mark the device RAW so that the child device can be started
	// and accessed without requiring a function driver. Since we are
	// creating a RAW PDO, we must provide a class guid.
	//
	status = WdfPdoInitAssignRawDevice(pDeviceInit, &GUID_DEVCLASS_SoftU2F);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}


	//
	// Since keyboard is secure device, we must protect ourselves from random
	// users sending ioctls and creating trouble.
	//
	status = WdfDeviceInitAssignSDDLString(pDeviceInit,
		&SDDL_MY_PERMISSIONS);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}

	//
	// Assign DeviceID - This will be reported to IRP_MN_QUERY_ID/BusQueryDeviceID
	//
	status = WdfPdoInitAssignDeviceID(pDeviceInit, &deviceId);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}

	//
	// We could be enumerating more than one children if the filter attaches
	// to multiple instances of keyboard, so we must provide a
	// BusQueryInstanceID. If we don't, system will throw CA bug check.
	//
	status = RtlUnicodeStringPrintf(&buffer, L"%02d", InstanceNo);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}

	status = WdfPdoInitAssignInstanceID(pDeviceInit, &buffer);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}


	//
	// Provide a description about the device. This text is usually read from
	// the device. In the case of USB device, this text comes from the string
	// descriptor. This text is displayed momentarily by the PnP manager while
	// it's looking for a matching INF. If it finds one, it uses the Device
	// Description from the INF file to display in the device manager.
	// Since our device is raw device and we don't provide any hardware ID
	// to match with an INF, this text will be displayed in the device manager.
	//
	status = RtlUnicodeStringPrintf(&buffer, L"SoftU2F_Filter_Pdo_%02d", InstanceNo);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}
	InstanceNo++;

	//
	// You can call WdfPdoInitAddDeviceText multiple times, adding device
	// text for multiple locales. When the system displays the text, it
	// chooses the text that matches the current locale, if available.
	// Otherwise it will use the string for the default locale.
	// The driver can specify the driver's default locale by calling
	// WdfPdoInitSetDefaultLocale.
	//
	status = WdfPdoInitAddDeviceText(pDeviceInit,
		&buffer,
		&deviceLocation,
		0x409
	);

	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}

	WdfPdoInitSetDefaultLocale(pDeviceInit, 0x409);

	//
   // Initialize the attributes to specify the size of PDO device extension.
   // All the state information private to the PDO will be tracked here.
   //
	WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&pdoAttributes, RAWPDO_DEVICE_CONTEXT);

	//
	// Set up our queue to allow forwarding of requests to the parent
	// This is done so that the cached Keyboard Attributes can be retrieved
	//
	WdfPdoInitAllowForwardingRequestToParent(pDeviceInit);

	status = WdfDeviceCreate(&pDeviceInit, &pdoAttributes, &hChild);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}

	//
	// Get the device context.
	//
	pdoData = GetRawPdoDeviceContext(hChild);

	pdoData->InstanceNo = InstanceNo;


	//
	// Get the parent queue we will be forwarding to
	//
	deviceContext = GetDeviceContext(Device);
	pdoData->ParentQueue = deviceContext->RawQueue;

	//
	// Configure the default queue associated with the control device object
	// to be Serial so that request passed to EvtIoDeviceControl are serialized.
	// A default queue gets all the requests that are not
	// configure-fowarded using WdfDeviceConfigureRequestDispatching.
	//
	WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&ioQueueConfig,
		WdfIoQueueDispatchSequential);

	ioQueueConfig.EvtIoDeviceControl = EvtIoDeviceControlForRawPdo;


	status = WdfIoQueueCreate(hChild,
		&ioQueueConfig,
		WDF_NO_OBJECT_ATTRIBUTES,
		&queue // pointer to default queue
	);
	if (!NT_SUCCESS(status)) {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "WdfIoQueueCreate failed 0x%x\n", status));
		goto Cleanup;
	}

	//
	// Set some properties for the child device.
	//
	WDF_DEVICE_PNP_CAPABILITIES_INIT(&pnpCaps);

	pnpCaps.Removable = WdfTrue;
	pnpCaps.SurpriseRemovalOK = WdfTrue;
	pnpCaps.NoDisplayInUI = WdfTrue;

	pnpCaps.Address = InstanceNo;
	pnpCaps.UINumber = InstanceNo;

	WdfDeviceSetPnpCapabilities(hChild, &pnpCaps);

	//
	// TODO: In addition to setting NoDisplayInUI in DeviceCaps, we
	// have to do the following to hide the device. Following call
	// tells the framework to report the device state in
	// IRP_MN_QUERY_DEVICE_STATE request.
	//
	WDF_DEVICE_STATE_INIT(&deviceState);
	deviceState.DontDisplayInUI = WdfTrue;
	WdfDeviceSetDeviceState(hChild, &deviceState);

	//
	// Tell the Framework that this device will need an interface so that
	// application can find our device and talk to it.
	//
	status = WdfDeviceCreateDeviceInterface(
		hChild,
		&GUID_DEVINTERFACE_SOFTU2F_FILTER,
		NULL
	);

	if (!NT_SUCCESS(status)) {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "WdfDeviceCreateDeviceInterface failed 0x%x\n", status));
		goto Cleanup;
	}

	//
   // Add this device to the FDO's collection of children.
   // After the child device is added to the static collection successfully,
   // driver must call WdfPdoMarkMissing to get the device deleted. It
   // shouldn't delete the child device directly by calling WdfObjectDelete.
   //
	status = WdfFdoAddStaticChild(Device, hChild);
	if (!NT_SUCCESS(status)) {
		goto Cleanup;
	}
	deviceContext->RawPdo = hChild;


	//
	// pDeviceInit will be freed by WDF.
	//
	return STATUS_SUCCESS;
Cleanup:

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CreateRawPdo failed %x\n", status));

	//
	// Call WdfDeviceInitFree if you encounter an error while initializing
	// a new framework device object. If you call WdfDeviceInitFree,
	// do not call WdfDeviceCreate.
	//
	if (pDeviceInit != NULL) {
		WdfDeviceInitFree(pDeviceInit);
	}

	if (hChild) {
		WdfObjectDelete(hChild);
	}

	return status;
}

NTSTATUS
HidErrorMessageSend(
	_In_
	VHFHANDLE           VhfHandle,
	_In_
	UINT32			    cid,
	UINT8				code
)
{
	U2FHID_MESSAGE message = { 0 };
	UINT8 data[1];
	data[0] = code;
	RtlZeroMemory(&message, sizeof(U2FHID_MESSAGE));
	message.cmd = U2FHID_ERROR;
	message.cid = cid;
	message.bcnt = 1;
	message.data = (PUCHAR)& data[0];

	return HidMessageSend(VhfHandle, &message);

}

NTSTATUS
HidMessageSend(
	_In_ VHFHANDLE VhfHandle,
	_In_ PU2FHID_MESSAGE message
)
{
	NTSTATUS status;
	PUINT8 src;
	PUINT8 srcEnd;
	PUINT8 dst;
	PUINT8 dstEnd;
	UINT8 seq = 0x00;
	U2FHID_FRAME frame;
	HID_XFER_PACKET HidXferPacket;
	LARGE_INTEGER delayInterval;

	RtlZeroMemory(&frame, HID_RPT_SIZE);

	frame.cid = message->cid;
	frame.type |= TYPE_INIT;
	frame.init.cmd |= message->cmd;
	frame.init.bcnth = message->bcnt >> 8;
	frame.init.bcntl = message->bcnt & 0xff;

	src = message->data;
	srcEnd = src + message->bcnt;
	dst = frame.init.data;
	dstEnd = dst + sizeof(frame.init.data);

	while (1) {
		if (srcEnd - src > dstEnd - dst) {
			RtlCopyMemory(dst, src, dstEnd - dst);
			src += dstEnd - dst;
		}
		else {
			RtlCopyMemory(dst, src, srcEnd - src);
			src += srcEnd - src;
		}

		HidXferPacket.reportBuffer = (PUCHAR)& frame;
		HidXferPacket.reportBufferLen = sizeof(frame);
		HidXferPacket.reportId = 0;

		status = VhfReadReportSubmit(VhfHandle, &HidXferPacket);

		if (!NT_SUCCESS(status))
		{
			return status;
		}

		if (src >= srcEnd) {
			break;
		}

		delayInterval.QuadPart = 1 * RELATIVE_MILLISECOND;
		KeDelayExecutionThread(KernelMode, FALSE, &delayInterval);

		dst = frame.cont.data;
		dstEnd = dst + sizeof(frame.cont.data);
		frame.cont.seq = seq++;
		RtlZeroMemory(frame.cont.data, sizeof(frame.cont.data));
	}

	return status;
}

#pragma region Message List Operations

PU2FHID_MESSAGE
MessageListFind(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ UINT32 cid
)
{
	PU2FHID_MESSAGE msg = deviceContext->MessageList;

	while (msg)
	{
		if (msg->cid == cid) {
			break;
		}

		msg = msg->next;
	}

	return msg;
}

VOID
MessageListRemove(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	PU2FHID_MESSAGE previous;

	if (message == deviceContext->MessageList) {
		deviceContext->MessageList = message->next;
		MessageFree(message);
		return;
	}

	previous = deviceContext->MessageList;
	while (previous && previous->next != message) {
		previous = previous->next;
	}

	if (!previous) {
		return;
	}

	previous->next = message->next;
	MessageFree(message);
}

PU2FHID_MESSAGE
MessageListCreate(
	_In_ PDEVICE_CONTEXT deviceContext
)
{
	PU2FHID_MESSAGE message;
	PU2FHID_MESSAGE lastMessage = NULL;

	message = MessageAlloc(deviceContext);
	if (!message)
	{
		return NULL;
	}

	if (!deviceContext->MessageList) {
		deviceContext->MessageList = message;
		return message;
	}

	lastMessage = deviceContext->MessageList;
	while (lastMessage->next) {
		lastMessage = lastMessage->next;
	}
	lastMessage->next = message;
	return message;
}

PU2FHID_MESSAGE
MessageAlloc
(
	_In_ PDEVICE_CONTEXT deviceContext
)
{
	PU2FHID_MESSAGE message;

	message = (PU2FHID_MESSAGE)MmAllocateNonCachedMemory(sizeof(U2FHID_MESSAGE));
	if (!message)
	{
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "No memory for new message.\n"));
		return NULL;
	}

	RtlZeroMemory(message, sizeof(U2FHID_MESSAGE));
	KeQueryTickCount(&message->createdAtTicks);
	return message;
}

UINT32
MessageListCount(
	_In_ PDEVICE_CONTEXT deviceContext
)
{
	PU2FHID_MESSAGE msg = deviceContext->MessageList;
	UINT32 count = 0;

	while (msg) {
		count++;
		msg = msg->next;
	}

	return count;
}

VOID
MessageFree(
	_In_ PU2FHID_MESSAGE message
)
{
	if (message) {
		if (message->data)
		{
			MmFreeNonCachedMemory(message->data, sizeof(message->bcnt));
		}
		if (message->buf)
		{
			MmFreeNonCachedMemory(message->buf, sizeof(message->bufCap));
		}
		MmFreeNonCachedMemory(message, sizeof(U2FHID_MESSAGE));
	}
}

BOOLEAN
HidMessageIsComplete(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	if (message)
	{
		return message->bufLen == message->bcnt;
	}
	return FALSE;
}


BOOLEAN
HidMessageIsTimeout(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	// TODO: refactor please
	LARGE_INTEGER currentTimestampTicks;
	ULONG tickIncrement, nsToTimeout;
	LONGLONG ticksDelta, nsElapsed;

	tickIncrement = KeQueryTimeIncrement();
	nsToTimeout = 0.5 * 1000000000L;  // 0.5s

	KeQueryTickCount(&currentTimestampTicks);

	ticksDelta = currentTimestampTicks.QuadPart - message->createdAtTicks.QuadPart;

	nsElapsed = ticksDelta * tickIncrement * 100;

	return nsElapsed > nsToTimeout;
}

VOID
HidMessageFinalize(
	_In_ PDEVICE_CONTEXT deviceContext,
	_In_ PU2FHID_MESSAGE message
)
{
	message->data = MmAllocateNonCachedMemory(message->bufCap);
	RtlZeroMemory(message->data, message->bufCap);

	RtlCopyMemory(message->data, message->buf, message->bufCap);
	message->bcnt = message->bufCap;

	if (message->buf) {
		// in the case that WINK is sending zero length data
		// so buf is 0/NULL
		MmFreeNonCachedMemory(message->buf, message->bufCap);
		message->buf = NULL;
		message->bufCap = 0;
	}
}

VOID
TimeoutMessagesCleanup(
	WDFTIMER Timer
)
{
	WDFDEVICE device;
	PDEVICE_CONTEXT deviceContext;
	KIRQL OldIrql;

	device = (WDFDEVICE)WdfTimerGetParentObject(Timer);
	deviceContext = GetDeviceContext(device);

	KeAcquireSpinLock(&deviceContext->MessageProcessLock, &OldIrql);

	HidMessageHandle(deviceContext);

	KeReleaseSpinLock(&deviceContext->MessageProcessLock, OldIrql);
}

VOID
EvtIoDeviceControlForRawPdo(
	_In_
	WDFQUEUE Queue,
	_In_
	WDFREQUEST Request,
	_In_
	size_t OutputBufferLength,
	_In_
	size_t InputBufferLength,
	_In_
	ULONG IoControlCode
)
{
	NTSTATUS status = STATUS_SUCCESS;
	WDFDEVICE parent = WdfIoQueueGetDevice(Queue);
	PRAWPDO_DEVICE_CONTEXT pdoContext;
	WDF_REQUEST_FORWARD_OPTIONS forwardOptions;
	UNREFERENCED_PARAMETER(OutputBufferLength);
	UNREFERENCED_PARAMETER(InputBufferLength);

	pdoContext = GetRawPdoDeviceContext(parent);
	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "RawPdo got DeviceControl, ControlCode(%lu)\n", IoControlCode));

	switch (IoControlCode)
	{
	case  IOCTL_SOFTU2F_FILTER_INIT:
	case IOCTL_SOFTU2F_FILTER_WRITE_DATA:
		WDF_REQUEST_FORWARD_OPTIONS_INIT(&forwardOptions);
		status = WdfRequestForwardToParentDeviceIoQueue(Request, pdoContext->ParentQueue, &forwardOptions);
		if (!NT_SUCCESS(status))
		{
			WdfRequestComplete(Request, STATUS_DEVICE_BUSY);
		}
		break;
	default:
		WdfRequestComplete(Request, STATUS_NOT_IMPLEMENTED);
	}
}

VOID
EvtIoDeviceControlForMainPdo(
	_In_
	WDFQUEUE Queue,
	_In_
	WDFREQUEST Request,
	_In_
	size_t OutputBufferLength,
	_In_
	size_t InputBufferLength,
	_In_
	ULONG IoControlCode
)
{
	NTSTATUS status = STATUS_SUCCESS;
	WDFDEVICE device;
	PDEVICE_CONTEXT deviceContext;
	ULONG_PTR nData = 0;
	PVOID requestInputBuffer;
	PIO_CTL_XFER_MESSAGE xferMessage;
	U2FHID_MESSAGE response;

	const size_t IO_CTL_XFER_MESSAGE_SIZE = MAX_BCNT + sizeof(IO_CTL_XFER_MESSAGE);

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Entered EvtIoDeviceControlForMainPdo, IoControlCode(%lu), InputBufferLength %d, OutputBufferLength %d\n", IoControlCode, InputBufferLength, OutputBufferLength));

	device = WdfIoQueueGetDevice(Queue);
	deviceContext = GetDeviceContext(device);

	switch (IoControlCode)
	{
	case IOCTL_SOFTU2F_FILTER_INIT:
		if (OutputBufferLength < IO_CTL_XFER_MESSAGE_SIZE)
		{
			status = STATUS_BUFFER_TOO_SMALL;
			nData = IO_CTL_XFER_MESSAGE_SIZE;
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "OutputBuffer too small, required: %lu\n", nData));
			break;
		}

		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "BufferLen check passed %d, forwarding to manual queue\n", OutputBufferLength));
		
		// supply the req to ManualQueue, so when a message received from kernel, the inverted call can use this request handle to notify user space.
		status = WdfRequestForwardToIoQueue(Request, deviceContext->ManualQueue);
		if (!NT_SUCCESS(status))
		{
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Failed to forward to manual queue\n"));
			break;
		}
		
		// to see if User Space replied with anything
		if (InputBufferLength >= sizeof(IO_CTL_XFER_MESSAGE) && InputBufferLength <= IO_CTL_XFER_MESSAGE_SIZE)
		{
			status = WdfRequestRetrieveInputBuffer(Request, InputBufferLength, &requestInputBuffer, NULL);
			if (!NT_SUCCESS(status))
			{
				KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Failed to retrive inputBuffer, NT_STATUS: %d, InputBufferLength: %d\n", status, InputBufferLength));
				return;
			}

			xferMessage = (PIO_CTL_XFER_MESSAGE)requestInputBuffer;
			
			if (xferMessage->bcnt > InputBufferLength - sizeof(IO_CTL_XFER_MESSAGE)) {
				KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "message bcnt is out of bounds: %d\n", xferMessage->bcnt));
				return;
			}
			
			response.cmd = xferMessage->cmd;
			response.cid = xferMessage->cid;
			response.bcnt = xferMessage->bcnt;
			response.data = (PUCHAR)xferMessage + sizeof(IO_CTL_XFER_MESSAGE);

			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Response message built, sending cmd: %d, cid: %d, bcnt: %d\n", response.cmd, response.cid, response.bcnt));

			// if we got a response from User Space, submit it to HID VHF;
			status = HidMessageSend(deviceContext->VhfHandle, &response);
			if (!NT_SUCCESS(status))
			{
				KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Response message send failed\n"));
			}
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Response message sent\n"));

		}
		else 
		{
			KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Invalid InputBufferLength %d \n", InputBufferLength));
		}

		return;

	default:
		status = STATUS_NOT_IMPLEMENTED;
		break;
	}

	WdfRequestCompleteWithInformation(Request, status, nData);
}
#pragma endregion
