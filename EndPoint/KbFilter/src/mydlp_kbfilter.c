// Copyright (C) 2012 Huseyin Ozgur Batur <ozgur@medra.com.tr>
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

ULONG kbdClsNum;
ULONG startCount;
ULONG skip;
ULONG pendingIrpCount;

PDEVICE_OBJECT pKbdDeviceObject;
PDEVICE_OBJECT topOfStack;
IRPMJREAD oldFunction;



NTSTATUS DriverEntry(IN PDRIVER_OBJECT DriverObject,
					 IN PUNICODE_STRING RegistryPath)
{

	UNICODE_STRING uniNtDeviceName;
	UNICODE_STRING uniDosDeviceName;
	ULONG i;
	PDEVICE_OBJECT DeviceObject;
	NTSTATUS status = STATUS_SUCCESS;

	KdPrint(("MyDLPKBF: DriverEntry.\n"));

	for(i = 0; i <= IRP_MJ_MAXIMUM_FUNCTION; i++)
	{
		DriverObject->MajorFunction[i] = MyDLPKBF_PassThrough;
	}

	DriverObject->MajorFunction[IRP_MJ_CREATE] = MyDLPKBF_CreateClose;
	DriverObject->MajorFunction[IRP_MJ_CLOSE] = MyDLPKBF_CreateClose;
	DriverObject->MajorFunction[IRP_MJ_READ] = MyDLPKBF_DispatchRead;
	DriverObject->DriverUnload = MyDLPKBF_Unload;

	RtlInitUnicodeString(&uniNtDeviceName, NT_DEVICE_NAME);
	RtlInitUnicodeString(&uniDosDeviceName, DOS_DEVICE_NAME);

	status = IoCreateDevice(DriverObject, 0, &uniNtDeviceName,FILE_DEVICE_UNKNOWN, 0, FALSE, &DeviceObject);

	if(!NT_SUCCESS(status))
	{
		return status;
	}

	status = IoCreateSymbolicLink(&uniDosDeviceName, &uniNtDeviceName);

	if(status != STATUS_SUCCESS )
	{
		IoDeleteDevice(DeviceObject);
		return status;
	}

	DeviceObject->Flags |= DO_BUFFERED_IO;
	DeviceObject->Flags &= ~DO_DEVICE_INITIALIZING;
	skip = 0;
	return status;
}

NTSTATUS
MyDLPKBF_CreateClose(IN PDEVICE_OBJECT DeviceObject,
					 IN PIRP Irp)
{
	ULONG i;
	NTSTATUS status;
	PIO_STACK_LOCATION stack;
	UNICODE_STRING uniKbdDeviceName;
	PFILE_OBJECT KbdFileObject;
	PDEVICE_OBJECT KbdDeviceObject;
	ULONG counter;
	HANDLE hDir;
	OBJECT_ATTRIBUTES oa;
	UNICODE_STRING uniOa;
	PVOID pBuffer;
	PVOID pContext;
	ULONG RetLen;
	PDIRECTORY_BASIC_INFORMATION pDirBasicInfo;
	UNICODE_STRING uniKbdDrv;
	char* KbdclsNum;
	char arKbdCls[0x10];

	stack = IoGetCurrentIrpStackLocation(Irp);

	switch (stack->MajorFunction)
	{
	case IRP_MJ_CREATE:
		{
			if(InterlockedIncrement(&startCount) == 1)
			{
				if(skip)
				{
					break;
				}
				RtlInitUnicodeString(&uniOa, L"\\Device");
				InitializeObjectAttributes(&oa, &uniOa, OBJ_CASE_INSENSITIVE, NULL, NULL);
				status = ZwOpenDirectoryObject(&hDir, DIRECTORY_ALL_ACCESS, &oa);
				if(!NT_SUCCESS(status))
				{
					break;
				}

				pBuffer = ExAllocatePoolWithTag(PagedPool, ALLOC_SIZE, PoolTag);
				pContext = ExAllocatePoolWithTag(PagedPool, ALLOC_SIZE, PoolTag);
				memset(pBuffer, 0, ALLOC_SIZE);
				memset(pContext, 0, ALLOC_SIZE);
				memset(arKbdCls, 0, 0x10);
				counter = 0;
				kbdClsNum = 0;

				while(TRUE)
				{
					status = ZwQueryDirectoryObject(hDir, pBuffer, ALLOC_SIZE, TRUE, FALSE, pContext, &RetLen);
					if(!NT_SUCCESS(status))
					{
						break;
					}

					pDirBasicInfo = (PDIRECTORY_BASIC_INFORMATION)pBuffer;
					pDirBasicInfo->ObjectName.Length -= 2;

					RtlInitUnicodeString(&uniKbdDrv, L"KeyboardClass");

					if(RtlCompareUnicodeString(&pDirBasicInfo->ObjectName, &uniKbdDrv, FALSE) == 0)
					{
						KbdclsNum = (char*) ((ULONG_PTR) (pDirBasicInfo->ObjectName.Length) + (ULONG_PTR) (pDirBasicInfo->ObjectName.Buffer));
						arKbdCls[counter] = *KbdclsNum;
						counter++;
					}

					pDirBasicInfo->ObjectName.Length += 2;
				}

				ExFreePool(pBuffer);
				ExFreePool(pContext);
				ZwClose(hDir);
				for(i = 0; i < 0x10; i++)
				{
					if(arKbdCls[i] == 0)
						break;
					else if(arKbdCls[i] == 0x30)
						kbdClsNum |= KBDCLASS_0;
					else if(arKbdCls[i] == 0x31)
						kbdClsNum |= KBDCLASS_1;
					else if(arKbdCls[i] == 0x32)
						kbdClsNum |= KBDCLASS_2;
				}
				if(kbdClsNum & KBDCLASS_0)
				{
					RtlInitUnicodeString(&uniKbdDeviceName, L"\\Device\\KeyboardClass0");
				}
				else
				{
					if(kbdClsNum & KBDCLASS_2)
					{
						RtlInitUnicodeString(&uniKbdDeviceName, L"\\Device\\KeyboardClass2");
					}
					else if(kbdClsNum & KBDCLASS_1)
					{
						RtlInitUnicodeString(&uniKbdDeviceName, L"\\Device\\KeyboardClass1");
					}
					else
					{
						skip = 1;
						break;
					}
				}
				status = MyDLPKBF_IoGetDeviceObjectPointer(&uniKbdDeviceName, 0, &KbdFileObject, &KbdDeviceObject);

				if(NT_SUCCESS(status))
				{
					pKbdDeviceObject = KbdDeviceObject;
					ObDereferenceObject(KbdFileObject);
					//DbgPrint(("MyDLPKBF: Attach Device.\n"));
					__try
					{
						topOfStack = IoAttachDeviceToDeviceStack(DeviceObject, KbdDeviceObject);

						if (topOfStack != NULL)
						{
							oldFunction = pKbdDeviceObject->DriverObject->MajorFunction[IRP_MJ_READ];
							pKbdDeviceObject->DriverObject->MajorFunction[IRP_MJ_READ] = MyDLPKBF_HookProc;
						}
						else
						{
							skip = 1;
							topOfStack = NULL;
							break;
						}
					}
					__except(1)
					{
						skip = 1;
						break;
					}
				}
				else
				{
					skip = 1;
					break;
				}
			}
			break;
		}
	case IRP_MJ_CLOSE:
		{
			if(InterlockedDecrement(&startCount) == 0)
			{
				if(skip)
				{
					break;
				}
				__try
				{
					if(topOfStack)
					{
						if(oldFunction)
						{
							pKbdDeviceObject->DriverObject->MajorFunction[IRP_MJ_READ] = oldFunction;
							oldFunction = NULL;
						}
						KdPrint(("MyDLPKBF: Detach Device. \n"));
						IoDetachDevice(topOfStack);
						topOfStack = NULL;
					}
				}
				__except(1)
				{
					skip = 1;
					break;
				}
			}
			break;
		}
	}
	Irp->IoStatus.Status = STATUS_SUCCESS;
	Irp->IoStatus.Information = 0;
	IoCompleteRequest(Irp, IO_NO_INCREMENT);
	return STATUS_SUCCESS;
}

NTSTATUS MyDLPKBF_IoGetDeviceObjectPointer( IN PUNICODE_STRING ObjectName,
										   IN ACCESS_MASK DesiredAccess,
										   OUT PFILE_OBJECT *FileObject,
										   OUT PDEVICE_OBJECT *DeviceObject)
{
	NTSTATUS status;
	OBJECT_ATTRIBUTES oa;
	IO_STATUS_BLOCK iostatus;
	PVOID ptr;

	InitializeObjectAttributes( &oa, ObjectName, OBJ_KERNEL_HANDLE, NULL, NULL);

	status = ZwOpenFile( &ObjectName, DesiredAccess, &oa, &iostatus, 0x07, 0x40);

	if(!NT_SUCCESS(status))
	{
		return status;
	}
	status = ObReferenceObjectByHandle( ObjectName, DesiredAccess, 0, KernelMode, &ptr, 0);

	if(!NT_SUCCESS(status))
	{
		ZwClose(ObjectName);
		return status;
	}

	*FileObject = ptr;
	*DeviceObject = IoGetRelatedDeviceObject(ptr);
	ZwClose(ObjectName);
	return status;
}

NTSTATUS MyDLPKBF_HookProc(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
	NTSTATUS status;
	status = oldFunction(DeviceObject, Irp);
	return status;
}

NTSTATUS MyDLPKBF_DispatchRead(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
	PIO_STACK_LOCATION currentIrpStack;
	PIO_STACK_LOCATION nextIrpStack;
	NTSTATUS status;

	pendingIrpCount++;
	//DbgPrint(("MyDLPKBF: DispatchRead Start \n"));
	currentIrpStack = IoGetCurrentIrpStackLocation(Irp);

	nextIrpStack = IoGetNextIrpStackLocation(Irp);
	*nextIrpStack = *currentIrpStack;
	IoSetCompletionRoutine(Irp, MyDLPKBF_ReadComplete, DeviceObject, TRUE, TRUE, TRUE);
	status = IoCallDriver(topOfStack, Irp);
	KdPrint(("MyDLPKBF: DispatchRead End\n"));
	return status;
}

NTSTATUS MyDLPKBF_ReadComplete(IN PDEVICE_OBJECT DeviceObject,
							   IN PIRP Irp,
							   IN PVOID Context)
{
	int numKeys;
	int i;
	int block;

	PKEYBOARD_INPUT_DATA keys;

	//DbgPrint(("MyDLPKBF: ReadComplete Start\n"));

	keys = (PKEYBOARD_INPUT_DATA)Irp->AssociatedIrp.SystemBuffer;
	numKeys = Irp->IoStatus.Information / sizeof(KEYBOARD_INPUT_DATA);

	block = 0;
	for(i = 0; i < numKeys; i++)
	{
		//DbgPrint("ScanCode: %x\n", keys[i].MakeCode);
		//if(keys[i].Flags == KEY_MAKE)
		//DbgPrint("%s\n","Key Down");
		//if(keys[i].Flags == KEY_BREAK)
		//DbgPrint("%s\n","Key Up");

		if(keys[i].MakeCode == 0x54 || keys[i].MakeCode == 0x37)
		{
			block = 1;
		}
	}

	if(Irp->PendingReturned)
	{
		IoMarkIrpPending(Irp);
	}

	pendingIrpCount--;

	//DbgPrint(("MyDLPKBF: ReadComplete End.\n"));

	if (block)
	{

		Irp->IoStatus.Information = 0;
		KdPrint("MyDLPKBF: block printscreen");
		return STATUS_ACCESS_DENIED;
	}
	else
	{
		//DbPrint(("MyDLPKBF: Allow keypress\n"));
		return Irp->IoStatus.Status;
	}
}

NTSTATUS MyDLPKBF_PassThrough(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
	NTSTATUS status;
	//DbgPrint(("MyDLPKBF: Start MyDLPKBF_PassThrough\n"));
	if(!skip)
	{
		IoSkipCurrentIrpStackLocation(Irp);
		status = IoCallDriver(topOfStack, Irp);
	}
	//DbgPrint(("MyDLPKBF: End MyDLPKBF_PassThrough\n"));
	return status;
}

//This is called on driver service stop
VOID MyDLPKBF_Unload( __in PDRIVER_OBJECT DriverObject)
{
	UNICODE_STRING uniWin32NameString;
	PDEVICE_OBJECT deviceObject = DriverObject->DeviceObject;
	KTIMER kTimer;
	LARGE_INTEGER timeout;

	PAGED_CODE();

	KdPrint(("MyDLPKBF: Unload\n"));

	timeout.QuadPart = 1000000;
	KeInitializeTimer(&kTimer);

	//Wait until pending irps are handled
	//Before removing device(or BSOD happens)
	while(pendingIrpCount > 0)
	{
		KeSetTimer(&kTimer,timeout,NULL);
		KeWaitForSingleObject(&kTimer,Executive,KernelMode,NULL ,NULL);
	}

	RtlInitUnicodeString(&uniWin32NameString, DOS_DEVICE_NAME);

	IoDeleteSymbolicLink(&uniWin32NameString);

	//Delete device to compleely remove driver instance
	IoDeleteDevice(deviceObject);
	KdPrint(("MyDLPKBF: Deleted Device\n"));
}
