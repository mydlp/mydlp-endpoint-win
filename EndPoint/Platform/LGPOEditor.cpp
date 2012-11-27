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

#include "stdafx.h"
#include "LGPOEditor.h"

using namespace System;
using namespace System::ComponentModel;
using namespace System::Threading;
using namespace System::Runtime::InteropServices;
using namespace MyDLP::EndPoint::Core;

namespace MyDLPEP
{

	int LGPOEditor::EditDword(String ^ regPath, String ^ valName, int val)
	{

		LGPOEditor^ lgpoEdit = gcnew LGPOEditor();
		lgpoEdit->regPath = regPath;
		lgpoEdit->valName = valName;
		lgpoEdit->oldVal = -1;
		lgpoEdit->value = val;

		ThreadStart^ threadDelegate = gcnew ThreadStart(lgpoEdit,&LGPOEditor::Worker);
		Thread^ thread = gcnew Thread(threadDelegate);
		thread->SetApartmentState(ApartmentState::STA); 
		thread->Start(); 
		thread->Join(); 

		return lgpoEdit->oldVal;	
	}

	void LGPOEditor::Worker()
	{	
		HRESULT hr;
		int initialized = 0;

		IntPtr cPtr1 = IntPtr::Zero;
		IntPtr cPtr2 = IntPtr::Zero;

		GUID myCLSID_GroupPolicyObject = { 0xea502722, 0xa23d, 0x11d1, { 0xa7, 0xd3, 0x0, 0x0, 0xf8, 0x75, 0x71, 0xe3 } };
		IID myIID_IGroupPolicyObject = { 0xea502723, 0xa23d, 0x11d1 ,{ 0xa7, 0xd3, 0x0, 0x0, 0xf8, 0x75, 0x71, 0xe3 } };

		GUID ext_guid = REGISTRY_EXTENSION_GUID;
		GUID snap_guid = { 0x3935da5c, 0xef08, 0x48f3, { 0x84, 0xaa, 0xba, 0x11, 0x5f, 0x5c, 0x2d, 0x1d } };

		DWORD val, val_size=sizeof(DWORD);
		DWORD old_val; 

		IGroupPolicyObject* pLGPO = NULL;
		HKEY machine_key = NULL;
		HKEY dsrkey = NULL;

		hr = CoInitializeEx( NULL, COINIT_APARTMENTTHREADED );		
		if ( FAILED(hr))
		{
			Logger::GetInstance()->Error ("Failed CoInitializeEx:" + ErrorToString(hr));
			goto error;
		}

		hr = CoCreateInstance(myCLSID_GroupPolicyObject, NULL, CLSCTX_INPROC_SERVER,
			myIID_IGroupPolicyObject, (LPVOID*)&pLGPO);

		if ( FAILED(hr))
		{
			Logger::GetInstance()->Error ("Failed CoCreateInstance:" + ErrorToString(hr));
			goto error;
		}
		initialized = 1;

		pLGPO->OpenLocalMachineGPO(GPO_OPEN_LOAD_REGISTRY);
		pLGPO->GetRegistryKey(GPO_SECTION_MACHINE, &machine_key);

		cPtr1 = Marshal::StringToHGlobalUni(regPath);
		hr  = RegCreateKeyEx(machine_key, (LPCWSTR) cPtr1.ToPointer(),
			0, NULL, 0, KEY_SET_VALUE | KEY_QUERY_VALUE, NULL, &dsrkey, NULL);

		if (FAILED(hr))
		{
			Logger::GetInstance()->Error ("Failed RegCreateKeyEx:" + ErrorToString(hr));
			goto error;
		}

		val = this->value;

		cPtr2 = Marshal::StringToHGlobalUni(valName);

		hr = RegQueryValueEx (dsrkey,(LPCWSTR) cPtr2.ToPointer(),NULL, NULL, (LPBYTE) &old_val, &val_size);
		if (FAILED(hr))
		{
			Logger::GetInstance()->Error ("Failed RegGetValue:" + ErrorToString(hr));
			goto error;
		}

		this->oldVal = old_val;

		hr = RegSetValueEx(dsrkey,(LPCWSTR) cPtr2.ToPointer(),NULL, REG_DWORD, (LPBYTE)&val, sizeof(val));

		if (FAILED(hr))
		{
			Logger::GetInstance()->Error ("Failed RegSetKeyValue:" + ErrorToString(hr));
			goto error;
		}

		hr = pLGPO->Save(TRUE, TRUE, &ext_guid, &snap_guid);
		if (FAILED(hr))
		{
			Logger::GetInstance()->Error("Failed  pLGPO->Save:" + ErrorToString(hr));
			goto error;
		}

error:	

		if (IntPtr::Zero != cPtr1)
			Marshal::FreeHGlobal(cPtr1);

		if (IntPtr::Zero != cPtr2)
			Marshal::FreeHGlobal(cPtr2);		

		if (NULL != machine_key)
			RegCloseKey(machine_key);

		if (NULL != dsrkey)
			RegCloseKey(dsrkey);

		if (NULL != pLGPO)
			pLGPO->Release();

		if (initialized)
			CoUninitialize();
	}

	String^ LGPOEditor::ErrorToString(int errorno)
	{
		Win32Exception^ ex = gcnew Win32Exception(errorno);
		return " errrono:" + ex->ErrorCode.ToString("X") + "  message:" + ex->Message; 

	}
}