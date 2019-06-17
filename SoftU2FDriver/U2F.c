#include "U2F.h"

void softu2f_debug_frame(U2FHID_FRAME *frame, BOOLEAN recv) {
	UINT8 *data = NULL;
	UINT16 dlen = 0;

	if (recv) {
		KdPrint(("Received frame:\n"));
	}
	else {
		KdPrint(("Sending frame:\n"));
	}

	KdPrint(("\tCID: 0x%08x\n", frame->cid));

	switch (FRAME_TYPE(*frame)) {
	case TYPE_INIT:
		KdPrint(( "\tTYPE: INIT\n"));
		KdPrint(("\tCMD: 0x%02x\n", frame->init.cmd & ~TYPE_MASK));
		KdPrint(("\tBCNTH: 0x%02x\n", frame->init.bcnth));
		KdPrint(("\tBCNTL: 0x%02x\n", frame->init.bcntl));
		data = frame->init.data;
		dlen = HID_RPT_SIZE - 7;

		break;

	case TYPE_CONT:
		KdPrint(("\tTYPE: CONT\n"));
		KdPrint(("\tSEQ: 0x%02x\n", frame->cont.seq));
		data = frame->cont.data;
		dlen = HID_RPT_SIZE - 5;

		break;
	}

	KdPrint(("\tDATA:"));
	for (int i = 0; i < dlen; i++) {
		KdPrint((" %02x", data[i]));
	}

	KdPrint(("\n\n"));
}
