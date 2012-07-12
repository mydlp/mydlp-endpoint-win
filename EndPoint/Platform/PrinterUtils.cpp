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
using namespace System::ComponentModel;

namespace MyDLPEP
{
	String^ PrinterUtils::GetPrinterSecurityDescriptor(String ^pName)
	{
		HANDLE pHandle = NULL;
		HANDLE hToken = NULL;
		DWORD pcbNeeded = 0;
		PRINTER_INFO_3* pPrinterInfo = NULL;
		PSECURITY_DESCRIPTOR psd = NULL;
		LPWSTR lpszSecDesc = NULL;
		String^ securityDescriptor = "";
		IntPtr cPtr = Marshal::StringToHGlobalUni(pName);

		try
		{
			if (!OpenProcessToken(GetCurrentProcess(),
				TOKEN_ADJUST_PRIVILEGES,
				&hToken))
			{
				throw gcnew Exception("Unable to open proces token");
			}

			if (!SetPrivilege(hToken, SE_BACKUP_NAME, TRUE))
			{
				throw gcnew Exception("SetPriviledge SE_BACKUP_NAME true failed");
			}

			int error = GetNamedSecurityInfo(
						(LPWSTR)cPtr.ToPointer(),
						SE_PRINTER,
						OWNER_SECURITY_INFORMATION|GROUP_SECURITY_INFORMATION|
						DACL_SECURITY_INFORMATION|SACL_SECURITY_INFORMATION,
						NULL,
						NULL,
						NULL,
						NULL,
						&psd); 
			if (error != ERROR_SUCCESS)
			{
				throw gcnew Win32Exception(error); 
			}

			if (!SetPrivilege(hToken, SE_BACKUP_NAME, FALSE))
			{
				throw gcnew Exception("SetPriviledge SE_BACKUP_NAME false failed");
			}

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
		}
		catch (Exception ^ex)
		{
			int error = (int) GetLastError();
			Logger::GetInstance()->Error("GetSecurityDescriptorStringForPrinter error: " +
				ex->Message + " " +
				" LastError: " + (gcnew Win32Exception(error))->Message + " " +
				ex->StackTrace);
		}
		finally
		{
			if (pPrinterInfo)
				free(pPrinterInfo);
			if (pHandle)
				ClosePrinter(pHandle);
			if (lpszSecDesc)
				LocalFree(lpszSecDesc);
			if (psd)
				LocalFree(psd);

			Marshal::FreeHGlobal(cPtr);
			CloseHandle(hToken);
		}

		Logger::GetInstance()->Debug("GetSecurityDescriptorStringForPrinter pName:" + pName + " result, secDesc:" + securityDescriptor);
		return securityDescriptor;
	}

	bool PrinterUtils::SetPrinterSecurityDescriptor(String ^pName, String ^secDesc)
	{
		HANDLE pHandle = NULL;
		DWORD pcbNeeded	= 0;
		PRINTER_INFO_3* pPrinterInfo = NULL;
		PSECURITY_DESCRIPTOR psd = NULL;
		LPWSTR lpszSecDesc = NULL;
		HANDLE hToken = NULL;
		IntPtr cPtr = IntPtr::Zero;
		IntPtr cPtrSec = IntPtr::Zero;


		PSID psidOwner = NULL;
		PSID psidGroup = NULL;
		PACL pDacl = NULL;
		PACL pSacl =NULL;

		cPtr = Marshal::StringToHGlobalUni(pName);
		cPtrSec = Marshal::StringToHGlobalUni(secDesc);

		bool errorFlag = false;

		Logger::GetInstance()->Debug("SetPrinterSecurityDescriptor  pName:" + pName + " secDesc: " + secDesc);

		try
		{
			if (!OpenProcessToken(GetCurrentProcess(),
				TOKEN_ADJUST_PRIVILEGES,
				&hToken))
			{
				throw gcnew Exception("Unable to open proces token");
			}

			if (!SetPrivilege(hToken, SE_RESTORE_NAME, TRUE))
			{
				throw gcnew Exception("SetPriviledge SE_RESTORE_NAME true failed");
			}

			if (!SetPrivilege(hToken, SE_TAKE_OWNERSHIP_NAME, TRUE))
			{
				throw gcnew Exception("SetPriviledge SE_TAKE_OWNERSHIP_NAME true failed");
			}

			if (!SetPrivilege(hToken, SE_SECURITY_NAME, TRUE))
			{
				throw gcnew Exception("SetPriviledge SE_SECURITY_NAME true failed");
			}


			if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
				(LPWSTR)cPtrSec.ToPointer(),
				SDDL_REVISION_1,
				&psd,
				NULL
				))
			{
				throw gcnew Exception("ConvertStringSecurityDescriptorToSecurityDescriptor failed");
			}

			BOOL bOwnerDefaulted;
			GetSecurityDescriptorOwner(psd, &psidOwner, &bOwnerDefaulted);

			BOOL bGroupDefaulted;
			GetSecurityDescriptorGroup(psd, &psidGroup, &bGroupDefaulted);

			BOOL bSaclPresent;
			BOOL bSaclDefaulted;
			GetSecurityDescriptorSacl(psd, &bSaclPresent, &pSacl, &bSaclDefaulted);

			BOOL bDaclPresent;
			BOOL bDaclDefaulted;
			GetSecurityDescriptorDacl(psd,  &bDaclPresent, &pDacl, &bDaclDefaulted);


			int error = SetNamedSecurityInfo(
						(LPWSTR)cPtr.ToPointer(),
						SE_PRINTER,
						OWNER_SECURITY_INFORMATION|GROUP_SECURITY_INFORMATION|
						DACL_SECURITY_INFORMATION|SACL_SECURITY_INFORMATION,
						psidOwner,
						psidGroup,
						pDacl,
						pSacl);

			if (error != ERROR_SUCCESS)
			{
				throw gcnew Win32Exception(error); 
			}
		}
		catch (Exception ^ex)
		{
			Logger::GetInstance()->Error("SetSecurityDescriptorStringForPrinter error: " +
				ex->Message + " " +
				ex->StackTrace);

			errorFlag = true;
		}
		finally
		{
			if( pPrinterInfo)
				free(pPrinterInfo);
			if (pHandle)
				ClosePrinter(pHandle);
			if (cPtr != IntPtr::Zero)
				Marshal::FreeHGlobal(cPtr);
			if (cPtrSec != IntPtr::Zero)
				Marshal::FreeHGlobal(cPtrSec);
			CloseHandle(hToken);
			LocalFree(psd);
		}
		return errorFlag;
	}

	void PrinterUtils::RemovePrinter(String^ pName)
	{
		SetLastError(0);
		Logger::GetInstance()->Debug("Removing Printer: " + pName);
		HANDLE pHandle = GetPrinterHandle(pName);
		if (!pHandle)
		{
			int error = (int)GetLastError();
			Logger::GetInstance()->Error("RemovePrinter failed null handle" + pName);
			Logger::GetInstance()->Error("LastError: " + (gcnew Win32Exception(error))->Message);
			return;
		}

		SetPrinter(pHandle,0,NULL,PRINTER_CONTROL_PURGE);
		if (DeletePrinter(pHandle))
		{
			Logger::GetInstance()->Debug("Remove printer success: " + pName);
		}
		else
		{
			Logger::GetInstance()->Error("Remove printer failed: " + pName);
			int error = (int)GetLastError();
			Logger::GetInstance()->Error("LastError: " + (gcnew Win32Exception(error))->Message);
		}
		ClosePrinter(pHandle);
	}

	HANDLE PrinterUtils::GetPrinterHandle(System::String ^pName)
	{
		PRINTER_DEFAULTS	pdAiO;
		pdAiO.pDevMode		= NULL;
		pdAiO.pDatatype		= NULL;
		pdAiO.DesiredAccess = PRINTER_ALL_ACCESS;
		LPVOID sidAndAttrBuffer = NULL;
		DWORD returnLength = 0;

		HANDLE hPrinter = NULL;
		HANDLE hToken = NULL;

		IntPtr cPtr = Marshal::StringToHGlobalUni(pName);

		SetLastError(0);

		if(OpenPrinter((LPWSTR)cPtr.ToPointer(), &hPrinter, &pdAiO))
		{
			Logger::GetInstance()->Debug("Get handle success: " + pName + "<" + gcnew String((LPWSTR)cPtr.ToPointer()) + "> hadle no: <" + gcnew INT32((int)hPrinter) + ">");
		}
		else
		{
			int error = (int)GetLastError();

			if (error == ERROR_ACCESS_DENIED)
			{
				Logger::GetInstance()->Error("Get handle failed ACCESS_DENIED");
				return NULL;
			}
			else
			{
			Logger::GetInstance()->Error("Get handle failed: " + pName + " " + (gcnew Win32Exception(error))->Message + "||");
			}
		}

		Marshal::FreeHGlobal(cPtr);

		if(!hToken)
			CloseHandle(hToken);

		if(!sidAndAttrBuffer)
			free(sidAndAttrBuffer);

		return hPrinter;
	}

	void PrinterUtils::TakePrinterOwnership(System::String ^pName)
	{
		HANDLE hToken = NULL;
		IntPtr cPtr = Marshal::StringToHGlobalUni(pName);
		SetLastError(0);

		try
		{
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

			if (!SetPrivilege(hToken, SE_BACKUP_NAME, TRUE))
			{
				throw gcnew Exception("SetPriviledge SE_BACKUP_NAME failed");
			}

			if (!SetPrivilege(hToken, SE_SECURITY_NAME, TRUE))
			{
				throw gcnew Exception("SetPriviledge SE_SECURITY_NAME failed");
			}

			PSID pSID = NULL;

			if (System::Environment::UserInteractive == true)
			{
				SID_IDENTIFIER_AUTHORITY SIDAuthNT = SECURITY_NT_AUTHORITY;
				AllocateAndInitializeSid(&SIDAuthNT, 2,
				 SECURITY_BUILTIN_DOMAIN_RID,
				 DOMAIN_ALIAS_RID_ADMINS,
				 0, 0, 0, 0, 0, 0,
				 &pSID);
			}
			else
			{
				SID_IDENTIFIER_AUTHORITY SIDAuthNT = SECURITY_NT_AUTHORITY;
				AllocateAndInitializeSid(&SIDAuthNT, 1,
				 SECURITY_LOCAL_SYSTEM_RID,
				 0, 0, 0, 0, 0, 0, 0,
				 &pSID);
			}

			int error = SetNamedSecurityInfo(
				(LPWSTR)cPtr.ToPointer(),
				SE_PRINTER,
				OWNER_SECURITY_INFORMATION,
				pSID, // SID
				NULL,
				NULL,
				NULL);

			if (error != ERROR_SUCCESS)
			{
				throw gcnew Win32Exception(error); 
			}

			if (!SetPrivilege(hToken, SE_SECURITY_NAME, FALSE))
			{
				throw gcnew Exception("SetPriviledge SE_SECURITY_NAME falsefailed");
			}

			if (!SetPrivilege(hToken, SE_BACKUP_NAME, FALSE))
			{
				throw gcnew Exception("SetPriviledge SE_BACKUP_NAME false failed");
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
		catch(Exception ^ex)
		{
			Logger::GetInstance()->Error("GetPrinterHandle error: " +
				ex->Message + " " +
				ex->StackTrace);
		}

		Marshal::FreeHGlobal(cPtr);

		if(!hToken)
			CloseHandle(hToken);
		return;
	}

	void PrinterUtils::SetPrinterSpoolMode(String^ pName, bool spool)
	{
		Logger::GetInstance()->Debug("Set pinter spooling mode " + pName + " " + spool);

		HANDLE pHandle = PrinterUtils::GetPrinterHandle(pName);

		if (!pHandle)
		{
			int error = (int)GetLastError();
			Logger::GetInstance()->Error("Set pinter spooling mode failed null handle" + pName + " " + spool);
			Logger::GetInstance()->Error("LastError: " + (gcnew Win32Exception(error))->Message);
			return;
		}

		DWORD pcbNeeded = 0;
		PRINTER_INFO_5* pPrinterInfo = NULL;

		GetPrinter(pHandle, 5, (LPBYTE)pPrinterInfo, 0, &pcbNeeded);
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
					int error = (int) GetLastError();
					Logger::GetInstance()->Error("SetPrinterInfo failed: " + pName);
					Logger::GetInstance()->Error("LastError: " + (gcnew Win32Exception(error))->Message);

				}
			}
			else
			{
				Logger::GetInstance()->Error("GetPrinterInfo failed: " + pName);
			}
			free(pPrinterInfo);
			ClosePrinter(pHandle);
		}
	}

	bool PrinterUtils::CheckIfPrinterDriverExists(String ^driverName)
	{
		bool foundFlag = false;
		int i = 0;

		DWORD pcbNeeded = 0;
		DWORD pcbReturned = 0;
		DRIVER_INFO_1* pDriverInfo = NULL;

		try
		{
			EnumPrinterDrivers(NULL, NULL, 1, (LPBYTE)pDriverInfo, 0, &pcbNeeded, &pcbReturned);
			pDriverInfo = (DRIVER_INFO_1*) malloc(pcbNeeded);

			if ( !EnumPrinterDrivers(NULL, NULL, 1, (LPBYTE)pDriverInfo, pcbNeeded, &pcbNeeded, &pcbReturned))
			{
				throw gcnew Exception("EnumPrinterDrivers failed");
			}

			for (i = 0; i < pcbReturned; i++)
			{
				String^ dName = gcnew String(pDriverInfo[i].pName);
				if (dName == driverName)
				{
					foundFlag = true;
					break;
				}
			}

			if(pDriverInfo)
				free(pDriverInfo);
		}
		catch(Exception ^ex)
		{
			if(pDriverInfo)
				free(pDriverInfo);
		}

		return foundFlag;
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
		int error = (int)GetLastError();
		Logger::GetInstance()->Error("LookupPrivilegeValue error: " + (gcnew Win32Exception(error))->Message);
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
		  int error = (int) GetLastError();
		  Logger::GetInstance()->Error("AdjustTokenPrivileges LastError: " + (gcnew Win32Exception(error))->Message);
		  return FALSE;
	}

	if (GetLastError() == ERROR_NOT_ALL_ASSIGNED)
	{
		  Logger::GetInstance()->Error("The token does not have the specified privilege.");
		  return FALSE;
	}
	return TRUE;
}