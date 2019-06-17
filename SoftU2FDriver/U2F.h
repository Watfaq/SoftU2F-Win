#pragma once
#include <ntddk.h>

#pragma warning(disable:4201) // nameless struct/union

EXTERN_C_START

#define TYPE_MASK 0x80 // Frame type mask

#define TYPE_INIT 0x80	// Initial frame identifier
#define TYPE_CONT 0x00 // Continuation frame identifier

#define HID_RPT_SIZE 64 // Default size of raw HID report

#define CID_BROADCAST           0xffffffff // Broadcast channel id
#define U2FHID_IF_VERSION 2	


#define U2FHID_PING         (TYPE_INIT | 0x01)	// Echo data through local processor only
#define U2FHID_MSG          (TYPE_INIT | 0x03)	// Send U2F message frame
#define U2FHID_LOCK         (TYPE_INIT | 0x04)	// Send lock channel command
#define U2FHID_INIT         (TYPE_INIT | 0x06)	// Channel initialization
#define U2FHID_WINK         (TYPE_INIT | 0x08)	// Send device identification wink
#define U2FHID_SYNC         (TYPE_INIT | 0x3c)  // Protocol resync command
#define U2FHID_ERROR        (TYPE_INIT | 0x3f)	// Error response

#define INIT_NONCE_SIZE 8 // Size of channel initialization challenge
#define CAPFLAG_WINK            0x01    // Device supports WINK command
#define CAPFLAG_LOCK            0x02    // Device supports LOCK command

#define FRAME_TYPE(f) ((f).type & TYPE_MASK)
#define MESSAGE_LEN(f) ((f).init.bcnth*256 + (f).init.bcntl)
#define FRAME_SEQ(f)  ((f).cont.seq & ~TYPE_MASK)


#pragma region data structures
#pragma pack(push, 1)

typedef struct _U2FHID_FRAME {
	UINT32 cid;
	union {
		UINT8 type;
		struct {
			UINT8 cmd;
			UINT8 bcnth;
			UINT8 bcntl;
			UINT8 data[64 - 7];
		} init;
		struct {
			UINT8 seq;
			UINT8 data[64 - 5];
		} cont;
	};
} U2FHID_FRAME, *PU2FHID_FRAME;


typedef struct _U2FHID_INIT_REQ {
	UINT8 nonce[INIT_NONCE_SIZE];
}U2FHID_INIT_REQ, *PU2FHID_INIT_REQ;

typedef struct _U2FHID_INIT_RESP {
	UINT8 nonce[INIT_NONCE_SIZE];       // Client application nonce
	UINT32 cid;                         // Channel identifier
	UINT8 versionInterface;             // Interface version
	UINT8 versionMajor;                 // Major version number
	UINT8 versionMinor;                 // Minor version number
	UINT8 versionBuild;                 // Build version number
	UINT8 capFlags;                     // Capabilities flags
} U2FHID_INIT_RESP, *PU2FHID_INIT_RESP;

typedef struct _U2F_HID_MESSAGE {
	UINT8	cmd;
	UINT32	cid;
	UINT16	bcnt;
	PUCHAR	data;

	PUCHAR	buf;
	UINT16	bufCap;  // store the capacity of buf, for memory free
	ULONG	bufLen;  // track the actual buf length

	UINT8	lastSeq;

	struct _U2F_HID_MESSAGE *next;

	LARGE_INTEGER createdAtTicks;
} U2FHID_MESSAGE, *PU2FHID_MESSAGE;


typedef struct _IO_CTL_XFER_MESSAGE {
	UINT8	cmd;
	UINT32	cid;
	UINT16	bcnt;
} IO_CTL_XFER_MESSAGE, * PIO_CTL_XFER_MESSAGE;

#pragma pack(pop)
#pragma endregion

#define ERR_NONE                0x00    // No error
#define ERR_INVALID_CMD         0x01    // Invalid command
#define ERR_INVALID_PAR         0x02    // Invalid parameter
#define ERR_INVALID_LEN         0x03    // Invalid message length
#define ERR_INVALID_SEQ         0x04    // Invalid message sequencing
#define ERR_MSG_TIMEOUT         0x05    // Message has timed out
#define ERR_CHANNEL_BUSY        0x06    // Channel busy
#define ERR_LOCK_REQUIRED       0x0a    // Command requires channel lock
#define ERR_INVALID_CID         0x0b    // Message on CID 0
#define ERR_OTHER               0x7f    // Other unspecified error

void softu2f_debug_frame(U2FHID_FRAME* frame, BOOLEAN recv);


EXTERN_C_END