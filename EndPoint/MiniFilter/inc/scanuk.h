/*    
This minifilter is based on Microsoft WDK Scanner Sample Code. 
Copyright (c) 1999-2002  Microsoft Corporation

This file is part of MyDLP. 
No copyright claimed on any part of Microsoft WDK Scanner Sample Code. 
Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
*/

#ifndef __SCANUK_H__
#define __SCANUK_H__

const PWSTR MyDLPMFPortName = L"\\MyDLPMFPort";


#define MYDLPMF_READ_BUFFER_SIZE   65536
#define MYDLPMF_FILENAME_BUFFER_SIZE 1024

typedef enum _MSG_TYPE        
{
	NONE,
	PREWRITE,
	POSTCREATE,
	PRECLEANUP
} MSG_TYPE;


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


#endif //  __SCANUK_H__


