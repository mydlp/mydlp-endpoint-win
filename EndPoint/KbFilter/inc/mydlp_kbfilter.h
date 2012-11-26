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

#include <NTddk.h>
#include <Ntddkbd.h>

#define NT_DEVICE_NAME L"\\Device\\MyDLPKBF"
#define DOS_DEVICE_NAME L"\\DosDevices\\MyDLPKBF"
#define PoolTag "MyDLPKBF"
#define KBDCLASS_0 0x1
#define KBDCLASS_1 0x2
#define KBDCLASS_2 0x4
#define MAX_KBDCOUNT 3
#define	MYDLP_MAX_NAME_LENGTH		50

#define ALLOC_SIZE 0x1000

typedef struct _DIRECTORY_BASIC_INFORMATION{

	UNICODE_STRING ObjectName;
	UNICODE_STRING ObjectTypeName;
	char Data[1];
}DIRECTORY_BASIC_INFORMATION, *PDIRECTORY_BASIC_INFORMATION;

typedef struct MYDLPKBF_DEVICE_EXTENSION
{
	ULONG DeviceNumber;
}MYDLPKBF_DEVICE_EXTENSION, *PMYDLPKBF_DEVICE_EXTENSION;


typedef KEYBOARD_INPUT_DATA * PKEYBOARD_INPUT_DATA;

NTSYSAPI
NTSTATUS
NTAPI
ZwOpenDirectoryObject(OUT PHANDLE DirectoryObjectHandle,
					  IN ACCESS_MASK DesiredAccess,
					  IN POBJECT_ATTRIBUTES ObjectAttributes );

NTSYSAPI
NTSTATUS
NTAPI
ZwQueryDirectoryObject(IN HANDLE DirectoryObjectHandle,
					   OUT PDIRECTORY_BASIC_INFORMATION DirObjInformation,
					   IN ULONG BufferLength,
					   IN BOOLEAN GetNextIndex,
					   IN BOOLEAN IgnoreInputIndex,
					   IN OUT PULONG ObjectIndex,
					   OUT PULONG DataWritten OPTIONAL );

typedef NTSTATUS (*IRPMJREAD) (IN PDEVICE_OBJECT, IN PIRP);

MyDLPKBF_CreateDevice (IN PDRIVER_OBJECT pDriverObject,
					   IN ULONG DeviceNumber);

NTSTATUS MyDLPKBF_CreateClose(IN PDEVICE_OBJECT DeviceObject,
							  IN PIRP Irp);

NTSTATUS MyDLPKBF_IoGetDeviceObjectPointer(IN PUNICODE_STRING ObjectName,
										   IN ACCESS_MASK DesiredAccess,
										   OUT PFILE_OBJECT *FileObject,
										   OUT PDEVICE_OBJECT *DeviceObject);

NTSTATUS MyDLPKBF_HookProc(IN PDEVICE_OBJECT DeviceObject,
						   IN PIRP Irp);

NTSTATUS MyDLPKBF_DispatchRead(IN PDEVICE_OBJECT DeviceObject,
							   IN PIRP Irp);

NTSTATUS MyDLPKBF_ReadComplete(IN PDEVICE_OBJECT DeviceObject,
							   IN PIRP Irp,
							   IN PVOID Context);

NTSTATUS MyDLPKBF_PassThrough(IN PDEVICE_OBJECT DeviceObject,
							  IN PIRP Irp);

VOID MyDLPKBF_Unload( __in PDRIVER_OBJECT DriverObject);