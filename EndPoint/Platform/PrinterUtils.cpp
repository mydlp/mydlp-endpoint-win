//    Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
//
//--------------------------------------------------------------------------
//    This file is part of MyDLP.
//
//    MyDLP is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    MyDLP is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with MyDLP.  If not, see <http://www.gnu.org/licenses/>.
//--------------------------------------------------------------------------

#include "stdafx.h"
#include "PrinterUtils.h"

using namespace MyDLP::EndPoint::Core;
using namespace Microsoft::Win32;
using namespace System;
using namespace System::Runtime::InteropServices;

namespace MyDLPEP
{
	String^ PrinterUtils::GetPrinterSecurityDescriptor(String ^pName)
	{
		HANDLE pHandle = NULL;
		DWORD pcbNeeded = 0;
		DWORD cBuf = 0;
		PRINTER_INFO_3* pPrinterInfo = NULL;
		PSECURITY_DESCRIPTOR psd = NULL;
		LPWSTR lpszSecDesc = NULL;

		String^ securityDescriptor = "";

		try
		{
			pHandle = PrinterUtils::GetPrinterHandle(pName);

			if (!pHandle)
			{
				throw gcnew Exception("Unable to get printer handle");
			}

			GetPrinter(pHandle, 3, (LPBYTE)pPrinterInfo, 0, &pcbNeeded);
			cBuf = pcbNeeded;
			pPrinterInfo = (PRINTER_INFO_3*) malloc(pcbNeeded);

			if (!pPrinterInfo)
			{
				throw gcnew Exception("Memory allocation error for PRINTER_INFO_3");
			}

			if (!GetPrinter(pHandle, 3, (LPBYTE)pPrinterInfo, pcbNeeded, &pcbNeeded))
			{
				throw gcnew Exception("Unable to get PRINTER_INFO_3");
			}

			psd = pPrinterInfo->pSecurityDescriptor;

			if (!ConvertSecurityDescriptorToStringSecurityDescriptor(
				psd,
				SDDL_REVISION_1,
				OWNER_SECURITY_INFORMATION,
				&lpszSecDesc,
				NULL))
			{
				throw gcnew Exception("ConvertSecurityDescriptorToString for OWNER_SECURITY_INFORMATION failed");
			}

			securityDescriptor +=  gcnew String(lpszSecDesc);
			LocalFree(lpszSecDesc);
			lpszSecDesc = NULL;

			if (!ConvertSecurityDescriptorToStringSecurityDescriptor(
				psd,
				SDDL_REVISION_1,
				GROUP_SECURITY_INFORMATION,
				&lpszSecDesc,
				NULL))
			{
				throw gcnew Exception("ConvertSecurityDescriptorToString for GROUP_SECURITY_INFORMATION failed");
			}

			securityDescriptor +=  gcnew String(lpszSecDesc);
			LocalFree(lpszSecDesc);
			lpszSecDesc = NULL;

			if (!ConvertSecurityDescriptorToStringSecurityDescriptor(
				psd,
				SDDL_REVISION_1,
				DACL_SECURITY_INFORMATION,
				&lpszSecDesc,
				NULL))
			{
				throw gcnew Exception("ConvertSecurityDescriptorToString for DACL_SECURITY_INFORMATION failed");
			}

			securityDescriptor +=  gcnew String(lpszSecDesc);
			LocalFree(lpszSecDesc);
			lpszSecDesc = NULL;

			if (!ConvertSecurityDescriptorToStringSecurityDescriptor(
				psd,
				SDDL_REVISION_1,
				SACL_SECURITY_INFORMATION,
				&lpszSecDesc,
				NULL))
			{
				throw gcnew Exception("ConvertSecurityDescriptorToString for SACL_SECURITY_INFORMATION failed");
			}

			securityDescriptor +=  gcnew String(lpszSecDesc);
			LocalFree(lpszSecDesc);
			lpszSecDesc == NULL;
		}

		catch (Exception ^ex)
		{
			Logger::GetInstance()->Error("GetSecurityDescriptorStringForPrinter error: " +
				ex->Message + " " +
				" LastError: " + GetLastError() + " " +
				ex->StackTrace);

			if (lpszSecDesc)
				LocalFree(lpszSecDesc);
		}

		finally
		{
			if (pPrinterInfo)
				free(pPrinterInfo);
			if (pHandle)
				ClosePrinter(pHandle);			
		}

		return securityDescriptor;
	}

	bool PrinterUtils::SetPrinterSecurityDescriptor(String ^pName, String ^secDesc)
	{
		HANDLE pHandle = NULL;
		DWORD pcbNeeded	= 0;
		DWORD cBuf = 0;
		PRINTER_INFO_3* pPrinterInfo = NULL;

		PSECURITY_DESCRIPTOR psd = NULL;
		LPWSTR lpszSecDesc = NULL;

		HANDLE hToken = NULL;

		IntPtr cPtr = IntPtr::Zero;
		bool errorFlag = false;

		try
		{
			pHandle = PrinterUtils::GetPrinterHandle(pName);

			if (!pHandle)
			{
				throw gcnew Exception("Unable to get printer handle");
			}

			GetPrinter(pHandle, 3, (LPBYTE)pPrinterInfo, 0, &pcbNeeded);
			cBuf = pcbNeeded;
			pPrinterInfo = (PRINTER_INFO_3*) malloc(pcbNeeded);

			if (!pPrinterInfo)
			{
				throw gcnew Exception("Memory allocation error for PRINTER_INFO_3");
			}

			if (!GetPrinter(pHandle, 3, (LPBYTE)pPrinterInfo, pcbNeeded, &pcbNeeded))
			{
				throw gcnew Exception("Unable to get PRINTER_INFO_3");
			}

			if (!OpenProcessToken(GetCurrentProcess(),
				TOKEN_ADJUST_PRIVILEGES,
				&hToken))
			{
				throw gcnew Exception("Unable to open proces token");
			}

			
			if (!SetPrivilege(hToken, SE_TAKE_OWNERSHIP_NAME, TRUE)) 
			{
				throw gcnew Exception("SetPriviledge SE_TAKE_OWNERSHIP_NAME failed");
			}
			
			if (!SetPrivilege(hToken, SE_RESTORE_NAME, TRUE)) 
			{
				throw gcnew Exception("SetPriviledge SE_RESTORE_NAME failed");
			}

			cPtr = Marshal::StringToHGlobalUni(secDesc);	

			if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
				(LPWSTR)cPtr.ToPointer(),
				SDDL_REVISION_1,
				&psd,
				NULL
				))
			{
				throw gcnew Exception("ConvertStringSecurityDescriptorToSecurityDescriptor failed");
			}

			pPrinterInfo->pSecurityDescriptor = psd;
			Marshal::FreeHGlobal(cPtr);
			cPtr = IntPtr::Zero;

			if (!SetPrinter(pHandle,3,(LPBYTE)pPrinterInfo,0))
			{
				throw gcnew Exception("SetPrinter failed");
			}

			if (!SetPrivilege(hToken, SE_TAKE_OWNERSHIP_NAME, FALSE))
			{
				throw gcnew Exception("SetPrivilidge SE_TAKE_OWNERSHIP_NAME false failed");
			}

			if (!SetPrivilege(hToken, SE_RESTORE_NAME, FALSE))
			{
				throw gcnew Exception("SetPrivilidge SE_RESTORE_NAME false failed");	
			}
		}
		catch (Exception ^ex)
		{
			Logger::GetInstance()->Error("GetSecurityDescriptorStringForPrinter error: " +
				ex->Message + " " +
				" LastError: " + GetLastError() + " " +
				ex->StackTrace);

			if (cPtr != IntPtr::Zero)				
				Marshal::FreeHGlobal(cPtr);

			errorFlag = true;
		}
		finally
		{
			if( pPrinterInfo)
				free(pPrinterInfo);
			if (pHandle)	
				ClosePrinter(pHandle);		
		}	
		return errorFlag;
	}	

	void PrinterUtils::RemovePrinter(String^ pName)
	{
		SetLastError(0);
		Logger::GetInstance()->Debug("Removing Printer: " + pName);
		HANDLE pHandle = GetPrinterHandle(pName);
		SetPrinter(pHandle,0,NULL,PRINTER_CONTROL_PURGE);
		if (DeletePrinter(pHandle))
		{
			Logger::GetInstance()->Debug("Remove printer success: " + pName);
		}
		else
		{
			Logger::GetInstance()->Debug("Remove printer failed: " + pName);
			DWORD word = GetLastError();
			Logger::GetInstance()->Debug("LastError: " + word.ToString());
		}
		ClosePrinter(pHandle);
	}

	HANDLE PrinterUtils::GetPrinterHandle(System::String ^pName)
	{
		PRINTER_DEFAULTS	pdAiO;
		pdAiO.pDevMode		= NULL;
		pdAiO.pDatatype		= NULL;
		pdAiO.DesiredAccess = PRINTER_ALL_ACCESS;

		HANDLE hPrinter = NULL;

		IntPtr cPtr = Marshal::StringToHGlobalUni(pName);	

		if(OpenPrinter((LPWSTR)cPtr.ToPointer(), &hPrinter, &pdAiO))
		{
			Logger::GetInstance()->Debug("Get handle success:<" + gcnew String((LPWSTR)cPtr.ToPointer()) + "> hadle no: <" + gcnew INT32((int)hPrinter) + ">");		
		}
		else
		{
			Logger::GetInstance()->Debug("Get handle failed");
		}

		Marshal::FreeHGlobal(cPtr);

		return hPrinter;	
	}

	void PrinterUtils::SetPrinterSpoolMode(String^ pName, bool spool)
	{	
		Logger::GetInstance()->Debug("Set pinter spooling mode " + pName + " " + spool);	

		HANDLE pHandle = PrinterUtils::GetPrinterHandle(pName);
		DWORD pcbNeeded = 0;
		DWORD cBuf = 0;
		PRINTER_INFO_5* pPrinterInfo = NULL;

		GetPrinter(pHandle, 5, (LPBYTE)pPrinterInfo, 0, &pcbNeeded);
		cBuf = pcbNeeded;
		pPrinterInfo = (PRINTER_INFO_5*) malloc(pcbNeeded);

		if (pPrinterInfo)
		{
			if (GetPrinter(pHandle, 5, (LPBYTE)pPrinterInfo, pcbNeeded, &pcbNeeded))
			{
				Logger::GetInstance()->Debug("GetPrinterInfo success: " + pName);

				if (spool)
					pPrinterInfo->Attributes = pPrinterInfo->Attributes & ~PRINTER_ATTRIBUTE_DIRECT;
				else
					pPrinterInfo->Attributes = pPrinterInfo->Attributes | PRINTER_ATTRIBUTE_DIRECT;

				if(SetPrinter(pHandle, 5, (LPBYTE)pPrinterInfo, 0))
				{
					Logger::GetInstance()->Debug("SetPrinterInfo succeded: " + pName);
				}
				else 
				{					
					Logger::GetInstance()->Debug("SetPrinterInfo failed: " + pName);
					DWORD word = GetLastError();
					Logger::GetInstance()->Debug("LastError: " + word.ToString());

				}
			}
			else 
			{					
				Logger::GetInstance()->Debug("GetPrinterInfo failed: " + pName);
			}
			free(pPrinterInfo);
			ClosePrinter(pHandle);
		}
	}
}
	
BOOL SetPrivilege(
	HANDLE hToken,
	LPCTSTR lpszPrivilege, 
	BOOL bEnablePrivilege) 
{
	TOKEN_PRIVILEGES tp;
	LUID luid;

	if (!LookupPrivilegeValue( 
		NULL, 
		lpszPrivilege,
		&luid ) )
	{
		Logger::GetInstance()->Debug("LookupPrivilegeValue error: " + gcnew Int32(GetLastError())); 
		return FALSE; 
	}

	tp.PrivilegeCount = 1;
	tp.Privileges[0].Luid = luid;
	if (bEnablePrivilege)
	{
		tp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
	}
	else
	{
		tp.Privileges[0].Attributes = 0;
	}

	if ( !AdjustTokenPrivileges(
		   hToken, 
		   FALSE, 
		   &tp, 
		   sizeof(TOKEN_PRIVILEGES), 
		   (PTOKEN_PRIVILEGES) NULL, 
		   (PDWORD) NULL) )
	{ 
		  Logger::GetInstance()->Debug("AdjustTokenPrivileges error: " + GetLastError()); 
		  return FALSE; 
	} 

	if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)
	{
		  Logger::GetInstance()->Debug("The token does not have the specified privilege.");
		  return FALSE;
	}
	return TRUE;
}