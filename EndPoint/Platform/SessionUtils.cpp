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
	List<ActiveSession^>^ SessionUtils::EnumerateActiveSessionIds()
	{		
		List<ActiveSession^>^ activeList = gcnew List<ActiveSession^>();
		WTS_SESSION_INFO* ppSessionInfo = NULL;		
		DWORD count;
		BOOL bStatus; 
		DWORD dwLen;
		
		LPTSTR szUserName = NULL;
		LPTSTR szDomainName = NULL;


		bStatus = WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE,
			0,1,
			&ppSessionInfo,
			&count);

		if (bStatus)
		{
			for (int i = 0; i < count; i++)
			{
				if (ppSessionInfo[i].State == WTSActive)
				{
					ActiveSession^ session = gcnew ActiveSession();				
						
					session->sessionId = ppSessionInfo[i].SessionId;
					
					bStatus = WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE,
						session->sessionId,
						WTSDomainName,
						&szDomainName,
						&dwLen);

					if (bStatus)
					{
						session->domain = gcnew String(szDomainName);						
					}
					bStatus = WTSQuerySessionInformation(WTS_CURRENT_SERVER_HANDLE,
						session->sessionId,
						WTSUserName,
						&szUserName,
						&dwLen);
					if (bStatus)
					{
						session->name = gcnew String(szUserName);
					}
					activeList->Add(session);
				}
			}
		}
		if (ppSessionInfo != NULL)
		{
			WTSFreeMemory(ppSessionInfo);
		}
		
		if (szDomainName != NULL)
		{
			WTSFreeMemory(szDomainName);
		}
		
		if (szUserName != NULL)
		{
			WTSFreeMemory(szUserName);
		}

		return activeList;
	}

	
	bool SessionUtils::ImpersonateActiveUser()
	{
		if (System::Environment::UserInteractive)
			return true;

		List<ActiveSession^>^ activeList;
		activeList = EnumerateActiveSessionIds();

		if (activeList->Count < 1) 
		{
			return false;			
		}

	

		DWORD dwSessionId = (DWORD)(activeList[0])->sessionId;

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

	List<LogonSession^>^ SessionUtils::GetLogonSessions()
	{
		DWORD sessionId;
		ULONG sessionCount;
		PLUID sessionList;
		NTSTATUS retval;
		LogonSession^ session = nullptr;

		int i;
		List<LogonSession^>^ logonList = gcnew List<LogonSession^>();
		

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

			if (newSession->type != Interactive
				&& newSession->type != RemoteInteractive
				&& newSession->type != CachedInteractive
				&& newSession->type != CachedRemoteInteractive)
			{
				continue;
			}
			
			logonList->Add(newSession);
		}
		LsaFreeReturnBuffer(sessionList);
		return logonList;
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
			LocalFree(stringSid);
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
}