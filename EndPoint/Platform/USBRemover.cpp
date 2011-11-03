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
#include <vcclr.h>
#include "USBRemover.h"

using namespace MyDLP::EndPoint::Core;

namespace MyDLPEP
{

int USBRemover::remove(String ^deviceIdString)
{
	DEVNODE devHandle;
	ULONG status;
	ULONG problemNumber;
	int pVetoType;
	char pszVetoName;

	DEVINSTID_W deviceId = (DEVINSTID_W) ManagedStringToUnicodeString(deviceIdString->ToUpper());

	wprintf(L"%s\n",deviceId);
	int stat = CM_Locate_DevNodeW(&devHandle, deviceId, CM_LOCATE_DEVNODE_NORMAL);
	
	if ( stat == CR_SUCCESS)
	{
		stat = CM_Get_DevNode_Status(&status, &problemNumber, devHandle, 0);
		if( stat == CR_SUCCESS)
		{
			stat = CM_Request_Device_EjectA(devHandle, NULL, NULL, 0, 0);
			if (stat == CR_SUCCESS)
			{
				return 1;
			}
			else
			{
				Logger::GetInstance()->Debug("CM_Request_Device_EjectA failed:" + (gcnew INT(stat))->ToString("X"));	
			}
		}
		else
		{
			Logger::GetInstance()->Debug("CM_Get_DevNode_Status failed:" + (gcnew INT(stat))->ToString("X"));	
		}
	}
	else
	{
		Logger::GetInstance()->Debug("CM_Locate_DevNode failed:" + (gcnew INT(stat))->ToString("X"));
		wprintf(L"%s\n",deviceId);
	}


	return 0;
}

wchar_t *USBRemover::ManagedStringToUnicodeString(String ^s)
{
    // Declare
    wchar_t *ReturnString = nullptr;
    long len = s->Length;

    // Check length
    if(len == 0) return nullptr;

    // Pin the string
    pin_ptr<const wchar_t> PinnedString = PtrToStringChars(s);

    // Copy to new string
    ReturnString = (wchar_t *)malloc((len+1)*sizeof(wchar_t));
    if(ReturnString)
    {
        wcsncpy(ReturnString, (wchar_t *)PinnedString, len+1);
    }

    // Unpin
    PinnedString = nullptr;

    // Return
    return ReturnString;
}

}