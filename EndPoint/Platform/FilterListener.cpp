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
#include "FilterListener.h"

using namespace System::Runtime::InteropServices;
using namespace MyDLP::EndPoint::Core;
using namespace System::Diagnostics;
using namespace System::ComponentModel;

namespace MyDLPEP
{	
	FilterListener^ FilterListener::getInstance()
	{
		if (filterInstance == nullptr)
			return gcnew FilterListener();
		else
			return filterInstance;
	}

	void FilterListener::StartListener()
	{
		DWORD threadId;
		try
		{
			CreateThread( NULL, 0,(LPTHREAD_START_ROUTINE) InitializeListener, NULL, 0, &threadId );
		}
		catch(char * str){
			MyDLP::EndPoint::Core::Logger::GetInstance()->Error(gcnew String(str));
		}
	}
	
	FileOperation::Action FilterListener::HandleFileWrite(WCHAR *origPath, UCHAR * content, ULONG length)
	{
		String ^filename = gcnew String(origPath);

		//System::Console::WriteLine("HandleFileWrite:" + filename);
		array< Byte >^ byteArray = gcnew array< Byte >(length);

		// convert native pointer to System::IntPtr with C-Style cast
		Marshal::Copy((IntPtr) content, byteArray, 0, length);

		/*for ( int i = byteArray->GetLowerBound(0); i <= byteArray->GetUpperBound(0); i++ ) {
			char dc =  *(Byte^)   byteArray->GetValue(i);
			Console::Write((Char)dc);
		}*/

		FileOperationController ^fopController = FileOperationController::GetInstance();
		return fopController->HandleWriteOperation(filename, byteArray, length);
	}

	FileOperation::Action FilterListener::HandleFileOpen(WCHAR *origPath)
	{
		String ^filename = gcnew String(origPath);
		//System::Console::WriteLine("HandleFileOpen Here:" + filename);
		FileOperationController ^fopController = FileOperationController::GetInstance();
		return fopController->HandleOpenOperation(filename);
	}

	void FilterListener::HandleFileCleanup(WCHAR *origPath)
	{
		String ^filename = gcnew String(origPath);
		//System::Console::WriteLine("HandleFileOpen Here:" + filename);
		FileOperationController ^fopController = FileOperationController::GetInstance();
		fopController->HandleCleanupOperation(filename);
	}
}


//Initializer thread, creates and waits for all ListenerWorker threads to terminate
int InitializeListener(void) 
{
	DWORD requestCount = MYDLPMF_DEFAULT_REQUEST_COUNT;
	WORD threadCount = MYDLPMF_DEFAULT_THREAD_COUNT;
	HANDLE threads[MYDLPMF_MAX_THREAD_COUNT];
	MYDLPMF_THREAD_CONTEXT context;
	HANDLE port, completion;
	PMYDLPMF_MESSAGE msg;
	DWORD threadId;
	HRESULT hr;
	DWORD i, j;

	hr = FilterConnectCommunicationPort(MYDLPMFPortName, 0, NULL, 0, NULL, &port);

	if (IS_ERROR(hr)) {
		MyDLP::EndPoint::Core::Logger::GetInstance()->Error( "ERROR: Connecting to filter port: " + hr );
		return 2;
	}

	completion = CreateIoCompletionPort( port, NULL, 0, threadCount );

	if (completion == NULL) {
		CloseHandle( port );
		return 3;
	}

	context.Port = port;
	context.Completion = completion;

	for (i = 0; i < threadCount; i++) {
		threads[i] = CreateThread( NULL, 0,(LPTHREAD_START_ROUTINE) ListenerWorker, &context, 0, &threadId );
		
		if (threads[i] == NULL) {
			hr = GetLastError();
			goto main_cleanup;
		}

		for (j = 0; j < requestCount; j++) {
#pragma prefast(suppress:__WARNING_MEMORY_LEAK, "msg will not be leaked because it is freed in MyDLPMFWorker")
			msg = (PMYDLPMF_MESSAGE) malloc(sizeof(MYDLPMF_MESSAGE));

			if (msg == NULL) {
				hr = ERROR_NOT_ENOUGH_MEMORY;
				goto main_cleanup;
			}

			memset(&msg->Ovlp, 0, sizeof(OVERLAPPED));
			hr = FilterGetMessage( port, &msg->MessageHeader, FIELD_OFFSET( MYDLPMF_MESSAGE, Ovlp ), &msg->Ovlp);

			if (hr != HRESULT_FROM_WIN32(ERROR_IO_PENDING)) {
				free(msg);
				goto main_cleanup;
			}
		}
	}

	hr = S_OK;

	WaitForMultipleObjectsEx(i, threads, TRUE, INFINITE, FALSE);

main_cleanup:
	MyDLP::EndPoint::Core::Logger::GetInstance()->Debug("MyDLPMF:  All done. Result:" + hr);
	CloseHandle(port);
	CloseHandle(completion);
	return hr;
}	

//Worker thread stops when MYDLPMF service stops
DWORD ListenerWorker(__in PMYDLPMF_THREAD_CONTEXT Context)
	{
		MyDLP::EndPoint::Core::Logger::GetInstance()->Info("Start Listener\n");
		PMYDLPMF_NOTIFICATION notification;
		MYDLPMF_REPLY_MESSAGE replyMessage;
		MYDLPMF_CONF_REPLY_MESSAGE confMessage;
		PMYDLPMF_MESSAGE message = NULL;
		LPOVERLAPPED pOvlp;
		MyDLPEP::FilterListener ^listener;
		BOOL result;
		DWORD outSize;
		HRESULT hr;
		ULONG_PTR key;
		unsigned int i = 0;
		BOOL init = 0;

	#pragma warning(push)
	#pragma warning(disable:4127) // conditional expression is constant

		while (TRUE) {
	#pragma warning(pop)
			result = GetQueuedCompletionStatus( Context->Completion, &outSize, &key, &pOvlp, INFINITE );
			message = CONTAINING_RECORD( pOvlp, MYDLPMF_MESSAGE, Ovlp );

			if (!result) {
				hr = HRESULT_FROM_WIN32( GetLastError() );
				if (hr == HRESULT_FROM_WIN32( ERROR_INVALID_HANDLE )) {
					MyDLP::EndPoint::Core::Logger::GetInstance()->Error("invalid handle here\n");
				}
				break;
			}

			notification = &message->Notification;
			FileOperation::Action action = FileOperation::Action::ALLOW;

			if(notification->Type == POSTCREATE) {
				listener = MyDLPEP::FilterListener::getInstance();
				action = listener->HandleFileOpen(notification->FileName);
			} else if (notification->Type == PREWRITE) {
				listener = MyDLPEP::FilterListener::getInstance();

				if (notification->BytesToScan > MYDLPMF_READ_BUFFER_SIZE)
				{
					MyDLP::EndPoint::Core::Logger::GetInstance()->Error("ListenerWorker error notification->BytesToScan > MYDLPMF_READ_BUFFER_SIZE");
				}
				action = listener->HandleFileWrite(notification->FileName, notification->Contents, notification->BytesToScan);
			} else if (notification->Type == PRECLEANUP) {
				listener = MyDLPEP::FilterListener::getInstance();
				listener->HandleFileCleanup(notification->FileName);
			} else if (notification->Type == INIT){
				init = 1;
			} /*else if (notification->Type == INSTANCEINIT)
			{
				MyDLP::EndPoint::Core::Logger::GetInstance()->Error("Instance Init!!!!");
				MyDLP::EndPoint::Core::Logger::GetInstance()->Error("Device Id:" + gcnew String(notification->FileName));
			}*/

			if (init == 0)
			{
				result = FALSE;
				replyMessage.ReplyHeader.Status = 0;
				replyMessage.ReplyHeader.MessageId = message->MessageHeader.MessageId;

				if (action == FileOperation::Action::ALLOW )
					replyMessage.Reply.Action = _MYDLPMF_REPLY::ActionType::ALLOW;
				else if(action == FileOperation::Action::BLOCK)
					replyMessage.Reply.Action = _MYDLPMF_REPLY::ActionType::BLOCK;
				else if(action == FileOperation::Action::NOACTION)
					replyMessage.Reply.Action = _MYDLPMF_REPLY::ActionType::NOACTION;

				/*if(!replyMessage.Reply.SafeToOpen)
					printf("***Replying message, SafeToOpen: %d\n", replyMessage.Reply.SafeToOpen );*/
			
				hr = FilterReplyMessage( Context->Port, (PFILTER_REPLY_HEADER) &replyMessage, sizeof( replyMessage ) );

				if (SUCCEEDED(hr) || hr == ERROR_FLT_NO_WAITER_FOR_REPLY ) {
					//printf("Replied message\n");

				} else {
					MyDLP::EndPoint::Core::Logger::GetInstance()->Error("MyDLPMF: Error replying message. Error:" + (gcnew Int64(hr)));
					break;
				}

				memset(&message->Ovlp, 0, sizeof( OVERLAPPED));

				hr = FilterGetMessage(Context->Port, &message->MessageHeader, FIELD_OFFSET( MYDLPMF_MESSAGE, Ovlp), &message->Ovlp);
				if (hr != HRESULT_FROM_WIN32( ERROR_IO_PENDING ))
				{
					break;
				}
			}
			else
			{
				init = 0;
				result = FALSE;
				confMessage.ReplyHeader.Status = 0;
				confMessage.ReplyHeader.MessageId = message->MessageHeader.MessageId;

				confMessage.Reply.Pid = MyDLP::EndPoint::Core::Configuration::ErlPid;
				hr = FilterReplyMessage( Context->Port, (PFILTER_REPLY_HEADER) &confMessage, sizeof( confMessage ) );

				if (SUCCEEDED(hr) || hr == ERROR_FLT_NO_WAITER_FOR_REPLY ){
					//printf("Replied message\n");

				} else {

					MyDLP::EndPoint::Core::Logger::GetInstance()->Error("MyDLPMF: Error replying message.Error:" + (gcnew Int64(hr)));
					break;
				}

				memset(&message->Ovlp, 0, sizeof( OVERLAPPED));

				hr = FilterGetMessage(Context->Port, &message->MessageHeader, FIELD_OFFSET( MYDLPMF_MESSAGE, Ovlp), &message->Ovlp);
				if (hr != HRESULT_FROM_WIN32( ERROR_IO_PENDING ))
				{
					break;
				}
			}
		}

		if (!SUCCEEDED(hr))	{
			if (hr == HRESULT_FROM_WIN32( ERROR_INVALID_HANDLE))
			{
				MyDLP::EndPoint::Core::Logger::GetInstance()->Error("MyDLPMF: Port is disconnected, probably due to scanner filter unloading.\n");
			} else {
				MyDLP::EndPoint::Core::Logger::GetInstance()->Error("MyDLPMF: Unknown error occured. Error:" + gcnew Int64(hr));
			}
		}

		free(message);
		return hr;
	}