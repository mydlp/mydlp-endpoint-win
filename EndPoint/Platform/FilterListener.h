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
#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <winioctl.h>
#include <string.h>
#include <crtdbg.h>
#include <assert.h>
#include <fltuser.h>
#include <dontuse.h>
#include "Debug.h"
#include <mydlp_common.h>

using namespace System;
using namespace MyDLP::EndPoint::Core;

#pragma pack(1)
typedef struct _MYDLPMF_MESSAGE {

	//
	//  Required structure header.
	//

	FILTER_MESSAGE_HEADER MessageHeader;

	// Largest notification type is used here
	// MYDLPMF_MESSAGE will be used to recieve both MYDLPMF_WRITE_NOTIFICATION
	// MYDLPMF_FILE_NOTIFICATION and MYDLPMF_NOTIFICATION

	_MYDLPMF_WRITE_NOTIFICATION Notification;

	//
	//  Overlapped structure, DO NOT change its order in structure 
	//  since it is used with FIELD_OFFSET
	//

	OVERLAPPED Ovlp;
    
} MYDLPMF_MESSAGE, *PMYDLPMF_MESSAGE;

typedef struct _MYDLPMF_REPLY_MESSAGE {


	FILTER_REPLY_HEADER ReplyHeader;


	MYDLPMF_REPLY Reply;

} MYDLPMF_REPLY_MESSAGE, *PMYDLPMF_REPLY_MESSAGE;

typedef struct _MYDLPMF_CONF_REPLY_MESSAGE {


	FILTER_REPLY_HEADER ReplyHeader;


	MYDLPMF_CONF_REPLY Reply;

} MYDLPMF_CONF_REPLY_MESSAGE, *PMYDLPMF_CONF_REPLY_MESSAGE;


//
//  Default and Maximum number of threads.
//


#define MYDLPMF_DEFAULT_REQUEST_COUNT       5
#define MYDLPMF_DEFAULT_THREAD_COUNT        1
#define MYDLPMF_MAX_THREAD_COUNT            64

typedef struct _MYDLPMF_THREAD_CONTEXT {

	HANDLE Port;
	HANDLE Completion;

} MYDLPMF_THREAD_CONTEXT, *PMYDLPMF_THREAD_CONTEXT;

namespace MyDLPEP
{
	//Listener stops when MyDLPMF stops
	public ref class FilterListener			
	{

	private:
		static FilterListener ^filterInstance = nullptr;		
		
	public:
		static FilterListener ^getInstance();
		void StartListener(void);		
		
		FileOperation::Action HandleFileWrite(WCHAR *origPath, UCHAR * content, ULONG length);
		FileOperation::Action HandleFileOpen(WCHAR *origPath);
		void HandleFileCleanup(WCHAR *origPath);
	};
}

DWORD ListenerWorker( __in PMYDLPMF_THREAD_CONTEXT Context );
int InitializeListener(void);





