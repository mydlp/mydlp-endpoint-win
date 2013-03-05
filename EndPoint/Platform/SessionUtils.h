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

#include "Windows.h"
#include "Ntsecapi.h"
#include "Sddl.h"
#include "stdio.h"
#include "Wtsapi32.h"

#define STATUS_SUCCESS 0

using namespace System;
using namespace MyDLP::EndPoint::Core;
using System::Collections::ArrayList;
using namespace System::Collections::Generic;

namespace MyDLPEP
{

	public ref class LogonSession
	{
	public:
		String^ upn;
		String^ name;
		String^ domain;
		String^ sid;
		int sessionId;
		Int64 logonTime;
		SECURITY_LOGON_TYPE type;


		LogonSession(const LogonSession^ iSession)
		{
			this->upn = iSession->upn;
			this->sid = iSession->sid;
			this->name = iSession->name;
			this->domain = iSession ->domain;
			this->sessionId = iSession->sessionId;
			this->logonTime = iSession->logonTime;
			this->type = iSession->type;
		}

		LogonSession()
		{
			this->upn = "";
			this->sid = "";
			this->name = "";
			this->domain = "";
			this->sessionId = 0;
			this->logonTime = 0;
			this->type = SECURITY_LOGON_TYPE::UndefinedLogonType;
		}
	};

	public ref class SessionUtils
	{

	public:
		static LogonSession^ GetSessionData(PLUID);
		static LogonSession^ GetActiveSession();
		static bool ImpersonateActiveUser();
		static bool StopImpersonation();
		static int GetPhysicalMemory();
		static List<int>^ EnumerateActiveSessionIds();
	};
}