//    Copyright (C) 2012 Huseyin Ozgur Batur <ozgur@medra.com.tr>
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
#include <tchar.h>
#include "KbFilterController.h"

using namespace MyDLP::EndPoint::Core;
using namespace Microsoft::Win32;
using namespace System;
using namespace System::Runtime::InteropServices;


namespace MyDLPEP
{
	KbFilterController::KbFilterController( void )
	{
	};

	KbFilterController^ KbFilterController::GetInstance()
	{
		if (controller == nullptr)
		{
			controller = gcnew KbFilterController();
		}

		return controller;
	}

	void KbFilterController::Start()
	{		
		Logger::GetInstance()->Debug("Installing mydlpkbf service");

        configAttempt = 0;

		const LPCTSTR DRV_NAME = _T("MyDLPKBF");
		RegistryKey ^key = nullptr;

		SC_HANDLE hSCManager = OpenSCManager( NULL, NULL, SC_MANAGER_ALL_ACCESS );
		SC_HANDLE hService = OpenService( hSCManager , DRV_NAME, SERVICE_ALL_ACCESS);

		if (hService == 0)
		{
			Logger::GetInstance()->Debug("MyDLPKBF service does not exist");
			String ^driverPath = MyDLP::EndPoint::Core::Configuration::KbFilterPath;	
			IntPtr cPtr = Marshal::StringToHGlobalUni(driverPath);

			hService = CreateService( hSCManager, DRV_NAME, DRV_NAME, SERVICE_ALL_ACCESS,
									  SERVICE_KERNEL_DRIVER, SERVICE_DEMAND_START, SERVICE_ERROR_NORMAL,
									  (LPCWSTR)cPtr.ToPointer(),NULL, NULL, NULL, NULL, NULL );
			
			Marshal::FreeHGlobal(cPtr);
			
			if( 0 == hService )
			{
				Logger::GetInstance()->Error("Unable to create service, win error no:" + gcnew Int32(GetLastError()));
				CloseServiceHandle(hSCManager);
				return;
			}
			Logger::GetInstance()->Debug("Creating registry keys");
			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services");
			key->CreateSubKey("MyDLPKBF");
			key->Close();

			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPKBF" );
			key->SetValue("Description", "MyDLP Windows Endpoint Keyboard Filter" ,RegistryValueKind::String);

			key->Close();
			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPKBF" );
			key->CreateSubKey( "Instances" );
			key->Close();

			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPKBF\\Instances" );
			key->SetValue("DefaultInstance", "MyDLPKBF Instance", RegistryValueKind::String);
			key->CreateSubKey("MyDLPKBF Instance");
			key->Close();
			delete key;
		}

		Logger::GetInstance()->Debug("Starting mydlpkbf service");
		if (!StartService( hService, 0, NULL ))
		{
			if( GetLastError() != ERROR_SERVICE_ALREADY_RUNNING )
			{
				DeleteService(hService);
				CloseServiceHandle(hService);
				CloseServiceHandle(hSCManager);
				Logger::GetInstance()->Error("Unable to start MyDLPKBF service, win error no" + gcnew Int32(GetLastError()));
				return;
			}
			else
			{
				Logger::GetInstance()->Error("MyDLPKBF service already running");
			}
		}
		else 
		{
			Logger::GetInstance()->Info("mydlpkbf service started");
		}

		//Activate device
		/*drvDevice = CreateFileA( "\\\\.\\MyDLPKBF", GENERIC_READ, 0, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);

        if ( drvDevice == INVALID_HANDLE_VALUE )
		{
			Logger::GetInstance()->Error ("CreateFile Failed " + GetLastError());
          
        }*/

		CloseServiceHandle(hService);
		CloseServiceHandle(hSCManager);
	}


	void KbFilterController::Stop()
	{

		//Deactivate Device
		//CloseHandle(drvDevice);

		const LPCTSTR DRV_NAME = _T("MyDLPKBF");
		SC_HANDLE hSCManager = OpenSCManager( NULL, NULL, SC_MANAGER_ALL_ACCESS );
		SC_HANDLE hService = OpenService( hSCManager , DRV_NAME, SERVICE_ALL_ACCESS );
		SERVICE_STATUS  stSrvStatus = {0};
		BOOL stopped = ControlService( hService, SERVICE_CONTROL_STOP, &stSrvStatus );
		if (stopped)
		{
			Logger::GetInstance()->Info("mydlpkbf service stopped");
		}
		else
		{
			Logger::GetInstance()->Error("Unabled to stop mydlpkbf service, win error no" + gcnew Int32(GetLastError()));
		}

		BOOL bDeleted = DeleteService(hService);
		if (bDeleted)
		{
			Logger::GetInstance()->Info("mydlpkbf service removed");
		}
		else 
		{
			Logger::GetInstance()->Error("Unable to remove myflpkbf service, win error no" + gcnew Int32(GetLastError()));
		}
		CloseServiceHandle(hService);
		CloseServiceHandle(hSCManager);
	}
	void KbFilterController::ActivateDevice()
	{
		drvDevice = CreateFileA( "\\\\.\\MyDLPKBF", GENERIC_READ, 0, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);

        if ( drvDevice == INVALID_HANDLE_VALUE )
		{
			Logger::GetInstance()->Error ("CreateFile Failed " + GetLastError());
          
        }
	}

	void KbFilterController::DeactivateDevice()
	{
		CloseHandle(drvDevice);
	}
}