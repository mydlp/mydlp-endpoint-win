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

namespace MyDLPEP
{

	/*int SessionUtils::GetActiveSessionId()
	{
	DWORD sessionId;

	sessionId = WTSGetActiveConsoleSessionId();

	return sessionId;

	}*/

	InteractiveSession^ SessionUtils::GetActiveSession()
	{
		DWORD sessionId;
		ULONG sessionCount;
		PLUID sessionList;
		NTSTATUS retval;
		InteractiveSession^ session = nullptr;
		int i;

		sessionId = WTSGetActiveConsoleSessionId();

		Logger::GetInstance()->Debug("WTSGetActiveConsoleSessionId sessionId:" + sessionId);

		retval = LsaEnumerateLogonSessions( &sessionCount, &sessionList);
		if (retval != STATUS_SUCCESS)
		{
			Logger::GetInstance()->Error("LsaEnumerateLogonSessions failed:" + LsaNtStatusToWinError(retval));
			return nullptr;
		}


		for (i = 0;i < (int) sessionCount; i++) {

			InteractiveSession^ newSession = nullptr;
			newSession = GetSessionData (&sessionList[i]);

			if (newSession == nullptr)
			{
				//Logger::GetInstance()->Error("New session data is empty, session id:" + i);
				continue;
			}

			if (newSession->sessionId != sessionId)
			{
				continue;
			}

			if (session == nullptr)
			{
				session = newSession;
			}
			else if (session->logonTime < newSession->logonTime)
			{
				//Logger::GetInstance()->Debug("Session data is old setting new session");
				session = newSession;
			}
		}
		LsaFreeReturnBuffer(sessionList);
		return session;
	}


	/*ArrayList^ SessionUtils::EnumerateLogonSessions()
	{
	ULONG sessionCount;
	PLUID sessionList;
	NTSTATUS retval;
	int i, j, listCount;
	ArrayList^ list = gcnew ArrayList();

	Logger::GetInstance()->Debug("EnumerateSessionData");

	retval = LsaEnumerateLogonSessions( &sessionCount, &sessionList);
	if (retval != STATUS_SUCCESS)
	{
	Logger::GetInstance()->Error("LsaEnumerateLogonSessions failed :" + LsaNtStatusToWinError(retval));
	return list;
	}

	for (i = 0;i < (int) sessionCount; i++) {

	InteractiveSession^ newSession;
	newSession = GetSessionData (&sessionList[i]);
	if (newSession == nullptr) continue;

	listCount = list->Count;

	bool sessionCollision = false;

	for (j = 0; j < listCount; j++)
	{
	InteractiveSession ^ session = (InteractiveSession ^) list[j];
	if (newSession->sessionId == session->sessionId)
	{
	sessionCollision = true;
	if(newSession->logonTime > session->logonTime)
	{
	list->RemoveAt(j);
	j--;
	list->Add(newSession);
	}
	}
	}

	if (!sessionCollision)
	{
	list->Add(newSession);
	}
	}

	LsaFreeReturnBuffer(sessionList);
	return list;
	}
	*/


	InteractiveSession^ SessionUtils::GetSessionData(PLUID session)
	{
		PSECURITY_LOGON_SESSION_DATA sessionData = NULL;
		NTSTATUS retval;
		WCHAR buffer[256];
		WCHAR *usBuffer;
		int usLength;

		InteractiveSession^ iSession = gcnew InteractiveSession();

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

		/*if ((SECURITY_LOGON_TYPE) sessionData->LogonType != Interactive)
		{
		Logger::GetInstance()->Debug("Logon type non interactive");
		LsaFreeReturnBuffer(sessionData);
		return nullptr;
		}*/

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
				return nullptr;
			}

		}
		else
		{
			Logger::GetInstance()->Error("Missing upn");
			LsaFreeReturnBuffer(sessionData);
			return nullptr;
		}

		if (sessionData->UserName.Buffer != NULL) {

			usBuffer = (sessionData->UserName).Buffer;
			usLength = (sessionData->UserName).Length;

			if((sessionData->UserName).Length < 256)
			{
				wcsncpy_s(buffer, 256, usBuffer, usLength);
				wcscat_s(buffer, 256, L"");
				iSession->name = gcnew String(buffer);
				//Logger::GetInstance()->Debug(iSession->name);
			}
			else
			{
				Logger::GetInstance()->Error("name too long");
				LsaFreeReturnBuffer(sessionData);
				return nullptr;
			}

		} else
		{
			Logger::GetInstance()->Error("Missing name");
			LsaFreeReturnBuffer(sessionData);
			return nullptr;
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
				//Logger::GetInstance()->Debug(iSession->domain);
			}
			else
			{
				Logger::GetInstance()->Error("domain too long");
				LsaFreeReturnBuffer(sessionData);
				return nullptr;
			}
		}
		else
		{
			Logger::GetInstance()->Debug("Missing domain");
			LsaFreeReturnBuffer(sessionData);
			return nullptr;
		}

		if (sessionData->AuthenticationPackage.Buffer != NULL) {
			// Get the authentication package name.
			usBuffer = (sessionData->AuthenticationPackage).Buffer;
			usLength = (sessionData->AuthenticationPackage).Length;
			if(usLength < 256)
			{
				wcsncpy_s (buffer, 256, usBuffer, usLength);
				wcscat_s (buffer, 256, L"");
				//Logger::GetInstance()->Debug(gcnew String(buffer));
			}
			else
			{
				Logger::GetInstance()->Error("Authentication package too long for buffer");
				LsaFreeReturnBuffer(sessionData);
				return nullptr;

			}
		}
		else
		{
			Logger::GetInstance()->Error("Missing authentication package.");
			LsaFreeReturnBuffer(sessionData);
			return nullptr;
		}

		LPTSTR stringSid;

		if (ConvertSidToStringSid(sessionData->Sid, &stringSid))
		{
			iSession->sid = gcnew String(stringSid);
		}
		else
		{
			//Logger::GetInstance()->Error("SID error");
			LocalFree(stringSid);
			return nullptr;
		}

		LocalFree(stringSid);

		iSession->sessionId = sessionData->Session;
		//Logger::GetInstance()->Debug("Session ID:" + iSession->sessionId);

		iSession->logonTime = sessionData->LogonTime.QuadPart;
		//Logger::GetInstance()->Debug("LogonTime" + iSession->logonTime);

		LsaFreeReturnBuffer(sessionData);
		return iSession;
	}

	bool SessionUtils::ImpersonateActiveUser()
	{
		DWORD dwSessionId = WTSGetActiveConsoleSessionId();

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
		return RevertToSelf();
	}
}