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
PDEVICE_OBJECT topOfStack[MAX_KBDCOUNT];
//IRPMJREAD oldFunction;

NTSTATUS DriverEntry(IN PDRIVER_OBJECT pDriverObject,
					 IN PUNICODE_STRING RegistryPath)
{

	ULONG i;	
	NTSTATUS status = STATUS_SUCCESS;
	skip = 0;

	DbgPrint("MyDLPKBF: DriverEntry.\n");

	//Set noop functions to allow Irps to pass 
	for(i = 0; i <= IRP_MJ_MAXIMUM_FUNCTION; i++)
	{
		pDriverObject->MajorFunction[i] = MyDLPKBF_PassThrough;
	}

	//Set functions which adds the flavor
	pDriverObject->MajorFunction[IRP_MJ_CREATE] = MyDLPKBF_CreateClose;
	pDriverObject->MajorFunction[IRP_MJ_CLOSE] = MyDLPKBF_CreateClose;
	pDriverObject->MajorFunction[IRP_MJ_READ] = MyDLPKBF_DispatchRead;
	pDriverObject->DriverUnload = MyDLPKBF_Unload;
	DbgPrint("MyDLPKBF: DriverEntry 1.\n");
	for (i = 0; i < MAX_KBDCOUNT; i++)
	{
		DbgPrint("MyDLPKBF: DriverEntry 2. %d\n",i);
		status = MyDLPKBF_CreateDevice(pDriverObject, i);
		if(!NT_SUCCESS(status))
		{
			DbgPrint("MyDLPKBF: DriverEntry Fail: %d Status:%x\n", i, status);
			return status;
		}

	}
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
	NTSTATUS status = STATUS_SUCCESS;

	DbgPrint("MyDLPKBF: MyDLPKBF_Create_Device. [%d]\n", DeviceNumber);

	uniNtDeviceName.Buffer = ntDeviceNameBuffer;
	uniNtDeviceName.MaximumLength  = MYDLP_MAX_NAME_LENGTH * 2;
	uniNtDeviceName.Length = 0;

	uniDosDeviceName.Buffer = dosDeviceNameBuffer;
	uniDosDeviceName.MaximumLength  = MYDLP_MAX_NAME_LENGTH * 2;
	uniDosDeviceName.Length = 0;

	uniNumber.Buffer = numberBuffer;
	uniNumber.MaximumLength = 4;
	uniNumber.Length = 0;

	RtlIntegerToUnicodeString( DeviceNumber, 10, &uniNumber); 
	RtlAppendUnicodeToString( &uniNtDeviceName, NT_DEVICE_NAME);	
	RtlAppendUnicodeStringToString( &uniNtDeviceName, &uniNumber);
	RtlAppendUnicodeToString( &uniDosDeviceName, DOS_DEVICE_NAME);	
	RtlAppendUnicodeStringToString( &uniDosDeviceName, &uniNumber);

	status = IoCreateDevice(pDriverObject, sizeof(MYDLPKBF_DEVICE_EXTENSION), &uniNtDeviceName, FILE_DEVICE_UNKNOWN, 0, FALSE, &pDevObj);
	if(!NT_SUCCESS(status))
	{
		return status;
	}

	//set device number in device extension
	if (pDevObj!= NULL && pDevObj->DeviceExtension != NULL)
	{
		DbgPrint("MyDLPKBF: MyDLPKBF_Create Set Extension DeviceNumber:[%d]\n", DeviceNumber);
		pDevExt = pDevObj->DeviceExtension;
		pDevExt->DeviceNumber = DeviceNumber;
	}

	DbgPrint("MyDLPKBF: IoCreateSymbolicLink %wZ to %wZ\n", &uniDosDeviceName, &uniNtDeviceName);
	status = IoCreateSymbolicLink(&uniDosDeviceName, &uniNtDeviceName);
	if(status != STATUS_SUCCESS )
	{
		IoDeleteDevice(pDevObj);			

		return status;
	}


	pDevObj->Flags |= DO_BUFFERED_IO;
	pDevObj->Flags &= ~DO_DEVICE_INITIALIZING;
	//skip = 0;
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
	pDevExt = DeviceObject->DeviceExtension;
	
	switch (stack->MajorFunction)
	{
	case IRP_MJ_CREATE:
		{

			DbgPrint("MyDLPKBF: MyDLPKBF_Create.\n");

			if (DeviceObject->DeviceExtension != NULL)
			{
				pDevExt = DeviceObject->DeviceExtension;
				DbgPrint("MyDLPKBF: MyDLPKBF_Create no:%d", pDevExt->DeviceNumber);
			}

			//if(InterlockedIncrement(&startCount) == 1)
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

				DbgPrint("MyDLPKBF: MyDLPKBF_Create device:%d\n", pDevExt->DeviceNumber);	

				if (pDevExt->DeviceNumber == 0)
				{
					if(kbdClsNum & KBDCLASS_0)
					{
						RtlInitUnicodeString(&uniKbdDeviceName, L"\\Device\\KeyboardClass0");

					}
				}

				else if (pDevExt->DeviceNumber == 2)
				{
					if(kbdClsNum & KBDCLASS_2)
					{
						RtlInitUnicodeString(&uniKbdDeviceName, L"\\Device\\KeyboardClass2");
					}
				}

				else if (pDevExt->DeviceNumber == 1)
				{
					if(kbdClsNum & KBDCLASS_1)
					{
						RtlInitUnicodeString(&uniKbdDeviceName, L"\\Device\\KeyboardClass1");
					}
				}
				else
				{
					skip = 1;
					break;
				}

				DbgPrint("MyDLPKBF: MyDLPKBF_Attached device no:%d kbddevice: %wZ\n ", pDevExt->DeviceNumber, &uniKbdDeviceName);	
				status = MyDLPKBF_IoGetDeviceObjectPointer(&uniKbdDeviceName, 0, &KbdFileObject, &KbdDeviceObject);

				if(NT_SUCCESS(status))
				{
					pKbdDeviceObject = KbdDeviceObject;
					ObDereferenceObject(KbdFileObject);

					__try
					{
						topOfStack[pDevExt->DeviceNumber] = IoAttachDeviceToDeviceStack(DeviceObject, KbdDeviceObject);
						DbgPrint("MyDLPKBF: Attached Device.\n");
						/*if (topOfStack != NULL)
						{
						oldFunction = pKbdDeviceObject->DriverObject->MajorFunction[IRP_MJ_READ];
						pKbdDeviceObject->DriverObject->MajorFunction[IRP_MJ_READ] = MyDLPKBF_HookProc;
						}
						else
						{
						skip = 1;
						topOfStack = NULL;
						break;
						}*/
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
			DbgPrint("MyDLPKBF: MyDLPKBF_Close.\n");

			//if(InterlockedDecrement(&startCount) == 0)
			{
				if(skip)
				{
					break;
				}
				__try
				{
					if(topOfStack[pDevExt->DeviceNumber])
					{
						/*if(oldFunction)
						{
						pKbdDeviceObject->DriverObject->MajorFunction[IRP_MJ_READ] = oldFunction;
						oldFunction = NULL;
						}*/
						DbgPrint("MyDLPKBF: Detach Device. \n");
						IoDetachDevice(topOfStack[pDevExt->DeviceNumber]);
						topOfStack[pDevExt->DeviceNumber] = NULL;
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

/*
NTSTATUS MyDLPKBF_HookProc(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
NTSTATUS status;
status = oldFunction(DeviceObject, Irp);
return status;
}*/

NTSTATUS MyDLPKBF_DispatchRead(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
	PIO_STACK_LOCATION currentIrpStack;
	PIO_STACK_LOCATION nextIrpStack;
	NTSTATUS status;
	PMYDLPKBF_DEVICE_EXTENSION pDevExt;

	DbgPrint("MyDLPKBF: DispatchRead Start \n");
	InterlockedIncrement(&pendingIrpCount);	
	currentIrpStack = IoGetCurrentIrpStackLocation(Irp);
	pDevExt = DeviceObject->DeviceExtension;

	nextIrpStack = IoGetNextIrpStackLocation(Irp);
	*nextIrpStack = *currentIrpStack;
	IoSetCompletionRoutine(Irp, MyDLPKBF_ReadComplete, DeviceObject, TRUE, TRUE, TRUE);
	status = IoCallDriver(topOfStack[pDevExt->DeviceNumber], Irp);
	DbgPrint("MyDLPKBF: DispatchRead End\n");
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

	DbgPrint("MyDLPKBF: ReadComplete Start\n");

	keys = (PKEYBOARD_INPUT_DATA)Irp->AssociatedIrp.SystemBuffer;
	numKeys = Irp->IoStatus.Information / sizeof(KEYBOARD_INPUT_DATA);

	block = 0;
	for(i = 0; i < numKeys; i++)
	{
		DbgPrint("ScanCode: %x\n", keys[i].MakeCode);
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

	InterlockedDecrement(&pendingIrpCount);

	//DbgPrint("MyDLPKBF: ReadComplete End.\n");

	if (block)
	{

		Irp->IoStatus.Information = 0;
		DbgPrint("MyDLPKBF: block printscreen");
		return STATUS_ACCESS_DENIED;
	}
	else
	{
		//DbPrint("MyDLPKBF: Allow keypress\n");
		return Irp->IoStatus.Status;
	}
}

NTSTATUS MyDLPKBF_PassThrough(IN PDEVICE_OBJECT DeviceObject, IN PIRP Irp)
{
	NTSTATUS status;
	PMYDLPKBF_DEVICE_EXTENSION pDevExt;
	DbgPrint("MyDLPKBF: Start MyDLPKBF_PassThrough\n");
	if(!skip)
	{
		pDevExt =  DeviceObject->DeviceExtension;
		IoSkipCurrentIrpStackLocation(Irp);
		status = IoCallDriver(topOfStack[pDevExt->DeviceNumber], Irp);
	}
	//DbgPrint("MyDLPKBF: End MyDLPKBF_PassThrough\n");
	return status;
}

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
	uniDosDeviceName.MaximumLength  = MYDLP_MAX_NAME_LENGTH * 2;
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
		DbgPrint("MyDLPKBF: Deleting Device Address:%x\n", pDeviceObject);
		pNextDeviceObject = pDeviceObject->NextDevice;			

		pDevExt = pDeviceObject->DeviceExtension;
		
		DbgPrint("MyDLPKBF: Deleting Device Ext Address:%x\n", pDevExt);
		DbgPrint("MyDLPKBF: Deleting Device %d\n", pDevExt->DeviceNumber);
		IoDeleteDevice(pDeviceObject);
	}


	DbgPrint("MyDLPKBF: Deleted Devices\n");
}
