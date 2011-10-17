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
#include <tchar.h>
#include "MiniFilterController.h"

using namespace MyDLP::EndPoint::Core;
using namespace Microsoft::Win32;
using namespace System;
using namespace System::Runtime::InteropServices;


namespace MyDLPEP
{
	MiniFilterController::MiniFilterController( void )
	{

	};

	MiniFilterController^ MiniFilterController::GetInstance()
	{
		if (controller == nullptr)
		{
			controller = gcnew MiniFilterController();
		}

		return controller;
	}

	void MiniFilterController::Start()
	{		
		Logger::GetInstance()->Debug("Installing mydlpmf service");
		const LPCTSTR DRV_NAME = _T("MyDLPMF");
		const LPCTSTR DRV_FILE_NAME = _T("MyDLPMF.sys");
		RegistryKey ^key = nullptr;

		SC_HANDLE hSCManager = OpenSCManager( NULL, NULL, SC_MANAGER_ALL_ACCESS );
		SC_HANDLE hService = OpenService( hSCManager , DRV_NAME, SERVICE_ALL_ACCESS);

		if (hService == 0)
		{
			Logger::GetInstance()->Debug("MyDLPMF service does not exist");
			String ^driverPath = MyDLP::EndPoint::Core::Configuration::MinifilterPath;	
			IntPtr cPtr = Marshal::StringToHGlobalUni(driverPath);

			hService = CreateService( hSCManager, DRV_NAME, DRV_NAME, SERVICE_ALL_ACCESS,
									  SERVICE_FILE_SYSTEM_DRIVER, SERVICE_DEMAND_START, SERVICE_ERROR_NORMAL,
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
			key->CreateSubKey("MyDLPMF");
			key->Close();

			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPMF" );
			key->SetValue("Tag", 0x0000000e, RegistryValueKind::DWord);
			key->SetValue("Group", "FSFilter Content Screener", RegistryValueKind::String);
			key->SetValue("DependOnGroup", gcnew array<String ^>{},RegistryValueKind::MultiString);
			key->SetValue("DependOnService", gcnew array<String ^>{"FltMgr"} ,RegistryValueKind::MultiString);
			key->SetValue("Description", "MyDLP Windows Endpoint MiniFilter" ,RegistryValueKind::String);

			key->Close();
			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPMF" );
			key->CreateSubKey( "Instances" );
			key->Close();

			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPMF\\Instances" );
			key->SetValue("DefaultInstance", "MyDLPMF Instance", RegistryValueKind::String);
			key->CreateSubKey("MyDLPMF Instance");
			key->Close();

			key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPMF\\Instances\\MyDLPMF Instance" );
			key->SetValue("Altitude", "268100", RegistryValueKind::String);
			Int32 flagVal = 0;
			key->SetValue("Flags", flagVal, RegistryValueKind::DWord);
			key->Close();
			delete key;
		}

		key = Registry::LocalMachine->CreateSubKey( "System\\CurrentControlSet\\Services\\MyDLPMF" );
		key->SetValue("Tag", 0x0000000e, RegistryValueKind::DWord);
		key->SetValue("Group", "FSFilter Content Screener", RegistryValueKind::String);
		key->SetValue("DependOnGroup", gcnew array<String ^>{}, RegistryValueKind::MultiString);
		key->SetValue("DependOnService", gcnew array<String ^>{"FltMgr"}, RegistryValueKind::MultiString);
		key->SetValue("Description", "MyDLP Windows Endpoint MiniFilter", RegistryValueKind::String);

		Logger::GetInstance()->Debug("Starting mydlpmf service");
		if (!StartService( hService, 0, NULL ))
		{
			if( GetLastError() != ERROR_SERVICE_ALREADY_RUNNING )
			{
				DeleteService(hService);
				CloseServiceHandle(hService);
				CloseServiceHandle(hSCManager);
				Logger::GetInstance()->Error("Unable to start MyDLPMF service, win error no" + gcnew Int32(GetLastError()));
				return;
			} else
				Logger::GetInstance()->Error("MyLDLPMF service already running");
		}
		else 
		{
			Logger::GetInstance()->Info("mydlpmf service started");
		}

		CloseServiceHandle(hService);
		CloseServiceHandle(hSCManager);
	}


	void MiniFilterController::Stop()
	{
		const LPCTSTR DRV_NAME = _T("MyDLPMF");
		SC_HANDLE hSCManager = OpenSCManager( NULL, NULL, SC_MANAGER_ALL_ACCESS );
		SC_HANDLE hService = OpenService( hSCManager , DRV_NAME, SERVICE_ALL_ACCESS );
		SERVICE_STATUS  stSrvStatus = {0};
		BOOL stopped = ControlService( hService, SERVICE_CONTROL_STOP, &stSrvStatus );
		if (stopped)
		{
			Logger::GetInstance()->Info("mydlpmf service stopped");
		}
		else
		{
			Logger::GetInstance()->Error("Unabled to stop mydlpmf service, win error no" + gcnew Int32(GetLastError()));
		}

		BOOL bDeleted = DeleteService(hService);
		if (bDeleted)
		{
			Logger::GetInstance()->Info("mydlpmf service removed");
		}
		else 
		{
			Logger::GetInstance()->Error("Unable to remove mydlpmf service, win error no" + gcnew Int32(GetLastError()));
		}
		CloseServiceHandle(hService);
		CloseServiceHandle(hSCManager);
	}
}