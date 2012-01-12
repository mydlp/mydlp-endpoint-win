/*    
This minifilter is based on Microsoft WDK Scanner Sample Code. 
Copyright (c) 1999-2002  Microsoft Corporation

This file is part of MyDLP. 
No copyright claimed on any part of Microsoft WDK Scanner Sample Code. 
Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
*/

#pragma region Definitions

#include <fltKernel.h>
#include <dontuse.h>
#include <suppress.h>
#include <Ntddstor.h>
#include "mydlp_common.h"
#include "mydlp_minifilter.h"
#define DBG_PRINT

#pragma prefast(disable:__WARNING_ENCODE_MEMBER_FUNCTION_POINTER, "Not valid for kernel mode drivers")

//Filter runtime configuration
MYDLPMF_CONF FilterConf;

//IO buffers used between filter and userspace
PMYDLPMF_WRITE_NOTIFICATION writeNotification = NULL;
PMYDLPMF_FILE_NOTIFICATION fileNotification = NULL;
PMYDLPMF_NOTIFICATION notification = NULL;
PMYDLPMF_REPLY reply = NULL;
PMYDLPMF_CONF_REPLY confReply = NULL;
BOOLEAN Configured = FALSE;

NTSTATUS
MyDLPMFPortConnect (
    __in PFLT_PORT ClientPort,
    __in_opt PVOID ServerPortCookie,
    __in_bcount_opt(SizeOfContext) PVOID ConnectionContext,
    __in ULONG SizeOfContext,
    __deref_out_opt PVOID *ConnectionCookie
    );

VOID
MyDLPMFPortDisconnect (
    __in_opt PVOID ConnectionCookie
    );

NTSTATUS
MyDLPMFpScanFileInUserMode (
    __in PFLT_INSTANCE Instance,
    __in PFILE_OBJECT FileObject,
    __out PBOOLEAN SafeToOpen
    );

#ifdef ALLOC_PRAGMA
    #pragma alloc_text(INIT, DriverEntry)
    #pragma alloc_text(PAGE, MyDLPMFInstanceSetup)
    #pragma alloc_text(PAGE, MyDLPMFConfigurationUpdate)
    #pragma alloc_text(PAGE, MyDLPMFPreCreate)
    #pragma alloc_text(PAGE, MyDLPMFPreWrite)
    #pragma alloc_text(PAGE, MyDLPMFPostCreate)
    #pragma alloc_text(PAGE, MyDLPMFPreCleanup)
    #pragma alloc_text(PAGE, MyDLPMFPortConnect)
    #pragma alloc_text(PAGE, MyDLPMFPortDisconnect)
#endif


const FLT_OPERATION_REGISTRATION Callbacks[] = {

    { IRP_MJ_CREATE,
      FLTFL_OPERATION_REGISTRATION_SKIP_PAGING_IO,
      MyDLPMFPreCreate,
      MyDLPMFPostCreate},

    { IRP_MJ_CLEANUP,
      0,
      MyDLPMFPreCleanup,
      NULL},

    { IRP_MJ_WRITE,
      FLTFL_OPERATION_REGISTRATION_SKIP_PAGING_IO,
      MyDLPMFPreWrite,
      NULL},

    { IRP_MJ_OPERATION_END}
};


const FLT_CONTEXT_REGISTRATION ContextRegistration[] = {

    { FLT_STREAMHANDLE_CONTEXT,
      0,
      NULL,
      sizeof(MYDLPMF_STREAM_HANDLE_CONTEXT),
      'chBS' },

    { FLT_CONTEXT_END }
};

const FLT_REGISTRATION FilterRegistration = {

    sizeof( FLT_REGISTRATION ),         //  Size
    FLT_REGISTRATION_VERSION,           //  Version
    0,                                  //  Flags
    ContextRegistration,                //  Context Registration.
    Callbacks,                          //  Operation callbacks
    MyDLPMFUnload,                      //  FilterUnload
    MyDLPMFInstanceSetup,               //  InstanceSetup
    MyDLPMFQueryTeardown,               //  InstanceQueryTeardown
    NULL,                               //  InstanceTeardownStart
    NULL,                               //  InstanceTeardownComplete
    NULL,                               //  GenerateFileName
    NULL,                               //  GenerateDestinationFileName
    NULL                                //  NormalizeNameComponent
};
#pragma endregion 

#pragma region DriverEntry
NTSTATUS
DriverEntry (
    __in PDRIVER_OBJECT DriverObject,
    __in PUNICODE_STRING RegistryPath
    )
{
    OBJECT_ATTRIBUTES oa;
    UNICODE_STRING uniString;
    PSECURITY_DESCRIPTOR sd;
    NTSTATUS status;

    UNREFERENCED_PARAMETER( RegistryPath );
	
	//Timeouts hardcoded for now
	FilterConf.ReadTimeout.QuadPart = (LONGLONG) - 10 * 1000 * 1000;  //1 seconds
	FilterConf.WriteTimeout.QuadPart = (LONGLONG) - 50 * 1000 * 1000;  //5 seconds


#ifdef DBG_PRINT
	DbgPrint("In DriverEntry");
#endif

    status = FltRegisterFilter( DriverObject,
                                &FilterRegistration,
                                &FilterConf.Filter );


    if (!NT_SUCCESS( status )) {

        return status;
    }

    RtlInitUnicodeString( &uniString, MyDLPMFPortName );

    status = FltBuildDefaultSecurityDescriptor( &sd, FLT_PORT_ALL_ACCESS );

    if (NT_SUCCESS( status )) {

        InitializeObjectAttributes( &oa,
                                    &uniString,
                                    OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE,
                                    NULL,
                                    sd );

        status = FltCreateCommunicationPort( FilterConf.Filter,
                                             &FilterConf.ServerPort,
                                             &oa,
                                             NULL,
                                             MyDLPMFPortConnect,
                                             MyDLPMFPortDisconnect,
                                             NULL,
                                             1 );
    
        FltFreeSecurityDescriptor( sd );

        if (NT_SUCCESS( status )) {

			writeNotification = ExAllocatePoolWithTag( NonPagedPool,
                                          sizeof( MYDLPMF_WRITE_NOTIFICATION ),
                                          'mdlp' );
			if (writeNotification == NULL)
			{
#ifdef DBG_PRINT
				DbgPrint("ExAllocatePoolWithTag failed for writeNotification!!!");
#endif
				FltCloseCommunicationPort( FilterConf.ServerPort );
				FltUnregisterFilter( FilterConf.Filter );
				return status;
            }
			
			fileNotification = ExAllocatePoolWithTag( NonPagedPool,
                                          sizeof( MYDLPMF_FILE_NOTIFICATION ),
                                          'mdlp' );
			if (fileNotification == NULL)
			{
#ifdef DBG_PRINT
				DbgPrint("ExAllocatePoolWithTag failed for fileNotification!!!");
#endif
				FltCloseCommunicationPort( FilterConf.ServerPort );
				FltUnregisterFilter( FilterConf.Filter );
				return status;
            }

			notification = ExAllocatePoolWithTag( NonPagedPool,
                                          sizeof( MYDLPMF_NOTIFICATION ),
                                          'mdlp' );
			if (notification == NULL)
			{
#ifdef DBG_PRINT
				DbgPrint("ExAllocatePoolWithTag failed for notification!!!");
#endif
				FltCloseCommunicationPort( FilterConf.ServerPort );
				FltUnregisterFilter( FilterConf.Filter );
				return status;
            }

#ifdef DBG_PRINT
				DbgPrint("ExAllocatePoolWithTag success for notification!!!");
#endif
			reply = ExAllocatePoolWithTag( NonPagedPool,
                                          sizeof( MYDLPMF_REPLY ) + sizeof (FILTER_REPLY_HEADER),
                                          'mdlp' );
			if (reply == NULL)
			{
#ifdef DBG_PRINT
				DbgPrint("ExAllocatePoolWithTag failed for reply!!!");
#endif
				FltCloseCommunicationPort( FilterConf.ServerPort );
				FltUnregisterFilter( FilterConf.Filter );
				return status;
            }


			confReply = ExAllocatePoolWithTag( NonPagedPool,
                                          sizeof( MYDLPMF_CONF_REPLY ) + sizeof (FILTER_REPLY_HEADER),
                                          'mdlp' );
			if (confReply == NULL)
			{
#ifdef DBG_PRINT
				DbgPrint("ExAllocatePoolWithTag failed for confReply!!!");
#endif
				FltCloseCommunicationPort( FilterConf.ServerPort );
				FltUnregisterFilter( FilterConf.Filter );
				return status;
            }        
        
            status = FltStartFiltering( FilterConf.Filter );

            if (NT_SUCCESS( status )) {

				return STATUS_SUCCESS;
			}	
			
			FltCloseCommunicationPort( FilterConf.ServerPort );
		}			
    }

    FltUnregisterFilter( FilterConf.Filter );

    return status;
}
#pragma endregion 

#pragma region PortConnect
NTSTATUS
MyDLPMFPortConnect (
    __in PFLT_PORT ClientPort,
    __in_opt PVOID ServerPortCookie,
    __in_bcount_opt(SizeOfContext) PVOID ConnectionContext,
    __in ULONG SizeOfContext,
    __deref_out_opt PVOID *ConnectionCookie
    )
{
    PAGED_CODE();

    UNREFERENCED_PARAMETER( ServerPortCookie );
    UNREFERENCED_PARAMETER( ConnectionContext );
    UNREFERENCED_PARAMETER( SizeOfContext);
    UNREFERENCED_PARAMETER( ConnectionCookie );

    ASSERT( FilterConf.ClientPort == NULL );
    ASSERT( FilterConf.UserProcess == NULL );
	ASSERT( FilterConf.ErlangProcess == NULL );

    FilterConf.UserProcess = PsGetCurrentProcess();
    FilterConf.ClientPort = ClientPort;

#ifdef DBG_PRINT
    DbgPrint( "mydlpmf connected, port=0x%p\n", ClientPort );
#endif
    return STATUS_SUCCESS;
}
#pragma endregion 

#pragma region PortDisconnect
VOID
MyDLPMFPortDisconnect(
     __in_opt PVOID ConnectionCookie
     )
{
    UNREFERENCED_PARAMETER( ConnectionCookie );

    PAGED_CODE();
#ifdef DBG_PRINT
    DbgPrint( "mydlpmf disconnected, port=0x%p\n", FilterConf.ClientPort );
#endif
    FltCloseClientPort( FilterConf.Filter, &FilterConf.ClientPort );

    FilterConf.UserProcess = NULL;
	FilterConf.ErlangProcess = NULL;
}
#pragma endregion 

#pragma region Unload
NTSTATUS
MyDLPMFUnload (
    __in FLT_FILTER_UNLOAD_FLAGS Flags
    )
{
    UNREFERENCED_PARAMETER( Flags );
#ifdef DBG_PRINT
	DbgPrint( "mydlpmf unload" );
#endif
    FltCloseCommunicationPort( FilterConf.ServerPort );
	FltUnregisterFilter( FilterConf.Filter );

	if (writeNotification != NULL) {
        ExFreePoolWithTag( writeNotification, 'mdlp' );			
    }
	
	if (fileNotification != NULL) {
		ExFreePoolWithTag( fileNotification, 'mdlp' );
	}

		
	if (notification != NULL) {
		ExFreePoolWithTag( notification, 'mdlp' );
	}

		
	if (reply != NULL) {
		ExFreePoolWithTag( reply, 'mdlp' );
	}

		
	if (confReply != NULL) {
		ExFreePoolWithTag( confReply, 'mdlp' );
	}

    return STATUS_SUCCESS;
}
#pragma endregion 

#pragma region Setup
NTSTATUS
MyDLPMFInstanceSetup (
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __in FLT_INSTANCE_SETUP_FLAGS Flags,
    __in DEVICE_TYPE VolumeDeviceType,
    __in FLT_FILESYSTEM_TYPE VolumeFilesystemType
    )
{
	NTSTATUS status;
	PFLT_VOLUME_PROPERTIES VolumeProperties;
	ULONG returnedLength;

	PIRP NewIrp;
	STORAGE_PROPERTY_QUERY Query;
	PSTORAGE_DEVICE_DESCRIPTOR Descriptor = NULL;
	KEVENT WaitEvent;
	NTSTATUS Status;
	IO_STATUS_BLOCK IoStatus;
	PDEVICE_OBJECT DeviceObject;
	UNICODE_STRING volumeDosName = {0}; 
	int replyLength;

	UNREFERENCED_PARAMETER( FltObjects );
    UNREFERENCED_PARAMETER( Flags );
    UNREFERENCED_PARAMETER( VolumeFilesystemType );    

    PAGED_CODE();

#ifdef DBG_PRINT
    DbgPrint( "mydlpmf instance setup" );
#endif


    ASSERT( FltObjects->Filter == FilterConf.Filter );

	Query.PropertyId = StorageDeviceProperty;
	Query.QueryType = PropertyStandardQuery;
	KeInitializeEvent(&WaitEvent, NotificationEvent, FALSE);
	Descriptor = ExAllocatePoolWithTag(NonPagedPool,
			sizeof(STORAGE_DEVICE_DESCRIPTOR)+512, 'mdlp');

	Status = FltGetDiskDeviceObject( FltObjects->Volume, &DeviceObject );   

	if(NT_SUCCESS(Status) && Descriptor != NULL)
	{
		NewIrp = IoBuildDeviceIoControlRequest(IOCTL_STORAGE_QUERY_PROPERTY,
			DeviceObject,
			(PVOID)&Query, sizeof(STORAGE_PROPERTY_QUERY),
			(PVOID)Descriptor, sizeof(STORAGE_DEVICE_DESCRIPTOR)+512,
			FALSE, &WaitEvent, &IoStatus);

		if (!NewIrp)
		{
#ifdef DBG_PRINT
			DbgPrint("Failed to create new IRP, IOCTL_STORAGE_QUERY_PROPERTY");
#endif
			ExFreePoolWithTag(Descriptor, 'mdlp');
			return STATUS_FLT_DO_NOT_ATTACH;
		}

		Status = IoCallDriver(DeviceObject, NewIrp);

		
		if (Status == STATUS_PENDING)
		{
			Status = KeWaitForSingleObject(&WaitEvent, Executive, KernelMode, FALSE, NULL);
			Status = IoStatus.Status;
		}

		if (!NT_SUCCESS(Status))
		{
#ifdef DBG_PRINT
			DbgPrint("Query IOCTL_STORAGE_QUERY_PROPERTY failed, status =0x%x", Status);
#endif
			ExFreePoolWithTag(Descriptor, 'mdlp');
			return STATUS_FLT_DO_NOT_ATTACH;
		}

		if(Descriptor->BusType != BusTypeUsb && Descriptor->BusType != BusType1394)
		{
#ifdef DBG_PRINT
			DbgPrint("Device is not USB or 1394");
#endif
			ExFreePoolWithTag(Descriptor, 'mdlp');
			return STATUS_FLT_DO_NOT_ATTACH;
		}

		ExFreePoolWithTag(Descriptor, 'mdlp');
	}
	else
	{
		if(Descriptor != NULL)
			ExFreePoolWithTag(Descriptor, 'mdlp');

		return STATUS_FLT_DO_NOT_ATTACH;
	}

#ifdef DBG_PRINT
    DbgPrint( "mydlpmf end setup" );
#endif
    return STATUS_SUCCESS;
}
#pragma endregion

#pragma region ConfigurationUpdate
void
MyDLPMFConfigurationUpdate ()
{
	int replyLength;
	NTSTATUS status;
	PAGED_CODE();

#ifdef DBG_PRINT
	DbgPrint("In ConfigurationUpdate");
#endif

	notification->Type = CONF;
	replyLength = sizeof( FILTER_REPLY_HEADER ) + sizeof(MYDLPMF_CONF_REPLY);
	status = FltSendMessage( FilterConf.Filter,
		&FilterConf.ClientPort,
		notification,
		sizeof( MYDLPMF_NOTIFICATION ),
		confReply,
		&replyLength,
		&FilterConf.ReadTimeout );	

	if (STATUS_SUCCESS == status) {
		HANDLE pid = confReply->Pid;
		status = PsLookupProcessByProcessId(pid, &FilterConf.ErlangProcess); 
		FilterConf.ArchiveInbound = confReply->ArchiveInbound;
		FilterConf.USBSerialAC = confReply->USBSerialAC;
		if (STATUS_SUCCESS == status) {
#ifdef DBG_PRINT
			DbgPrint("Initialized minifilter runtime conf pid: %d", pid);
			DbgPrint("Initialized minifilter USBSerialAC: %d", FilterConf.USBSerialAC);
			DbgPrint("Initialized minifilter ArchiveInbound: %d", FilterConf.ArchiveInbound);
#endif	
			Configured = TRUE;
		}
		else
		{
#ifdef DBG_PRINT
			DbgPrint("Configuration update failed");
#endif
		}
	}
	else
	{
#ifdef DBG_PRINT
		DbgPrint(" FltSendMessage failed status 0x%X\n", status);
#endif
	}
}

#pragma endregion

#pragma region QueryTeardown 
NTSTATUS
MyDLPMFQueryTeardown (
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __in FLT_INSTANCE_QUERY_TEARDOWN_FLAGS Flags
    )
{
    UNREFERENCED_PARAMETER( FltObjects );
    UNREFERENCED_PARAMETER( Flags );

    return STATUS_SUCCESS;
}
#pragma endregion  

#pragma region PreCreate
FLT_PREOP_CALLBACK_STATUS
MyDLPMFPreCreate (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __deref_out_opt PVOID *CompletionContext
    )
{
	POBJECT_NAME_INFORMATION dosNameInfo = NULL;
	NTSTATUS status = STATUS_SUCCESS;
	ULONG replyLength;
	enum ActionType action;

	UNREFERENCED_PARAMETER( FltObjects );
    UNREFERENCED_PARAMETER( CompletionContext );

    PAGED_CODE();

	if (!Configured)
	{
		MyDLPMFConfigurationUpdate();
	}

	//check if accesing userspace process is MyDLP EP or MyDLP Engine
	if (IoThreadToProcess( Data->Thread ) == FilterConf.UserProcess || IoThreadToProcess( Data->Thread ) == FilterConf.ErlangProcess) {
		
		//and leave them alone forever
        return FLT_PREOP_SUCCESS_NO_CALLBACK;
    }

	//check if USBSerialAC is enabled
	//We do not need to analyze a prcreate if USBSerialAC is not enabled
	if (FilterConf.USBSerialAC){
		notification->Type = USBSAC;
		replyLength = sizeof (FILTER_REPLY_HEADER) + sizeof( MYDLPMF_REPLY );

		status = FltSendMessage( FilterConf.Filter,
				&FilterConf.ClientPort,
				notification,
				sizeof( MYDLPMF_NOTIFICATION ),
				reply,
				&replyLength,
				&FilterConf.ReadTimeout);

		if (STATUS_TIMEOUT == status) {
#ifdef DBG_PRINT
			DbgPrint("Usb block failed");
#endif
		}

		action = reply->Action;

		if (action == BLOCK)
		{
	#ifdef DBG_PRINT
			//	DbgPrint("Precreate block");
	#endif
			//disables any read write for un authorized USB device
			Data->IoStatus.Status = STATUS_ACCESS_DENIED;
			Data->IoStatus.Information = 0;
			return FLT_PREOP_COMPLETE;
		}
	}   
    return FLT_PREOP_SUCCESS_WITH_CALLBACK;
}
#pragma endregion

#pragma region PostCreate
FLT_POSTOP_CALLBACK_STATUS
MyDLPMFPostCreate (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __in_opt PVOID CompletionContext,
    __in FLT_POST_OPERATION_FLAGS Flags
    )
{	
	
	POBJECT_NAME_INFORMATION dosNameInfo = NULL;
	NTSTATUS status = STATUS_SUCCESS;
	ULONG replyLength;
	enum ActionType action;

    PMYDLPMF_STREAM_HANDLE_CONTEXT scannerContext;
    FLT_POSTOP_CALLBACK_STATUS returnStatus = FLT_POSTOP_FINISHED_PROCESSING;  
    BOOLEAN safeToOpen, scanFile;

    UNREFERENCED_PARAMETER( CompletionContext );
    UNREFERENCED_PARAMETER( Flags );  
    
	PAGED_CODE();

	if (!Configured)
	{
		MyDLPMFConfigurationUpdate();
	}

    if (!NT_SUCCESS( Data->IoStatus.Status ) ||
        (STATUS_REPARSE == Data->IoStatus.Status)) {

        return FLT_POSTOP_FINISHED_PROCESSING;
    }  

	
	//We do not need to analyze a post create if archive inbound is not enabled
	if (FilterConf.ArchiveInbound)
	{
		status = IoQueryFileDosDeviceName( FltObjects->FileObject, &dosNameInfo );

		if ( status == STATUS_SUCCESS )
		{

			fileNotification->Type = POSTCREATE;

			RtlCopyMemory( &fileNotification->FileName,
							dosNameInfo->Name.Buffer,
							dosNameInfo->Name.Length + 1*sizeof(WCHAR));
			fileNotification->FileNameLength = dosNameInfo->Name.Length + 1*sizeof(WCHAR);
			
			//Check if file is not a directory
			if ((Data->Iopb->Parameters.Create.Options & FILE_NON_DIRECTORY_FILE) && dosNameInfo->Name.Length > 2*sizeof(WCHAR))
			{
				//DbgPrint("Regular file: %ws\n", fileNotification->FileName);

			}
			else
			{
				//File is not a regular file, no need for further processing
				//ExReleaseFastMutexUnsafe(&fileNotificationMutex);
				/* fileNotification buffer critical section ends */

				return FLT_POSTOP_FINISHED_PROCESSING;
				//DbgPrint("Unknown file: %ws\n", dosNameInfo->Name.Buffer);
			}

			if(dosNameInfo != NULL)
				ExFreePool(dosNameInfo);

			replyLength = sizeof( MYDLPMF_REPLY ) + sizeof (FILTER_REPLY_HEADER);

			status = FltSendMessage( FilterConf.Filter,
									 &FilterConf.ClientPort,
									 fileNotification,
									 sizeof( MYDLPMF_FILE_NOTIFICATION ),
									 reply,
									 &replyLength,
									 &FilterConf.ReadTimeout );	

			if (STATUS_TIMEOUT == status)
			{
					action = ALLOW;
#ifdef DBG_PRINT
					DbgPrint("PostCreate FltSendMessage timeout");
#endif
			}
			else if (STATUS_SUCCESS == status)
			{
					action = reply->Action;
			}
			else
			{
					action = ALLOW;
#ifdef DBG_PRINT
					DbgPrint(" FltSendMessage failed status 0x%X\n", status);
#endif
			}
		}
		else
		{
				//Unable to get file path
				return FLT_POSTOP_FINISHED_PROCESSING;
		}
	}
	else
	{
		action = ALLOW;
	}

	if (action == BLOCK){
		//TODO: Use this to prevent file open
        FltCancelFileOpen( FltObjects->Instance, FltObjects->FileObject );

        Data->IoStatus.Status = STATUS_ACCESS_DENIED;
        Data->IoStatus.Information = 0;
        returnStatus = FLT_POSTOP_FINISHED_PROCESSING;

    } else if (FltObjects->FileObject->WriteAccess) {
        
		status = FltAllocateContext( FilterConf.Filter,
                                     FLT_STREAMHANDLE_CONTEXT,
                                     sizeof(MYDLPMF_STREAM_HANDLE_CONTEXT),
                                     PagedPool,
                                     &scannerContext );

        if (NT_SUCCESS(status)) 
		{           
			scannerContext->RescanRequired = TRUE;

            (VOID) FltSetStreamHandleContext( FltObjects->Instance,
                                              FltObjects->FileObject,
                                              FLT_SET_CONTEXT_REPLACE_IF_EXISTS,
                                              scannerContext,
                                              NULL );
            FltReleaseContext(scannerContext);
        }
    }
    return returnStatus;
}
#pragma endregion

#pragma region PreCleanup
FLT_PREOP_CALLBACK_STATUS
MyDLPMFPreCleanup (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __deref_out_opt PVOID *CompletionContext
    )
{
	POBJECT_NAME_INFORMATION dosNameInfo = NULL;
    NTSTATUS status;
    PMYDLPMF_STREAM_HANDLE_CONTEXT context;
    BOOLEAN safe;
	ULONG replyLength ;

    UNREFERENCED_PARAMETER(Data);
    UNREFERENCED_PARAMETER(CompletionContext);

	PAGED_CODE();

	
	if (!Configured)
	{
		MyDLPMFConfigurationUpdate();
	}

	fileNotification->Type = NONE;
    status = FltGetStreamHandleContext(FltObjects->Instance,
                                        FltObjects->FileObject,
                                        &context );

    if (NT_SUCCESS(status))
	{
        if (context->RescanRequired)
		{
			status = IoQueryFileDosDeviceName( FltObjects->FileObject, &dosNameInfo );
			if (status == STATUS_SUCCESS)
			{
				fileNotification->Type = PRECLEANUP;

				RtlCopyMemory( &fileNotification->FileName,
								dosNameInfo->Name.Buffer,
								dosNameInfo->Name.Length + 1*sizeof(WCHAR));
				fileNotification->FileNameLength = dosNameInfo->Name.Length + 1*sizeof(WCHAR);		

				replyLength = sizeof (FILTER_REPLY_HEADER) + sizeof( MYDLPMF_REPLY );
			
				status = FltSendMessage( FilterConf.Filter,
					&FilterConf.ClientPort,
					fileNotification,
					sizeof( MYDLPMF_FILE_NOTIFICATION ),
					reply,
					&replyLength,
					&FilterConf.ReadTimeout );
					
				if(dosNameInfo != NULL)
					ExFreePool(dosNameInfo);
			}		
        }
        FltReleaseContext( context );
    }
    return FLT_PREOP_SUCCESS_NO_CALLBACK;
}
#pragma endregion

#pragma region PreWrite
FLT_PREOP_CALLBACK_STATUS
MyDLPMFPreWrite (
    __inout PFLT_CALLBACK_DATA Data,
    __in PCFLT_RELATED_OBJECTS FltObjects,
    __deref_out_opt PVOID *CompletionContext
    )
{
    FLT_PREOP_CALLBACK_STATUS returnStatus = FLT_PREOP_SUCCESS_NO_CALLBACK;
    NTSTATUS status;
    PMYDLPMF_STREAM_HANDLE_CONTEXT context = NULL;	
    ULONG replyLength;
	enum ActionType action;
    PUCHAR buffer;
	int i,j = 0;
	ULONG writeLength = 0;
	POBJECT_NAME_INFORMATION dosNameInfo = NULL;

    UNREFERENCED_PARAMETER(CompletionContext);

	PAGED_CODE();
	
	if (!Configured)
	{
		MyDLPMFConfigurationUpdate();
	}

	if (FLT_IS_FASTIO_OPERATION(Data))
		return FLT_PREOP_DISALLOW_FASTIO;

    if (FilterConf.ClientPort == NULL)
	{
        return FLT_PREOP_SUCCESS_NO_CALLBACK;
	}

    status = FltGetStreamHandleContext( FltObjects->Instance,
										FltObjects->FileObject,
										&context );


    if (!NT_SUCCESS( status )) 
	{
        return FLT_PREOP_SUCCESS_NO_CALLBACK;
    }
	
    try {
        if (Data->Iopb->Parameters.Write.Length != 0) 
		{

			if (Data->Iopb->Parameters.Write.MdlAddress != NULL)
			{
                buffer = MmGetSystemAddressForMdlSafe( Data->Iopb->Parameters.Write.MdlAddress,
														NormalPagePriority );
                if (buffer == NULL)
				{
                    Data->IoStatus.Status = STATUS_INSUFFICIENT_RESOURCES;
                    Data->IoStatus.Information = 0;
                    returnStatus = FLT_PREOP_COMPLETE;
                    leave;
                }
            }
			else
			{
                buffer  = Data->Iopb->Parameters.Write.WriteBuffer;
            }
		}

        if (writeNotification == NULL)
		{
            Data->IoStatus.Status = STATUS_INSUFFICIENT_RESOURCES;
            Data->IoStatus.Information = 0;
            returnStatus = FLT_PREOP_COMPLETE;
            leave;
        }
		
		writeNotification->Type = PREWRITE;		
		writeNotification->BytesToScan = 0;		

		for(i = 0, writeLength = 0; writeLength < Data->Iopb->Parameters.Write.Length; 
			writeLength += MYDLPMF_READ_BUFFER_SIZE, i++)
		{
#ifdef DBG_PRINT
			//DbgPrint("PreWrite inside loop start writeNotification->Type: %d,\n", writeNotification->Type);	
#endif
			writeNotification->BytesToScan = min( Data->Iopb->Parameters.Write.Length - writeLength, MYDLPMF_READ_BUFFER_SIZE );
	
			try  
			{
				RtlCopyMemory( &writeNotification->Contents,
					buffer + writeLength,
					writeNotification->BytesToScan );
#ifdef DBG_PRINT
				//DbgPrint("Bytes to scan %d, offset: %d\n", writeNotification->BytesToScan, Data->Iopb->Parameters.Write.ByteOffset);
#endif
			}
			except(EXCEPTION_EXECUTE_HANDLER)
			{
				Data->IoStatus.Status = GetExceptionCode();
				Data->IoStatus.Information = 0;
				returnStatus = FLT_PREOP_COMPLETE;
				leave;
			}

			replyLength = sizeof (FILTER_REPLY_HEADER) + sizeof( MYDLPMF_REPLY );

			status = IoQueryFileDosDeviceName(FltObjects->FileObject, &dosNameInfo);

			if (status == STATUS_SUCCESS)
			{
				RtlCopyMemory(&writeNotification->FileName,
                           dosNameInfo->Name.Buffer,
						   dosNameInfo->Name.Length + 1*sizeof(WCHAR));
				writeNotification->FileNameLength = dosNameInfo->Name.Length + 1*sizeof(WCHAR);
#ifdef DBG_PRINT
				//DbgPrint("writeNotification->FileName: %ws\n", writeNotification->FileName);
#endif
			}
			else
			{
				writeNotification->FileNameLength = 0;
			}

			if (dosNameInfo != NULL)
				ExFreePool(dosNameInfo);
#ifdef DBG_PRINT
			//DbgPrint("writeNotification->Type: %d\n", writeNotification->Type);
#endif
			status = FltSendMessage( FilterConf.Filter,
									 &FilterConf.ClientPort,
									 writeNotification,
									 sizeof( MYDLPMF_WRITE_NOTIFICATION ) - (MYDLPMF_READ_BUFFER_SIZE - writeNotification->BytesToScan),
									 reply,
									 &replyLength,
									 &FilterConf.WriteTimeout );								 

			if (STATUS_TIMEOUT == status)
			{
				action = ALLOW;
			}
			else if (STATUS_SUCCESS == status)
			{
				action = reply->Action;
			}
			else
			{
				action = ALLOW;
#ifdef DBG_PRINT
				DbgPrint( "mydlpmf couldn't send message to user-mode, status 0x%\n", status );
#endif
			}

			if (action == BLOCK)
			{
				if (!FlagOn(Data->Iopb->IrpFlags, IRP_PAGING_IO))
				{
#ifdef DBG_PRINT
					DbgPrint( "mydlpmf block write\n" );
#endif
					Data->IoStatus.Status = STATUS_ACCESS_DENIED;
					Data->IoStatus.Information = 0;
					returnStatus = FLT_PREOP_COMPLETE;
				}
			}
			//important response corrupts write notfication
			writeNotification->Type = PREWRITE;
#ifdef DBG_PRINT
			//DbgPrint("PreWrite inside loop end writeNotification->Type: %d,\n", writeNotification->Type);
#endif
		}
    }
	finally
	{
		//ExReleaseFastMutexUnsafe(&WriteNotificationMutex);
		/* writeNotification buffer critical section ends */

        if (context)
		{
            FltReleaseContext(context);
        }
    }

    return returnStatus;
}
#pragma endregion