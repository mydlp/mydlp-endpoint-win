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
#include <gpedit.h>
#include <gpmgmt.h>

using namespace System;
using namespace System::Threading;
using namespace System::Runtime::InteropServices;
using namespace MyDLP::EndPoint::Core;

namespace MyDLPEP
{

	public ref class LGPOEditor
	{

	public:		
		static int EditDword(String ^ regPath, String ^ valName, int val);

	private:

		LGPOEditor(){};	
		~LGPOEditor(){};
		String^ regPath;
		String^ valName; 
		int value;
		int oldVal;
		void Worker();
	};

}