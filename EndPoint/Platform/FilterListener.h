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

using namespace System;
using namespace MyDLP::EndPoint::Core;

//
//  Name of port used to communicate
//
const PWSTR MYDLPMFPortName = L"\\MyDLPMFPort";


#define MYDLPMF_READ_BUFFER_SIZE   65536
#define MYDLPMF_FILENAME_BUFFER_SIZE 1024

typedef enum _MSG_TYPE        
{
	NONE,
	PREWRITE,
	POSTCREATE,
	PRECLEANUP
} MSG_TYPE;

/*typedef struct _MYDLPMF_NOTIFICATION {

    ULONG BytesToScan;
    ULONG FileNameLength;
    UCHAR Contents[MYDLPMF_READ_BUFFER_SIZE];
	WCHAR FileName[MYDLPMF_FILENAME_BUFFER_SIZE];	
	MSG_TYPE Type;
	ULONG Reserved;
    
} MYDLPMF_NOTIFICATION, *PMYDLPMF_NOTIFICATION;*/

typedef struct _MYDLPMF_NOTIFICATION {

	MSG_TYPE Type;
	WCHAR FileName[MYDLPMF_FILENAME_BUFFER_SIZE];	
	ULONG FileNameLength;
    ULONG BytesToScan;    
    UCHAR Contents[MYDLPMF_READ_BUFFER_SIZE];
    
} MYDLPMF_NOTIFICATION, *PMYDLPMF_NOTIFICATION;

typedef struct _MYDLPMF_MINI_NOTIFICATION {

	MSG_TYPE Type;
	WCHAR FileName[MYDLPMF_FILENAME_BUFFER_SIZE];	
	ULONG FileNameLength;
    ULONG BytesToScan; //just incase for alignment   
    UCHAR Contents[1];//just incase for alignment
    
} MYDLPMF_MINI_NOTIFICATION, *PMYDLPMF_MINI_NOTIFICATION;

typedef struct _MYDLPMF_REPLY {

	enum ActionType
	{
		ALLOW,
		BLOCK,
		NOACTION	
	} Action;
    
} MYDLPMF_REPLY, *PMYDLPMF_REPLY;


#pragma pack(1)
typedef struct _MYDLPMF_MESSAGE {

	//
	//  Required structure header.
	//

	FILTER_MESSAGE_HEADER MessageHeader;


	//
	//  Private scanner-specific fields begin here.
	//

	MYDLPMF_NOTIFICATION Notification;

	//
	//  Overlapped structure: this is not really part of the message
	//  However we embed it instead of using a separately allocated overlap structure
	//

	OVERLAPPED Ovlp;
    
} MYDLPMF_MESSAGE, *PMYDLPMF_MESSAGE;

typedef struct _MYDLPMF_REPLY_MESSAGE {

	//
	//  Required structure header.
	//

	FILTER_REPLY_HEADER ReplyHeader;

	//
	//  Private scanner-specific fields begin here.
	//

	MYDLPMF_REPLY Reply;

} MYDLPMF_REPLY_MESSAGE, *PMYDLPMF_REPLY_MESSAGE;

//
//  Default and Maximum number of threads.
//

#define MYDLPMF_DEFAULT_REQUEST_COUNT       5
#define MYDLPMF_DEFAULT_THREAD_COUNT        1
#define MYDLPMF_MAX_THREAD_COUNT            64

//UCHAR FoulString[] = "foul";

//
//  Context passed to worker threads
//

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





