//
// RemoveDriveByLetter.cpp by Uwe Sieber - www.uwe-sieber.de
http://www.codeproject.com/KB/system/RemoveDriveByLetter.aspx

#include "stdafx.h"
#include <stdio.h>

#include <windows.h>

#include <Setupapi.h>
#include <winioctl.h>
#include <winioctl.h>
#include <cfgmgr32.h>
#include "USBRemover.h"

namespace MyDLPEP
{

DEVINST GetDrivesDevInstByDeviceNumber(long DeviceNumber, UINT DriveType, char* szDosDeviceName);

int USBRemover::remove(char drive_letter)
{
	char DriveLetter = drive_letter;
	DriveLetter &= ~0x20; // uppercase

	printf("%c\n", drive_letter);
	if ( DriveLetter < 'A' || DriveLetter > 'Z' ) {
		return 1;
	}

	char szRootPath[] = "X:\\";   // "X:\"  -> for GetDriveType
	szRootPath[0] = DriveLetter;

	char szDevicePath[] = "X:";   // "X:"   -> for QueryDosDevice
	szDevicePath[0] = DriveLetter;

	char szVolumeAccessPath[] = "\\\\.\\X:";   // "\\.\X:"  -> to open the volume
	szVolumeAccessPath[4] = DriveLetter;

	long DeviceNumber = -1;

	// open the storage volume
	HANDLE hVolume = CreateFileA(szVolumeAccessPath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, NULL, NULL);
	if (hVolume == INVALID_HANDLE_VALUE) {
		return 1;
	}

	// get the volume's device number
	STORAGE_DEVICE_NUMBER sdn;
	DWORD dwBytesReturned = 0;
	long res = DeviceIoControl(hVolume, IOCTL_STORAGE_GET_DEVICE_NUMBER, NULL, 0, &sdn, sizeof(sdn), &dwBytesReturned, NULL);
	if ( res ) {
		DeviceNumber = sdn.DeviceNumber;
	}
	CloseHandle(hVolume);

	if ( DeviceNumber == -1 ) {
		return 1;
	}

	// get the drive type which is required to match the device numbers correctely
	UINT DriveType = GetDriveTypeA(szRootPath);

	// get the dos device name (like \device\floppy0) to decide if it's a floppy or not - who knows a better way?
	char szDosDeviceName[MAX_PATH];
	res = QueryDosDeviceA(szDevicePath, szDosDeviceName, MAX_PATH);
	if ( !res ) {
		return 1;
	}

	// get the device instance handle of the storage volume by means of a SetupDi enum and matching the device number
	DEVINST DevInst = GetDrivesDevInstByDeviceNumber(DeviceNumber, DriveType, szDosDeviceName);

	if ( DevInst == 0 ) {
		return 1;
	}

	PNP_VETO_TYPE VetoType = PNP_VetoTypeUnknown; 
	WCHAR VetoNameW[MAX_PATH];
	VetoNameW[0] = 0;
	bool bSuccess = false;

	// get drives's parent, e.g. the USB bridge, the SATA port, an IDE channel with two drives!
	DEVINST DevInstParent = 0;
	res = CM_Get_Parent(&DevInstParent, DevInst, 0); 

	for ( long tries=1; tries<=3; tries++ ) { // sometimes we need some tries...

		VetoNameW[0] = 0;

		// CM_Query_And_Remove_SubTree doesn't work for restricted users
		//res = CM_Query_And_Remove_SubTreeW(DevInstParent, &VetoType, VetoNameW, MAX_PATH, CM_REMOVE_NO_RESTART); // CM_Query_And_Remove_SubTreeA is not implemented under W2K!
		//res = CM_Query_And_Remove_SubTreeW(DevInstParent, NULL, NULL, 0, CM_REMOVE_NO_RESTART);  // with messagebox (W2K, Vista) or balloon (XP)
		
		res = CM_Request_Device_EjectW(DevInstParent, &VetoType, VetoNameW, MAX_PATH, 0);
		//res = CM_Request_Device_EjectW(DevInstParent, NULL, NULL, 0, 0); // with messagebox (W2K, Vista) or balloon (XP)

		bSuccess = (res==CR_SUCCESS && VetoType==PNP_VetoTypeUnknown);
		if ( bSuccess )  { 
			break;
		}

		Sleep(500); // required to give the next tries a chance!
	}

	if ( bSuccess ) {
		printf("Success\n\n");
		return 0;
	}

	printf("failed\n");
	
	printf("Result=0x%2X\n", res);

	if ( VetoNameW[0] ) {
		printf("VetoName=%ws)\n\n", VetoNameW);
	}	
	return 1;
}
//-----------------------------------------------------------



//----------------------------------------------------------------------
// returns the device instance handle of a storage volume or 0 on error
//----------------------------------------------------------------------
DEVINST GetDrivesDevInstByDeviceNumber(long DeviceNumber, UINT DriveType, char* szDosDeviceName)
{
	bool IsFloppy = (strstr(szDosDeviceName, "\\Floppy") != NULL); // who knows a better way?

	GUID* guid;

	switch (DriveType) {
	case DRIVE_REMOVABLE:
		if ( IsFloppy ) {
			guid = (GUID*)&GUID_DEVINTERFACE_FLOPPY;
		} else {
			guid = (GUID*)&GUID_DEVINTERFACE_DISK;
		}
		break;
	case DRIVE_FIXED:
		guid = (GUID*)&GUID_DEVINTERFACE_DISK;
		break;
	case DRIVE_CDROM:
		guid = (GUID*)&GUID_DEVINTERFACE_CDROM;
		break;
	default:
		return 0;
	}

	// Get device interface info set handle for all devices attached to system
	HDEVINFO hDevInfo = SetupDiGetClassDevs(guid, NULL, NULL, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

	if (hDevInfo == INVALID_HANDLE_VALUE)	{
		return 0;
	}

	// Retrieve a context structure for a device interface of a device information set
	DWORD dwIndex = 0;
	long res;

	BYTE Buf[1024];
	PSP_DEVICE_INTERFACE_DETAIL_DATA pspdidd = (PSP_DEVICE_INTERFACE_DETAIL_DATA)Buf;
	SP_DEVICE_INTERFACE_DATA         spdid;
	SP_DEVINFO_DATA                  spdd;
	DWORD                            dwSize;
	
	spdid.cbSize = sizeof(spdid);

	while ( true )	{
		res = SetupDiEnumDeviceInterfaces(hDevInfo, NULL, guid, dwIndex, &spdid);
		if ( !res ) {
			break;
		}

		dwSize = 0;
		SetupDiGetDeviceInterfaceDetail(hDevInfo, &spdid, NULL, 0, &dwSize, NULL); // check the buffer size

		if ( dwSize!=0 && dwSize<=sizeof(Buf) ) {

			pspdidd->cbSize = sizeof(*pspdidd); // 5 Bytes!

			ZeroMemory(&spdd, sizeof(spdd));
			spdd.cbSize = sizeof(spdd);

			long res = SetupDiGetDeviceInterfaceDetail(hDevInfo, &spdid, pspdidd, dwSize, &dwSize, &spdd);
			if ( res ) {

				// in case you are interested in the USB serial number:
				// the device id string contains the serial number if the device has one,
				// otherwise a generated id that contains the '&' char...
				/*
				DEVINST DevInstParent = 0;
				CM_Get_Parent(&DevInstParent, spdd.DevInst, 0); 
				char szDeviceIdString[MAX_PATH];
				CM_Get_Device_ID(DevInstParent, szDeviceIdString, MAX_PATH, 0);
				printf("DeviceId=%s\n", szDeviceIdString);
				*/

				// open the disk or cdrom or floppy
				HANDLE hDrive = CreateFile(pspdidd->DevicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
				if ( hDrive != INVALID_HANDLE_VALUE ) {
					// get its device number
					STORAGE_DEVICE_NUMBER sdn;
					DWORD dwBytesReturned = 0;
					res = DeviceIoControl(hDrive, IOCTL_STORAGE_GET_DEVICE_NUMBER, NULL, 0, &sdn, sizeof(sdn), &dwBytesReturned, NULL);
					if ( res ) {
						if ( DeviceNumber == (long)sdn.DeviceNumber ) {  // match the given device number with the one of the current device
							CloseHandle(hDrive);
							SetupDiDestroyDeviceInfoList(hDevInfo);
							return spdd.DevInst;
						}
					}
					CloseHandle(hDrive);
				}
			}
		}
		dwIndex++;
	}

	SetupDiDestroyDeviceInfoList(hDevInfo);

	return 0;
}
//-----------------------------------------------------------
}