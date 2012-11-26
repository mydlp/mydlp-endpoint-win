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

#pragma once
#include <windows.h>
#include <strsafe.h>
#include <Setupapi.h>
#include <stdio.h>
#define MAX_DEVICE_COUNT 3

namespace MyDLPEP
{
	public ref class KbFilterController{

	private:
		KbFilterController( void );
		static KbFilterController ^controller = nullptr;	
		array<HANDLE>^ devices; 

	public:
		static KbFilterController ^GetInstance();
		static int configAttempt = 0;
		void Start();
		void Stop();
		void ActivateDevice();
		void DeactivateDevice();
	};
}