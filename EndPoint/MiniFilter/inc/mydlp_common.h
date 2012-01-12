/*    
This minifilter is based on Microsoft WDK Scanner Sample Code. 
Copyright (c) 1999-2002  Microsoft Corporation

This file is part of MyDLP. 
No copyright claimed on any part of Microsoft WDK Scanner Sample Code. 
Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
*/


//Definitions used between user space(platform) and kernel(minifilter)
#ifndef __MYDLP_COMMON_H__
#define __MYDLP_COMMON_H__

const PWSTR MyDLPMFPortName = L"\\MyDLPMFPort";

#define MYDLPMF_READ_BUFFER_SIZE   65536
#define MYDLPMF_FILENAME_BUFFER_SIZE 1024

//Notification message types
typedef enum _MSG_TYPE        
{
	NONE,
	PREWRITE,
	POSTCREATE,
	PRECLEANUP,
	CONF,
	USBSAC
} MSG_TYPE;

//write notifcation for PREWRITE
typedef struct _MYDLPMF_WRITE_NOTIFICATION {

	MSG_TYPE Type;
	WCHAR FileName[MYDLPMF_FILENAME_BUFFER_SIZE];	
	ULONG FileNameLength;
    ULONG BytesToScan;    
    UCHAR Contents[MYDLPMF_READ_BUFFER_SIZE];
    
} MYDLPMF_WRITE_NOTIFICATION, *PMYDLPMF_WRITE_NOTIFICATION;

//file access notification for PRECREATE(only when USBSAC enabled)
//PRECLEANUP, POSTCREATE(only when Archive inbound enabled)
typedef struct _MYDLPMF_FILE_NOTIFICATION {

	MSG_TYPE Type;
	WCHAR FileName[MYDLPMF_FILENAME_BUFFER_SIZE];	
	ULONG FileNameLength; 
    
} MYDLPMF_FILE_NOTIFICATION, *PMYDLPMF_FILE_NOTIFICATION;

//basic notification and for CONFIGURATION update
typedef struct _MYDLPMF_NOTIFICATION {

	MSG_TYPE Type;

} MYDLPMF_NOTIFICATION, *PMYDLPMF_NOTIFICATION;

typedef struct _MYDLPMF_REPLY {

	BOOLEAN ConfUpdate;
	enum ActionType
	{
		ALLOW,
		BLOCK,
		NOACTION	
	} Action;
    
} MYDLPMF_REPLY, *PMYDLPMF_REPLY;

typedef struct _MYDLPMF_CONF_REPLY {

	int Pid;
	BOOLEAN ArchiveInbound;
	BOOLEAN USBSerialAC;
    
} MYDLPMF_CONF_REPLY, *PMYDLPMF_CONF_REPLY;

#endif __MYDLP_COMMON_H__

