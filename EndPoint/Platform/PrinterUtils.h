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

#include "Windows.h"
#include <stdio.h>
#include <stdlib.h>
#include <vcclr.h>

using namespace System;
using namespace MyDLP::EndPoint::Core;

namespace MyDLPEP
{
	public ref class PrinterUtils{

	private:
		static HANDLE GetPrinterHandle(String ^printerName);
	public:
		static void HidePrinter(String^ printerName);
		static void RevealPrinter(String^ printerName);
		static void RemovePrinter(String^ printerName);
	};
}