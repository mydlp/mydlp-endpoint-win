/*    
This minifilter is based on Microsoft WDK Scanner Sample Code. 
Copyright (c) 1999-2002  Microsoft Corporation

This file is part of MyDLP. 
No copyright claimed on any part of Microsoft WDK Scanner Sample Code. 
Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
*/

#ifndef __MYDLPMF_H__
#define __MYDLPMF_H__


typedef struct _MYDLPMF_DATA {  

    PDRIVER_OBJECT DriverObject;
    PFLT_FILTER Filter;
    PFLT_PORT ServerPort;   
    PEPROCESS UserProcess; 
	PEPROCESS ErlangProcess;
    PFLT_PORT ClientPort;

} MYDLPMF_DATA, *PMYDLPMF_DATA;

extern MYDLPMF_DATA MyDLPMFData;

typedef struct _MYDLPMF_STREAM_HANDLE_CONTEXT {

    BOOLEAN RescanRequired;
    
} MYDLPMF_STREAM_HANDLE_CONTEXT, *PMYDLPMF_STREAM_HANDLE_CONTEXT;

#pragma warning(push)
#pragma warning(disable:4200) 
// disable warnings for structures with zero length arrays.

typedef struct _MYDLPMF_CREATE_PARAMS {

    WCHAR String[0];

} MYDLPMF_CREATE_PARAMS, *PMYDLPMF_CREATE_PARAMS;

#pragma warning(pop)

DRIVER_INITIALIZE DriverEntry;
NTSTATUS
DriverEntry (
    __in PDRIVER_OBJECT DriverObject,
    __in PUNICODE_STRING RegistryPath
    );

NTSTATUS
MyDLPMFUnload (
    __in FLT_FILTER_UNLOAD_FLAGS Flags
    );

NTSTATUS
MyDLPMFQueryTeardown (
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __in FLT_INSTANCE_QUERY_TEARDOWN_FLAGS Flags
    );

FLT_PREOP_CALLBACK_STATUS
MyDLPMFPreCreate (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __deref_out_opt PVOID *CompletionContext
    );

FLT_POSTOP_CALLBACK_STATUS
MyDLPMFPostCreate (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __in_opt PVOID CompletionContext,
    __in FLT_POST_OPERATION_FLAGS Flags
    );

FLT_PREOP_CALLBACK_STATUS
MyDLPMFPreCleanup (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __deref_out_opt PVOID *CompletionContext
    );

FLT_PREOP_CALLBACK_STATUS
MyDLPMFPreWrite (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __deref_out_opt PVOID *CompletionContext
    );

NTSTATUS
MyDLPMFInstanceSetup (
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __in FLT_INSTANCE_SETUP_FLAGS Flags,
    __in DEVICE_TYPE VolumeDeviceType,
    __in FLT_FILESYSTEM_TYPE VolumeFilesystemType
    );

int CheckRegularFile(WCHAR *path);
#endif /* __MYDLPMF_H__ */

