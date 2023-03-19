#include "U2F.h"

void softu2f_debug_frame(U2FHID_FRAME *frame, BOOLEAN recv) {
	UINT8 *data = NULL;
	UINT16 dlen = 0;

	if (recv) {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Received frame:\n"));
	}
	else {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "Sending frame:\n"));
	}

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tCID: 0x%08x\n", frame->cid));

	switch (FRAME_TYPE(*frame)) {
	case TYPE_INIT:
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tTYPE: INIT\n"));
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tCMD: 0x%02x\n", frame->init.cmd & ~TYPE_MASK));
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tBCNTH: 0x%02x\n", frame->init.bcnth));
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tBCNTL: 0x%02x\n", frame->init.bcntl));
		data = frame->init.data;
		dlen = HID_RPT_SIZE - 7;

		break;

	case TYPE_CONT:
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tTYPE: CONT\n"));
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tSEQ: 0x%02x\n", frame->cont.seq));
		data = frame->cont.data;
		dlen = HID_RPT_SIZE - 5;

		break;
	}

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\tDATA:"));
	for (int i = 0; i < dlen; i++) {
		KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, " %02x", data[i]));
	}

	KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "\n\n"));
}
