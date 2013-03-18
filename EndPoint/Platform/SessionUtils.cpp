// Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
//
//--------------------------------------------------------------------------
// This file is part of MyDLP.
//
// MyDLP is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyDLP is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with MyDLP. If not, see <http://www.gnu.org/licenses/>.
//--------------------------------------------------------------------------
#include "stdafx.h"
#include "SessionUtils.h"

using namespace System::ComponentModel;
using namespace System::Collections::Generic;

namespace MyDLPEP
{
	List<int>^ SessionUtils::EnumerateActiveSessionIds()
	{
		List<int>^ activeList = gcnew List<int>();
		WTS_SESSION_INFO* ppSessionInfo = NULL;
		DWORD count;
		BOOL retval; 
		retval = WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE,
			0,1,
			&ppSessionInfo,
			&count);

		if (retval)
		{
			for (int i = 0; i < count; i++)
			{
				if (ppSessionInfo[i].State == WTSActive)
				{
					activeList->Add(ppSessionInfo[i].SessionId);
				}
			}
		}
		if (ppSessionInfo != NULL)
		{
			WTSFreeMemory(ppSessionInfo);
		}

		return activeList;
	}

	LogonSession^ SessionUtils::GetActiveSession()
	{
		DWORD sessionId;
		ULONG sessionCount;
		PLUID sessionList;
		NTSTATUS retval;
		LogonSession^ session = nullptr;
		List<int>^ activeSessionIds;
		int i;

		activeSessionIds = EnumerateActiveSessionIds();

		if (activeSessionIds->Count == 0)
		{
			Logger::GetInstance()->Debug("EnumerateActiveSessionIds returns no id");
			return nullptr;
		}
		else if (activeSessionIds->Count > 1)
		{
			Logger::GetInstance()->Debug("EnumerateActiveSessionIds returns multiple id, using first one");
		}

		sessionId = activeSessionIds[0];

		Logger::GetInstance()->Debug("EnumerateActiveSessionIds[0] sessionId:" + sessionId);

		retval = LsaEnumerateLogonSessions( &sessionCount, &sessionList);
		if (retval != STATUS_SUCCESS)
		{
			Logger::GetInstance()->Error("LsaEnumerateLogonSessions failed:" + LsaNtStatusToWinError(retval));
			return nullptr;
		}

		for (i = 0;i < (int) sessionCount; i++) {
			LogonSession^ newSession = nullptr;
			newSession = GetSessionData (&sessionList[i]);

			if (newSession == nullptr)
			{
				Logger::GetInstance()->Error("New session data is empty, session id:" + i);
				continue;
			}

			if (newSession->sessionId != sessionId)
			{
				continue;
			}

			if (newSession->type != Interactive
				&& newSession->type != RemoteInteractive)
			{
				continue;
			}

			if (session == nullptr)
			{
				session = newSession;
			}
			else if (session->logonTime < newSession->logonTime)
			{
				Logger::GetInstance()->Debug("Session data is old setting new session");
				session = newSession;
			}
		}
		LsaFreeReturnBuffer(sessionList);
		return session;
	}


	LogonSession^ SessionUtils::GetSessionData(PLUID session)
	{
		PSECURITY_LOGON_SESSION_DATA sessionData = NULL;
		NTSTATUS retval;
		WCHAR buffer[256];
		WCHAR *usBuffer;
		int usLength;

		LogonSession^ iSession = gcnew LogonSession();

		if (!session )
		{
			Logger::GetInstance()->Error("Error - Invalid logon session identifier");
			return nullptr;
		}

		retval = LsaGetLogonSessionData (session, &sessionData);
		if (retval != STATUS_SUCCESS)
		{
			Logger::GetInstance()->Error("LsaGetLogonSessionData failed:" + LsaNtStatusToWinError(retval));
			if (sessionData) {
				LsaFreeReturnBuffer(sessionData);
			}
			return nullptr;
		}

		if (!sessionData)
		{
			Logger::GetInstance()->Error("Invalid session data");
			LsaFreeReturnBuffer(sessionData);
			return nullptr;
		}
		if (sessionData->Upn.Buffer != NULL) {

			usBuffer = (sessionData->Upn).Buffer;
			usLength = (sessionData->Upn).Length;

			if(usLength < 256)
			{
				wcsncpy_s(buffer, 256, usBuffer, usLength);
				wcscat_s(buffer, 256, L"");
				iSession->upn = gcnew String(buffer);
			}
			else
			{
				Logger::GetInstance()->Error("Upn too long");
				LsaFreeReturnBuffer(sessionData);
				iSession->upn = gcnew String("");
			}
		}
		else
		{
			LsaFreeReturnBuffer(sessionData);
			iSession->upn = gcnew String("");
		}

		if (sessionData->UserName.Buffer != NULL) {

			usBuffer = (sessionData->UserName).Buffer;
			usLength = (sessionData->UserName).Length;

			if((sessionData->UserName).Length < 256)
			{
				wcsncpy_s(buffer, 256, usBuffer, usLength);
				wcscat_s(buffer, 256, L"");
				iSession->name = gcnew String(buffer);
			}
			else
			{
				Logger::GetInstance()->Error("name too long");
				LsaFreeReturnBuffer(sessionData);
				iSession->name = gcnew String("");
			}

		} else
		{
			LsaFreeReturnBuffer(sessionData);
			iSession->name = gcnew String("");
		}


		if (sessionData->LogonDomain.Buffer != NULL)
		{
			usBuffer = (sessionData->LogonDomain).Buffer;
			usLength = (sessionData->LogonDomain).Length;

			if((sessionData->UserName).Length < 256)
			{
				wcsncpy_s(buffer, 256, usBuffer, usLength);
				wcscat_s(buffer, 256, L"");
				iSession->domain = gcnew String(buffer);
			}
			else
			{
				Logger::GetInstance()->Error("domain too long");
				LsaFreeReturnBuffer(sessionData);
				iSession->domain = gcnew String("");
			}
		}
		else
		{
			LsaFreeReturnBuffer(sessionData);
			iSession->domain = gcnew String("");
		}

		LPTSTR stringSid;
		if (ConvertSidToStringSid(sessionData->Sid, &stringSid))
		{
			iSession->sid = gcnew String(stringSid);
		}
		else
		{
			iSession->sid = gcnew String("");
			//Logger::GetInstance()->Error("SID error");
		}

		iSession->sessionId = sessionData->Session;
		iSession->logonTime = sessionData->LogonTime.QuadPart;
		iSession->type = (SECURITY_LOGON_TYPE) sessionData->LogonType;
		LsaFreeReturnBuffer(sessionData);
		return iSession;
	}


	bool SessionUtils::ImpersonateActiveUser()
	{
		if (System::Environment::UserInteractive)
			return true;

		LogonSession^ session = GetActiveSession();
		DWORD dwSessionId = (DWORD)session->sessionId;

		HANDLE hTokenNew = NULL, hTokenDup = NULL;

		if (!WTSQueryUserToken(dwSessionId, &hTokenNew))
		{
			Logger::GetInstance()->Error("WTSQueryUserToken failed" + (gcnew Win32Exception(GetLastError()))->Message );
			return false;
		}

		DuplicateTokenEx(hTokenNew,MAXIMUM_ALLOWED,NULL,SecurityIdentification,TokenPrimary,&hTokenDup);
		if (!ImpersonateLoggedOnUser (hTokenDup))
		{
			Logger::GetInstance()->Error("ImpersonateLoggedOnUser failed" + (gcnew Win32Exception(GetLastError()))->Message );
			return false;
		}

		return true;
	}


	bool SessionUtils::StopImpersonation()
	{
		if (System::Environment::UserInteractive)
			return true;

		return RevertToSelf();
	}


	int SessionUtils::GetPhysicalMemory()
	{
		MEMORYSTATUSEX statex;
		statex.dwLength = sizeof (statex);

		GlobalMemoryStatusEx (&statex);

		return statex.ullTotalPhys / (1024 * 1024);

	}
}