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

#pragma once
#include "Windows.h"
#include <stdio.h>
#include <stdlib.h>
#include <vcclr.h>
#include <sddl.h>
#include <accctrl.h>
#include <aclapi.h>


BOOL SetPrivilege(HANDLE hToken, LPCTSTR lpszPrivilege, BOOL bEnablePrivilege);

using namespace System;
using namespace MyDLP::EndPoint::Core;

namespace MyDLPEP
{
	public ref class PrinterUtils
	{

	public:
		static bool listenChanges;
		static HANDLE GetPrinterHandle(String ^printerName);	
		static HANDLE GetLocalPrintServerHandle();
		static void SetPrinterSpoolMode(String^ printerName, bool spool);	
		static void RemovePrinter(String^ printerName);
		static bool SetPrinterSecurityDescriptor(String ^pName, String ^secDesc);
		static String^ GetPrinterSecurityDescriptor(String ^pName);
		static bool CheckIfPrinterDriverExists(String ^driverName);
		static bool CheckIfPrinterPortExists(String ^portName);
		static void TakePrinterOwnership(String ^pName);
		static void StartBlockingLocalChangeListener();
		static String^ GetDefaultSystemPrinter();
		static bool SetDefaultSystemPrinter(String^);

		delegate void LocalPrinterRemoveHandlerDeleagate(void);
        static LocalPrinterRemoveHandlerDeleagate^ LocalPrinterRemoveHandler; 

		delegate void LocalPrinterAddHandlerDeleagate(void);
        static LocalPrinterAddHandlerDeleagate^ LocalPrinterAddHandler; 
	};

}