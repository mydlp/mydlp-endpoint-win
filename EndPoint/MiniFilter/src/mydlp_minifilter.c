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
#include "scanuk.h"
#include "mydlp_minifilter.h"

#pragma prefast(disable:__WARNING_ENCODE_MEMBER_FUNCTION_POINTER, "Not valid for kernel mode drivers")



MYDLPMF_DATA MyDLPMFData;
int initialized = 0; 

PMYDLPMF_NOTIFICATION writeNotification = NULL;
PMYDLPMF_NOTIFICATION miniNotification = NULL;
//FAST_MUTEX WriteNotificationMutex;
//FAST_MUTEX MiniNotificationMutex;

const UNICODE_STRING MyDLPMFExtensionsToScan[] =
    { RTL_CONSTANT_STRING( L"doc"),
      RTL_CONSTANT_STRING( L"txt"),
      RTL_CONSTANT_STRING( L"bat"),
      RTL_CONSTANT_STRING( L"cmd"),
      RTL_CONSTANT_STRING( L"inf"),
      {0, 0, NULL}
    };


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

BOOLEAN
MyDLPMFpCheckExtension (
    __in PUNICODE_STRING Extension
    );


#ifdef ALLOC_PRAGMA
    #pragma alloc_text(INIT, DriverEntry)
    #pragma alloc_text(PAGE, MyDLPMFInstanceSetup)
    #pragma alloc_text(PAGE, MyDLPMFPreCreate)
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

    status = FltRegisterFilter( DriverObject,
                                &FilterRegistration,
                                &MyDLPMFData.Filter );


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

        status = FltCreateCommunicationPort( MyDLPMFData.Filter,
                                             &MyDLPMFData.ServerPort,
                                             &oa,
                                             NULL,
                                             MyDLPMFPortConnect,
                                             MyDLPMFPortDisconnect,
                                             NULL,
                                             1 );
    
        FltFreeSecurityDescriptor( sd );

        if (NT_SUCCESS( status )) {

            status = FltStartFiltering( MyDLPMFData.Filter );

            if (NT_SUCCESS( status )) {

				writeNotification = ExAllocatePoolWithTag( NonPagedPool,
                                              sizeof( MYDLPMF_NOTIFICATION ),
                                              'mdlp' );

				if (writeNotification == NULL) 
				{					  
					DbgPrint("ExAllocatePoolWithTag failed for writeNotification!!!");
					FltCloseCommunicationPort( MyDLPMFData.ServerPort );
					FltUnregisterFilter( MyDLPMFData.Filter );
					return status;
                }
				
				miniNotification = ExAllocatePoolWithTag( NonPagedPool,
                                              sizeof( MYDLPMF_MINI_NOTIFICATION ),
                                              'mdlp' );

				if (miniNotification == NULL) 
				{					  
					DbgPrint("ExAllocatePoolWithTag failed for miniNotification!!!");
					FltCloseCommunicationPort( MyDLPMFData.ServerPort );
					FltUnregisterFilter( MyDLPMFData.Filter );
					return status;
                }

				//ExInitializeFastMutex(&WriteNotificationMutex);
				//ExInitializeFastMutex(&MiniNotificationMutex);

                return STATUS_SUCCESS;
            }

            FltCloseCommunicationPort( MyDLPMFData.ServerPort );
        }
    }

    FltUnregisterFilter( MyDLPMFData.Filter );

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

    ASSERT( MyDLPMFData.ClientPort == NULL );
    ASSERT( MyDLPMFData.UserProcess == NULL );
	ASSERT( MyDLPMFData.ErlangProcess == NULL );

    MyDLPMFData.UserProcess = PsGetCurrentProcess();
    MyDLPMFData.ClientPort = ClientPort;

    DbgPrint( "mydlpmf connected, port=0x%p\n", ClientPort );

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

    DbgPrint( "mydlpmf disconnected, port=0x%p\n", MyDLPMFData.ClientPort );
 
    FltCloseClientPort( MyDLPMFData.Filter, &MyDLPMFData.ClientPort );

    MyDLPMFData.UserProcess = NULL;
	MyDLPMFData.ErlangProcess = NULL;
	initialized = 0;
}
#pragma endregion 

#pragma region Unload
NTSTATUS
MyDLPMFUnload (
    __in FLT_FILTER_UNLOAD_FLAGS Flags
    )
{
    UNREFERENCED_PARAMETER( Flags );
	DbgPrint( "mydlpmf unload" );
    FltCloseCommunicationPort( MyDLPMFData.ServerPort );
	FltUnregisterFilter( MyDLPMFData.Filter );

	if (writeNotification != NULL) {
            ExFreePoolWithTag( writeNotification, 'mdlp' );			
    }
	
	if (writeNotification != NULL) {
		ExFreePoolWithTag( miniNotification, 'mdlp' );
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

	UNREFERENCED_PARAMETER( FltObjects );
    UNREFERENCED_PARAMETER( Flags );
    UNREFERENCED_PARAMETER( VolumeFilesystemType );
    
    PAGED_CODE();

    ASSERT( FltObjects->Filter == MyDLPMFData.Filter );

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

    return STATUS_SUCCESS;
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

	UNREFERENCED_PARAMETER( FltObjects );
    UNREFERENCED_PARAMETER( CompletionContext );

    PAGED_CODE();

	if (!initialized){

		// miniNotification Buffer critical section start 
		//ExAcquireFastMutexUnsafe(&MiniNotificationMutex);
		
		//Error here
		miniNotification->Type = INIT;
		replyLength = sizeof( MYDLPMF_REPLY );

		status = FltSendMessage( MyDLPMFData.Filter,
			&MyDLPMFData.ClientPort,
			miniNotification,
			sizeof( MYDLPMF_MINI_NOTIFICATION ),
			miniNotification,
			&replyLength,
			NULL );		
		
		if (STATUS_SUCCESS == status) {
			HANDLE pid = ((PMYDLPMF_CONF_REPLY) miniNotification)->pid;
			status = PsLookupProcessByProcessId(pid, &MyDLPMFData.ErlangProcess); 
			
			if (STATUS_SUCCESS == status) {
				initialized = 1;
				DbgPrint("Initialized minifilter runtime conf pid: %d", pid);
			}
			else
			{
				DbgPrint("Configuraition initialization failed");
			}
		}

		//ExReleaseFastMutexUnsafe(&MiniNotificationMutex);	
		// miniNotification buffer critical section ends					      
	}

	if (IoThreadToProcess( Data->Thread ) == MyDLPMFData.UserProcess || IoThreadToProcess( Data->Thread ) == MyDLPMFData.ErlangProcess) {

        return FLT_PREOP_SUCCESS_NO_CALLBACK;
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


    if (!NT_SUCCESS( Data->IoStatus.Status ) ||
        (STATUS_REPARSE == Data->IoStatus.Status)) {

        return FLT_POSTOP_FINISHED_PROCESSING;
    }
    
	status = IoQueryFileDosDeviceName( FltObjects->FileObject, &dosNameInfo );

	if ( status == STATUS_SUCCESS )
	{
		/* miniNotification Buffer critical section start */
		//ExAcquireFastMutexUnsafe(&MiniNotificationMutex);

		miniNotification->BytesToScan = 0;
		miniNotification->Type = POSTCREATE;

		RtlCopyMemory( &miniNotification->FileName,
                        dosNameInfo->Name.Buffer,
						dosNameInfo->Name.Length + 1*sizeof(WCHAR));
		miniNotification->FileNameLength = dosNameInfo->Name.Length + 1*sizeof(WCHAR);
		
		if(dosNameInfo != NULL)
			ExFreePool(dosNameInfo);

		//Check if file is not a directory
		if ((Data->Iopb->Parameters.Create.Options & FILE_NON_DIRECTORY_FILE) && dosNameInfo->Name.Length > 2*sizeof(WCHAR))
		{
			//DbgPrint("Regular file: %ws\n", miniNotification->FileName);

		}
		else
		{
			//File is not a regular file, no need for further processing
			//ExReleaseFastMutexUnsafe(&MiniNotificationMutex);
			/* miniNotification buffer critical section ends */

			return FLT_POSTOP_FINISHED_PROCESSING;
			//DbgPrint("Unknown file: %ws\n", dosNameInfo->Name.Buffer);
		}

		replyLength = sizeof( MYDLPMF_REPLY );

		status = FltSendMessage( MyDLPMFData.Filter,
								 &MyDLPMFData.ClientPort,
								 miniNotification,
								 sizeof( MYDLPMF_MINI_NOTIFICATION ),
								 miniNotification,
								 &replyLength,
								 NULL );

		if (STATUS_SUCCESS == status)
		{
				action = ((PMYDLPMF_REPLY) miniNotification)->Action;
				DbgPrint("GET action status: %s", action);
		}
		else
		{
				action = ALLOW;
				DbgPrint(" FltSendMessage failed status 0x%X\n", status);
		}

		//ExReleaseFastMutexUnsafe(&MiniNotificationMutex);
		/* miniNotification buffer critical section ends */
	}
	else
	{
			//Unable to get file path
			return FLT_POSTOP_FINISHED_PROCESSING;
	}

	if (!NT_SUCCESS( Data->IoStatus.Status ) ||
        (STATUS_REPARSE == Data->IoStatus.Status))
	{
        return FLT_POSTOP_FINISHED_PROCESSING;
    }

	if (action == BLOCK){

        DbgPrint( "Block file in postcreate\n" );

		//TODO: Use this to prevent file open
        FltCancelFileOpen( FltObjects->Instance, FltObjects->FileObject );

        Data->IoStatus.Status = STATUS_ACCESS_DENIED;
        Data->IoStatus.Information = 0;

        returnStatus = FLT_POSTOP_FINISHED_PROCESSING;

    } else if (FltObjects->FileObject->WriteAccess) {
        
		status = FltAllocateContext( MyDLPMFData.Filter,
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

	miniNotification->BytesToScan = 0;
	miniNotification->Type = NONE;
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
				DbgPrint("MyDLPMFPreCleanup path: %ws \n", dosNameInfo->Name.Buffer);

				// miniNotification Buffer critical section start 
				//ExAcquireFastMutexUnsafe(&MiniNotificationMutex);

				miniNotification->BytesToScan = 0;
				miniNotification->Type = PRECLEANUP;

				DbgPrint("MyDLPMFPreCleanup Copy memory");
				RtlCopyMemory( &miniNotification->FileName,
								dosNameInfo->Name.Buffer,
								dosNameInfo->Name.Length + 1*sizeof(WCHAR));
				miniNotification->FileNameLength = dosNameInfo->Name.Length + 1*sizeof(WCHAR);		

				replyLength = sizeof( MYDLPMF_REPLY );
			
				status = FltSendMessage( MyDLPMFData.Filter,
					&MyDLPMFData.ClientPort,
					miniNotification,
					sizeof( MYDLPMF_MINI_NOTIFICATION ),
					miniNotification,
					&replyLength,
					NULL );

				DbgPrint("MyDLPMFPreCleanup sending message length: %d replylength: %d\n", sizeof( MYDLPMF_MINI_NOTIFICATION ), replyLength);
				DbgPrint("MyDLPMFPreCleanup message sent status %x \n", status);

				//ExReleaseFastMutexUnsafe(&MiniNotificationMutex);	
				// miniNotification buffer critical section ends

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
	
	if (FLT_IS_FASTIO_OPERATION(Data))
		return FLT_PREOP_DISALLOW_FASTIO;

    if (MyDLPMFData.ClientPort == NULL)
	{
        return FLT_PREOP_SUCCESS_NO_CALLBACK;
    }

    status = FltGetStreamHandleContext( FltObjects->Instance,
										FltObjects->FileObject,
										&context );

    if (!NT_SUCCESS( status )) {        
        return FLT_PREOP_SUCCESS_NO_CALLBACK;
    }
	
    try {
		/* writeNotification Buffer critical section start */
		//ExAcquireFastMutexUnsafe(&WriteNotificationMutex);

        if (Data->Iopb->Parameters.Write.Length != 0) 
		{
			DbgPrint("Data->Iopb->Parameters.Write.Length = %d", Data->Iopb->Parameters.Write.Length);

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

		DbgPrint("PreWrite start loop writeNotification->Type: %d,\n", writeNotification->Type);

		for(i = 0, writeLength = 0; writeLength < Data->Iopb->Parameters.Write.Length; 
			writeLength += MYDLPMF_READ_BUFFER_SIZE, i++)
		{
			DbgPrint("PreWrite inside loop start writeNotification->Type: %d,\n", writeNotification->Type);	
			writeNotification->BytesToScan = min( Data->Iopb->Parameters.Write.Length - writeLength, MYDLPMF_READ_BUFFER_SIZE );
	
			try  
			{
				RtlCopyMemory( &writeNotification->Contents,
					buffer + writeLength,
					writeNotification->BytesToScan );
				DbgPrint("Bytes to scan %d, offset: %d\n", writeNotification->BytesToScan, Data->Iopb->Parameters.Write.ByteOffset);

			}
			except(EXCEPTION_EXECUTE_HANDLER)
			{
				Data->IoStatus.Status = GetExceptionCode();
				Data->IoStatus.Information = 0;
				returnStatus = FLT_PREOP_COMPLETE;
				leave;
			}

			replyLength = sizeof(MYDLPMF_REPLY);

			status = IoQueryFileDosDeviceName(FltObjects->FileObject, &dosNameInfo);

			if (status == STATUS_SUCCESS)
			{
				RtlCopyMemory(&writeNotification->FileName,
                           dosNameInfo->Name.Buffer,
						   dosNameInfo->Name.Length + 1*sizeof(WCHAR));
				writeNotification->FileNameLength = dosNameInfo->Name.Length + 1*sizeof(WCHAR);
				DbgPrint("writeNotification->FileName: %ws\n", writeNotification->FileName);

			}
			else
			{
				writeNotification->FileNameLength = 0;
			}

			if (dosNameInfo != NULL)
				ExFreePool(dosNameInfo);

			DbgPrint("writeNotification->Type: %d\n", writeNotification->Type);
			status = FltSendMessage( MyDLPMFData.Filter,
									 &MyDLPMFData.ClientPort,
									 writeNotification,
									 sizeof( MYDLPMF_NOTIFICATION ) - (MYDLPMF_READ_BUFFER_SIZE - writeNotification->BytesToScan),
									 writeNotification,
									 &replyLength,
									 NULL );

			if (STATUS_SUCCESS == status)
			{
				action = ((PMYDLPMF_REPLY) writeNotification)->Action;
			}
			else
			{
				action = ALLOW;
				DbgPrint( "mydlpmf couldn't send message to user-mode, status 0x%\n", status );
			}

			if (action == BLOCK)
			{
				if (!FlagOn(Data->Iopb->IrpFlags, IRP_PAGING_IO))
				{
					DbgPrint( "mydlpmf block write\n" );
					Data->IoStatus.Status = STATUS_ACCESS_DENIED;
					Data->IoStatus.Information = 0;
					returnStatus = FLT_PREOP_COMPLETE;
				}
			}
			//important response corrupts write notfication
			writeNotification->Type = PREWRITE;
			DbgPrint("PreWrite inside loop end writeNotification->Type: %d,\n", writeNotification->Type);
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