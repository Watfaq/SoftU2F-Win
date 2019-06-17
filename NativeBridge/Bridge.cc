#include "Bridge.h"

#include <stdio.h>
#include <stdlib.h>
#include <setupapi.h>
#include <wtypes.h>

EXTERN_C_START

PCHAR GetInterfaceDevicePath()
{

	HDEVINFO                            hardwareDeviceInfo;
	SP_DEVICE_INTERFACE_DATA            deviceInterfaceData;
	PSP_DEVICE_INTERFACE_DETAIL_DATA    deviceInterfaceDetailData = NULL;
	ULONG                               predictedLength = 0;
	ULONG                               requiredLength = 0, bytes = 0;
	ULONG                               i = 0;

	hardwareDeviceInfo = SetupDiGetClassDevs(
		(LPGUID)& GUID_DEVINTERFACE_SOFTU2F_FILTER,
		NULL,
		NULL,
		(DIGCF_PRESENT | DIGCF_DEVICEINTERFACE)
	);

	if(hardwareDeviceInfo == INVALID_HANDLE_VALUE)
	{
		printf("SetupDiGetClassDevs failed: %x\n", GetLastError());
		return nullptr;
	}

	deviceInterfaceData.cbSize = sizeof(SP_DEVICE_INTERFACE_DATA);
	printf("\nList of SoftU2F Filter Device Interfaces\n");
	printf("---------------------------------\n");

	do
	{
		if (SetupDiEnumDeviceInterfaces(
			hardwareDeviceInfo,
			0,
			(LPGUID)&GUID_DEVINTERFACE_SOFTU2F_FILTER,i,&deviceInterfaceData))
		{
			if(deviceInterfaceDetailData)
			{
				free(deviceInterfaceDetailData);
				deviceInterfaceDetailData = NULL;
			}

			if (!SetupDiGetDeviceInterfaceDetail(
				hardwareDeviceInfo,
				&deviceInterfaceData,
				NULL,
				0,
				&requiredLength,NULL))
			{
				if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
				{
					printf("SetupDiGetDeviceInterfaceDetail failed %d\n", GetLastError());
					SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
					return nullptr;
				}
			}

			predictedLength = requiredLength;

			deviceInterfaceDetailData = malloc(predictedLength);

			if (deviceInterfaceDetailData)
			{
				deviceInterfaceDetailData->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA);
			} else
			{
				printf("Couldn't allocate %d bytes for device interface details.\n", predictedLength);
				SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
				return nullptr;
			}

			if (!SetupDiGetDeviceInterfaceDetail(
				hardwareDeviceInfo,
				&deviceInterfaceData,
				deviceInterfaceDetailData,
				predictedLength,
				&requiredLength,
				NULL
			))
			{
				printf("Error in SetupDiGetDeviceInterfaceDetail\n");
				SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
				free(deviceInterfaceDetailData);
				return nullptr;
			}
			printf("%d) %s\n", ++i,
				deviceInterfaceDetailData->DevicePath);
		}
		else if (GetLastError() != ERROR_NO_MORE_ITEMS)
		{
			free(deviceInterfaceDetailData);
			deviceInterfaceDetailData = NULL;
		}
		else break;;
	} while (TRUE);

	SetupDiDestroyDeviceInfoList(hardwareDeviceInfo);
	if (!deviceInterfaceDetailData)
	{
		printf("No device interfaces present\n");
		return nullptr;
	}

	return (PWCHAR)&deviceInterfaceDetailData->DevicePath[0];
}

EXTERN_C_END