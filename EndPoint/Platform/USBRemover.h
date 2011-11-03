#pragma once
#include <windows.h>
#include <strsafe.h>
#include <Setupapi.h>
#include <stdio.h>

using namespace System;

namespace MyDLPEP
{
	//Listener stops when MyDLPMF stops
	public ref class USBRemover			
	{
	
	public:
		static int remove(String^);	
		static wchar_t * ManagedStringToUnicodeString(String ^);
	};
}
