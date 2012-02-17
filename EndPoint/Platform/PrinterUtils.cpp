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

#include "Stdafx.h"
#include "PrinterUtils.h"

using namespace MyDLP::EndPoint::Core;
using namespace Microsoft::Win32;
using namespace System;
using namespace System::Runtime::InteropServices;

namespace MyDLPEP
{
	void PrinterUtils::HidePrinter(String^ pName)
	{	
		Logger::GetInstance()->Debug("Hide Printer " + pName);	

		HANDLE pHandle = GetPrinterHandle(pName);
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

				pPrinterInfo->Attributes = pPrinterInfo->Attributes | PRINTER_ATTRIBUTE_HIDDEN;
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

	void PrinterUtils::RevealPrinter(String^ pName)
	{		
		Logger::GetInstance()->Debug("Reveal Printer: " + pName);

		HANDLE pHandle = GetPrinterHandle(pName);
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
				Logger::GetInstance()->Debug("GetPrinterInfo succeded: " + gcnew String (pPrinterInfo->pPrinterName));
				pPrinterInfo->Attributes = pPrinterInfo->Attributes & ~PRINTER_ATTRIBUTE_HIDDEN;
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
		PRINTER_DEFAULTS pdAiO;
		pdAiO.pDevMode=NULL;
		pdAiO.pDatatype=NULL;
		pdAiO.DesiredAccess=PRINTER_ALL_ACCESS;

		HANDLE hPrinter = 0;

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
}