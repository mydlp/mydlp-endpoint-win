// Copyright (C) 2012 Husetyin Ozgur Batur <ozgur@medra.com.tr>
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

#include "mydlp_kbfilter.h"

ULONG pendingIrpCount;

NTSTATUS DriverEntry(IN PDRIVER_OBJECT pDriverObject,
					 IN PUNICODE_STRING RegistryPath)
{
	ULONG i;
	NTSTATUS status;
	status = STATUS_SUCCESS;

	DbgPrint("DriverEntry: Start\n");

	//Set noop functions to allow Irps to pass
	for(i = 0; i <= IRP_MJ_MAXIMUM_FUNCTION; i++)
	{
		pDriverObject->MajorFunction[i] = MyDLPKBF_PassThrough;
	}

	//Set functions which adds the flavor
	pDriverObject->MajorFunction[IRP_MJ_CREATE] = MyDLPKBF_CreateClose;
	pDriverObject->MajorFunction[IRP_MJ_CLOSE] = MyDLPKBF_CreateClose;
	pDriverObject->MajorFunction[IRP_MJ_READ] = MyDLPKBF_DispatchRead;

	for (i = 0; i < MAX_KBDCOUNT; i++)
	{
		status = MyDLPKBF_CreateDevice(pDriverObject, i);
		if(!NT_SUCCESS(status))
		{
			DbgPrint("DriverEntry: End\n");
			return status;
		}
		else
		{
			DbgPrint("DriverEntry: Created device no:%d\n", i);
		}
	}

	DbgPrint("DriverEntry: End\n");
	return status;
}


NTSTATUS
MyDLPKBF_CreateDevice (IN PDRIVER_OBJECT pDriverObject,
					   IN ULONG DeviceNumber)
{
	PDEVICE_OBJECT pDevObj;
	UNICODE_STRING uniNtDeviceName, uniDosDeviceName, uniNumber;
	WCHAR ntDeviceNameBuffer[MYDLP_MAX_NAME_LENGTH];
	WCHAR dosDeviceNameBuffer[MYDLP_MAX_NAME_LENGTH];
	WCHAR numberBuffer[2];
	PMYDLPKBF_DEVICE_EXTENSION pDevExt;
	NTSTATUS status;

	DbgPrint("MyDLPKBF_CreateDevice: Start\n");
	status = STATUS_SUCCESS;

	uniNtDeviceName.Buffer = ntDeviceNameBuffer;
	uniNtDeviceName.MaximumLength = MYDLP_MAX_NAME_LENGTH * 2;
	uniNtDeviceName.Length = 0;

	uniDosDeviceName.Buffer = dosDeviceNameBuffer;
	uniDosDeviceName.MaximumLength = MYDLP_MAX_NAME_LENGTH * 2;
	uniDosDeviceName.Length = 0;

	uniNumber.Buffer = numberBuffer;
	uniNumber.MaximumLength = 4;
	uniNumber.Length = 0;

	RtlIntegerToUnicodeString( DeviceNumber, 10, &uniNumber);
	RtlAppendUnicodeToString( &uniNtDeviceName, NT_DEVICE_NAME);
	RtlAppendUnicodeStringToString( &uniNtDeviceName, &uniNumber);
	RtlAppendUnicodeToString( &uniDosDeviceName, DOS_DEVICE_NAME);
	RtlAppendUnicodeStringToString( &uniDosDeviceName, &uniNumber);

	status = IoCreateDevice(pDriverObject, sizeof(MYDLPKBF_DEVICE_EXTENSION),
		&uniNtDeviceName, FILE_DEVICE_UNKNOWN,
		0,
		TRUE,
		&pDevObj);

	if(!NT_SUCCESS(status))
	{
		DbgPrint("MyDLPKBF_CreateDevice: IoCreateDevice failed\
				 device no:%d\n", DeviceNumber);
		return status;
	}

	//set device number in device extension
	if (pDevObj!= NULL && pDevObj->DeviceExtension != NULL)
	{
		pDevExt = pDevObj->DeviceExtension;
		pDevExt->DeviceNumber = DeviceNumber;
		pDevExt->Skip = 1;
		pDevExt->OpenAttempt = 0;
	}
	else
	{
		DbgPrint("MyDLPKBF_CreateDevice: pDevObj!= NULL &&\
				 pDevObj->DeviceExtension != NULL failed device no:%d\n",
				 DeviceNumber);
		return status;
	}

	status = IoCreateSymbolicLink(&uniDosDeviceName, &uniNtDeviceName);
	if(status != STATUS_SUCCESS )
	{
		IoDeleteDevice(pDevObj);

		return status;
	}

	pDevObj->Flags |= DO_BUFFERED_IO;
	pDevObj->Flags &= ~DO_DEVICE_INITIALIZING;
	DbgPrint("MyDLPKBF_CreateDevice: End\n");
	return status;
}


NTSTATUS
MyDLPKBF_CreateClose(IN PDEVICE_OBJECT DeviceObject,
					 IN PIRP Irp)
{
	ULONG i;
	NTSTATUS status;
	PMYDLPKBF_DEVICE_EXTENSION pDevExt;
	PIO_STACK_LOCATION stack;
	UNICODE_STRING uniKbdDeviceName;
	PFILE_OBJECT KbdFileObject;
	PDEVICE_OBJECT KbdDeviceObject;

	DbgPrint("MyDLPKBF_CreateClose: Start\n");
	status = STATUS_SUCCESS;
	uniKbdDeviceName.Length = 0;
	stack = IoGetCurrentIrpStackLocation(Irp);

	switch (stack->MajorFunction)
	{
	case IRP_MJ_CREATE:
		{
			DbgPrint("MyDLPKBF_CreateClose: IRP_MJ_CREATE\n");
			pDevExt = DeviceObject->DeviceExtension;

			if (pDevExt->Skip == 0)
			{
				InterlockedIncrement(&(pDevExt->OpenAttempt));
				DbgPrint("MyDLPKBF_CreateClose: Device already opened\
						 device no:%d\n", pDevExt->DeviceNumber);
				break;
			}

			switch (pDevExt->DeviceNumber)
			{
			case 0:
				RtlInitUnicodeString(&uniKbdDeviceName,
					L"\\Device\\KeyboardClass0");
				break;
			case 1:
				RtlInitUnicodeString(&uniKbdDeviceName,
					L"\\Device\\KeyboardClass1");
				break;
			case 2:
				RtlInitUnicodeString(&uniKbdDeviceName,
					L"\\Device\\KeyboardClass2");
				break;
			default:
				pDevExt->Skip = 1;
				goto End;
			}

			status = MyDLPKBF_IoGetDeviceObjectPointer(&uniKbdDeviceName,
				0,
				&KbdFileObject,
				&KbdDeviceObject);

			if(NT_SUCCESS(status))
			{
				ObDereferenceObject(KbdFileObject);

				__try
				{
					pDevExt->TopOfStack = IoAttachDeviceToDeviceStack(
						DeviceObject,
						KbdDeviceObject);

					DbgPrint("MyDLPKBF: Attached Device device number:\
							 %d name:%wZ\n",
							 pDevExt->DeviceNumber,
							 uniKbdDeviceName);
					pDevExt->Skip = 0;
				}
				__except(1)
				{
					pDevExt->Skip = 1;
					break;
				}
			}
			else
			{
				pDevExt->Skip = 1;
				break;
			}
			break;
		}
	case IRP_MJ_CLOSE:
		{
			DbgPrint("MyDLPKBF_CreateClose: IRP_MJ_CLOSE\n");

			if (DeviceObject != NULL && DeviceObject->DeviceExtension != NULL)
			{
				pDevExt = DeviceObject->DeviceExtension;

				if (pDevExt->Skip == 1)
				{
					DbgPrint("MyDLPKBF_CreateClose: Unable to close already\
							 not enabled device no:%d\n",
							 pDevExt->DeviceNumber);
					break;
				}

				if (pDevExt->OpenAttempt > 0)
				{
					InterlockedDecrement(&(pDevExt->OpenAttempt));
					DbgPrint("MyDLPKBF_CreateClose: Discarding close for\
							 unscuccessfull open attempt",
							 pDevExt->DeviceNumber);
							 break;
				}

				__try
				{
					if(pDevExt->TopOfStack)
					{
						IoDetachDevice(pDevExt->TopOfStack);
						pDevExt->TopOfStack = NULL;
						pDevExt->Skip = 1;
					}
				}

				__except(1)
				{
					pDevExt->Skip = 1;
					break;
				}
			}
			break;
		}
	}

End:
	Irp->IoStatus.Status = status;
	Irp->IoStatus.Information = 0;
	IoCompleteRequest(Irp, IO_NO_INCREMENT);
	DbgPrint("MyDLPKBF_CreateClose: End\n");
	return status;
}

NTSTATUS MyDLPKBF_IoGetDeviceObjectPointer( IN PUNICODE_STRING ObjectName,
										   IN ACCESS_MASK DesiredAccess,
										   OUT PFILE_OBJECT *FileObject,
										   OUT PDEVICE_OBJECT *DeviceObject)
{
	NTSTATUS status;
	OBJECT_ATTRIBUTES oa;
	IO_STATUS_BLOCK iostatus;
	HANDLE objectHandle;
	PVOID ptr;

	DbgPrint("MyDLPKBF_IoGetDeviceObjectPointer: Start device name: %wZ\n",
		ObjectName);

	InitializeObjectAttributes( &oa, ObjectName, OBJ_KERNEL_HANDLE, NULL, NULL);

	status = ZwOpenFile( &objectHandle,
		DesiredAccess,
		&oa,
		&iostatus,
		0x07,
		0x40);

	if(!NT_SUCCESS(status))
	{
		DbgPrint("MyDLPKBF_IoGetDeviceObjectPointer: ZwOpenFie Failed\
				 device name: %wZ status:%d\n", ObjectName, status);
		return status;
	}
	status = ObReferenceObjectByHandle( objectHandle,
		DesiredAccess,
		0,
		KernelMode,
		&ptr,
		0);

	if(!NT_SUCCESS(status))
	{
		DbgPrint("MyDLPKBF_IoGetDeviceObjectPointer:\
				 ObReferenceObjectByHandle Failed\
				 device name: %wZ status:%d\n",
				 ObjectName,
				 status);
		ZwClose(objectHandle);
		return status;
	}

	*FileObject = ptr;
	*DeviceObject = IoGetRelatedDeviceObject(ptr);
	ZwClose(objectHandle);
	DbgPrint("MyDLPKBF_IoGetDeviceObjectPointer: End\n");
	return status;
}


NTSTATUS MyDLPKBF_DispatchRead(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
	PIO_STACK_LOCATION currentIrpStack;
	PIO_STACK_LOCATION nextIrpStack;
	NTSTATUS status;
	PMYDLPKBF_DEVICE_EXTENSION pDevExt;

	DbgPrint("MyDLPKBF_DispatchRead: Start\n");
	InterlockedIncrement(&pendingIrpCount);
	currentIrpStack = IoGetCurrentIrpStackLocation(Irp);
	pDevExt = DeviceObject->DeviceExtension;

	nextIrpStack = IoGetNextIrpStackLocation(Irp);
	*nextIrpStack = *currentIrpStack;
	IoSetCompletionRoutine(Irp,
		MyDLPKBF_ReadComplete,
		DeviceObject,
		TRUE,
		TRUE,
		TRUE);
	status = IoCallDriver(pDevExt->TopOfStack, Irp);
	DbgPrint("MyDLPKBF_DispatchRead: End\n");
	return status;
}

NTSTATUS MyDLPKBF_ReadComplete(IN PDEVICE_OBJECT DeviceObject,
							   IN PIRP Irp,
							   IN PVOID Context)
{
	int numKeys;
	int i;
	int block;

	PMYDLPKBF_DEVICE_EXTENSION pDevExt;
	PKEYBOARD_INPUT_DATA keys;

	DbgPrint("MyDLPKBF_ReadComplete: Start\n");

	pDevExt = DeviceObject->DeviceExtension;

	if(Irp->PendingReturned)
	{
		IoMarkIrpPending(Irp);
	}

	if (pDevExt->Skip)
	{
		DbgPrint("MyDLPKBF: ReadComplete pDevExt->Skip return 1 status\
				 success %d \n", pDevExt->Skip);
		return Irp->IoStatus.Status;;
	}

	keys = (PKEYBOARD_INPUT_DATA)Irp->AssociatedIrp.SystemBuffer;
	numKeys = Irp->IoStatus.Information / sizeof(KEYBOARD_INPUT_DATA);

	block = 0;
	for(i = 0; i < numKeys; i++)
	{
		if(keys[i].MakeCode == 0x54 || keys[i].MakeCode == 0x37)
		{
			block = 1;
		}
	}

	InterlockedDecrement(&pendingIrpCount);

	if (block)
	{
		Irp->IoStatus.Information = 0;
		DbgPrint("MyDLPKBF: block printscreen");
		DbgPrint("MyDLPKBF_ReadComplete: End\n");
		return STATUS_ACCESS_DENIED;
	}
	else
	{
		DbgPrint("MyDLPKBF_ReadComplete: End\n");
		return Irp->IoStatus.Status;
	}

}

NTSTATUS MyDLPKBF_PassThrough(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
	NTSTATUS status;
	PMYDLPKBF_DEVICE_EXTENSION pDevExt;
	status = STATUS_SUCCESS;
	DbgPrint("MyDLPKBF_PassThrough: Start\n");

	pDevExt = DeviceObject->DeviceExtension;
	if(!pDevExt->Skip)
	{
		IoSkipCurrentIrpStackLocation(Irp);
		status = IoCallDriver(pDevExt->TopOfStack, Irp);
	}
	else
	{
		IoCompleteRequest(Irp, IO_NO_INCREMENT);
	}
	DbgPrint("MyDLPKBF_PassThrough: End\n");
	return status;
}

//Unable to use this until found a proper way to cancel keyboard irps
//This is called on driver service stop
VOID MyDLPKBF_Unload( __in PDRIVER_OBJECT pDriverObject)
{

	UNICODE_STRING uniDosDeviceName, uniNumber;
	WCHAR dosDeviceNameBuffer[MYDLP_MAX_NAME_LENGTH];
	WCHAR numberBuffer[2];
	PDEVICE_OBJECT pDeviceObject;
	PDEVICE_OBJECT pNextDeviceObject;
	KTIMER kTimer;
	LARGE_INTEGER timeout;
	ULONG i;
	NTSTATUS status;
	PMYDLPKBF_DEVICE_EXTENSION pDevExt;
	PAGED_CODE();

	DbgPrint("MyDLPKBF: Unload\n");

	uniDosDeviceName.Buffer = dosDeviceNameBuffer;
	uniDosDeviceName.MaximumLength = MYDLP_MAX_NAME_LENGTH * 2;
	uniDosDeviceName.Length = 0;

	uniNumber.Buffer = numberBuffer;
	uniNumber.MaximumLength = 4;
	uniNumber.Length = 0;

	timeout.QuadPart = 1000000;
	KeInitializeTimer(&kTimer);

	//Wait until pending irps are handled
	//Before removing device(or BSOD happens)
	while(pendingIrpCount > 0)
	{
		KeSetTimer(&kTimer,timeout,NULL);
		KeWaitForSingleObject(&kTimer,Executive,KernelMode,NULL ,NULL);
	}

	//remove symlink for all devices
	for(i=0; i< MAX_KBDCOUNT; i++)
	{
		RtlIntegerToUnicodeString( i, 10, &uniNumber);
		RtlAppendUnicodeToString( &uniDosDeviceName, DOS_DEVICE_NAME);
		RtlAppendUnicodeStringToString( &uniDosDeviceName, &uniNumber);
		DbgPrint("MyDLPKBF: Deleting Device Symlink:%d name:%wZ\n",i, &uniDosDeviceName);
		status = IoDeleteSymbolicLink(&uniDosDeviceName);
		if(!NT_SUCCESS(status))
		{
			DbgPrint("MyDLPKBF: IoDeleteSymbolicLink Fail: %d Status:%x\n", i, status);
		}
		uniDosDeviceName.Length = 0;
		uniNumber.Length = 0;
	}

	//Delete devices to completely remove driver instance
	pDeviceObject = pDriverObject->DeviceObject;
	pNextDeviceObject = pDeviceObject->NextDevice;
	pDevExt = pDeviceObject->DeviceExtension;
	DbgPrint("MyDLPKBF: Deleting Device Ext Address:%x\n", pDevExt);
	DbgPrint("MyDLPKBF: Deleting Device %d\n", pDevExt->DeviceNumber);
	DbgPrint("MyDLPKBF: Deleting Device Address:%x\n", pDeviceObject);

	IoDeleteDevice(pDeviceObject);


	while(pNextDeviceObject != NULL)
	{
		pDeviceObject = pNextDeviceObject;
		pNextDeviceObject = pDeviceObject->NextDevice;

		pDevExt = pDeviceObject->DeviceExtension;

		IoDeleteDevice(pDeviceObject);
	}
}
