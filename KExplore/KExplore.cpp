#include "pch.h"
#include "KExploreClient.h"
#include "KExplore.h"

#define DRIVER_PREFIX "KExplore: "

KernelFunctions g_KernelFunctions;

// prototypes

void KExploreUnload(PDRIVER_OBJECT);
NTSTATUS KExploreDeviceControl(PDEVICE_OBJECT, PIRP);

// DriverEntry

extern "C"
NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING) {
	KdPrint((DRIVER_PREFIX "DriverEntry\n"));

	NTSTATUS status = STATUS_SUCCESS;

	UNICODE_STRING deviceName;
	UNICODE_STRING win32Name;

	RtlInitUnicodeString(&deviceName, L"\\Device\\KExplore");
	RtlInitUnicodeString(&win32Name, L"\\??\\KExplore");

	PDEVICE_OBJECT device;
	status = IoCreateDevice(DriverObject, 0, &deviceName, FILE_DEVICE_UNKNOWN, FILE_DEVICE_SECURE_OPEN, FALSE, &device);
	if (!NT_SUCCESS(status)) {
		KdPrint((DRIVER_PREFIX "Failed to create device object, status=%!STATUS!\n"));
		return status;
	}

	status = IoCreateSymbolicLink(&win32Name, &deviceName);
	if (!NT_SUCCESS(status)) {
		KdPrint((DRIVER_PREFIX "Failed to create symbolic link, status=%!STATUS!\n"));
		IoDeleteDevice(device);
		return status;
	}

	DriverObject->DriverUnload = KExploreUnload;

	DriverObject->MajorFunction[IRP_MJ_CREATE] = DriverObject->MajorFunction[IRP_MJ_CLOSE] = [](PDEVICE_OBJECT, PIRP Irp) {
		Irp->IoStatus.Status = STATUS_SUCCESS;
		Irp->IoStatus.Information = 0;
		IoCompleteRequest(Irp, 0);
		return STATUS_SUCCESS;
	};

	DriverObject->MajorFunction[IRP_MJ_DEVICE_CONTROL] = KExploreDeviceControl;

	return status;

}

void KExploreUnload(PDRIVER_OBJECT DriverObject) {
	KdPrint((DRIVER_PREFIX "Unload\n"));

	UNICODE_STRING win32Name;
	RtlInitUnicodeString(&win32Name, L"\\??\\KExplore");
	IoDeleteSymbolicLink(&win32Name);
	IoDeleteDevice(DriverObject->DeviceObject);
}

NTSTATUS KExploreDeviceControl(PDEVICE_OBJECT, PIRP Irp) {
	auto stack = IoGetCurrentIrpStackLocation(Irp);
	NTSTATUS status = STATUS_SUCCESS;
	ULONG_PTR len = 0;

	switch (stack->Parameters.DeviceIoControl.IoControlCode) {
	case KEXPLORE_IOCTL_GET_EXPORTED_NAME: {
		UNICODE_STRING name;
		PCWSTR exportName = static_cast<PCWSTR>(Irp->AssociatedIrp.SystemBuffer);
		RtlInitUnicodeString(&name, exportName);
		void* address = MmGetSystemRoutineAddress(&name);
		*(void**)exportName = address;
		len = sizeof(void*);
		break;
	}

	case KEXPLORE_IOCTL_ENUM_JOBS: {
		auto size = stack->Parameters.DeviceIoControl.OutputBufferLength;
		if (size == 0 || size % sizeof(KernelObjectData) != 0) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}

		if (stack->Parameters.DeviceIoControl.InputBufferLength < sizeof(ACCESS_MASK)) {
			status = STATUS_BUFFER_TOO_SMALL;
			break;
		}

		auto buffer = Irp->AssociatedIrp.SystemBuffer;
		auto accessMask = *static_cast<ACCESS_MASK*>(buffer);

		auto PspGetNextJob = g_KernelFunctions.PspGetNextJob;
		if (PspGetNextJob == nullptr) {
			status = STATUS_NOT_FOUND;
			KdPrint((DRIVER_PREFIX "Missing PspGetNextJob function\n"));
			break;
		}

		auto output = static_cast<KernelObjectData*>(buffer);

		int count = 0, total = 0;
		KernelObjectData data;
		for (auto job = PspGetNextJob(nullptr); job; job = PspGetNextJob(job)) {
			total++;
			if (size >= sizeof(data)) {
				data.Address = job;
				if (NT_SUCCESS(ObOpenObjectByPointer(job, 0, nullptr, accessMask, nullptr, KernelMode, &data.Handle))) {
					output[count++] = data;
					size -= sizeof(data);
				}
			}
		}

		len = count * sizeof(KernelObjectData);
		if (count < total)
			status = STATUS_MORE_ENTRIES;
		break;
	}

	case KEXPLORE_IOCTL_OPEN_OBJECT_HANDLE: {
		if (stack->Parameters.DeviceIoControl.InputBufferLength < sizeof(OpenHandleData)) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}
		HANDLE hObject = nullptr;
		auto data = static_cast<OpenHandleData*>(Irp->AssociatedIrp.SystemBuffer);
		status = ObOpenObjectByPointer(data->Object, 0, nullptr, data->AccessMask, nullptr, KernelMode, &hObject);
		if (NT_SUCCESS(status)) {
			*(HANDLE*)data = hObject;
			len = sizeof(HANDLE);
		}
		break;
	}

	case KEXPLORE_IOCTL_CLOSE_HANDLE: {
		auto size = stack->Parameters.DeviceIoControl.InputBufferLength;
		if (size == 0 || size % sizeof(HANDLE) != 0) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}

		auto handles = static_cast<HANDLE*>(Irp->AssociatedIrp.SystemBuffer);
		int count = size / sizeof(HANDLE);
		for (int i = 0; i < count; i++) {
			ZwClose(handles[i]);
		}
		len = size;
		break;
	}

	case KEXPLORE_IOCTL_READ_MEMORY: {
		if (stack->Parameters.DeviceIoControl.InputBufferLength < sizeof(PVOID)) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}

		auto memory = *static_cast<void**>(Irp->AssociatedIrp.SystemBuffer);
		auto size = stack->Parameters.DeviceIoControl.OutputBufferLength;
		auto data = MmGetSystemAddressForMdlSafe(Irp->MdlAddress, NormalPagePriority);
		if (data == nullptr) {
			status = STATUS_INSUFFICIENT_RESOURCES;
			KdPrint((DRIVER_PREFIX "failed in MmgetSystemAddressForMdlSafe\n"));
			break;
		}

		// perform the read
		RtlCopyMemory(data, memory, size);
		len = size;
		break;
	}

	case KEXPLORE_IOCTL_WRITE_MEMORY: {
		if (stack->Parameters.DeviceIoControl.InputBufferLength < sizeof(PVOID)) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}
		auto memory = *static_cast<void**>(Irp->AssociatedIrp.SystemBuffer);
		len = stack->Parameters.DeviceIoControl.OutputBufferLength;
		auto data = MmGetSystemAddressForMdlSafe(Irp->MdlAddress, NormalPagePriority);
		if (data == nullptr) {
			status = STATUS_INSUFFICIENT_RESOURCES;
			KdPrint((DRIVER_PREFIX "failed in MmgetSystemAddressForMdlSafe\n"));
			break;
		}

		// perform the write
		RtlCopyMemory(memory, data, len);
		break;
	}

	case KEXPLORE_IOCTL_READ_PROCESS_MEMORY: {
		if (stack->Parameters.DeviceIoControl.InputBufferLength < sizeof(ReadWriteProcessMemory)) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}

		auto data = static_cast<ReadWriteProcessMemory*>(Irp->AssociatedIrp.SystemBuffer);
		PEPROCESS targetProcess;
		KAPC_STATE apcState;

		// get target process
		status = PsLookupProcessByProcessId(reinterpret_cast<HANDLE>(data->ProcessId), &targetProcess);
		if (!NT_SUCCESS(status))
			break;

		auto buffer = MmGetSystemAddressForMdlSafe(Irp->MdlAddress, NormalPagePriority);
		if (buffer == nullptr) {
			status = STATUS_INSUFFICIENT_RESOURCES;
			KdPrint((DRIVER_PREFIX "failed in MmgetSystemAddressForMdlSafe\n"));
			break;
		}
		len = stack->Parameters.DeviceIoControl.OutputBufferLength;

		// attach to the address space of the target process
		KeStackAttachProcess(targetProcess, &apcState);

		__try {
			// perform the read
			RtlCopyMemory(buffer, data->Address, len);
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {
			status = STATUS_ACCESS_VIOLATION;
			len = 0;
		}

		// detach
		KeUnstackDetachProcess(&apcState);
		ObDereferenceObject(targetProcess);

		break;
	}

	case KEXPLORE_IOCTL_WRITE_PROCESS_MEMORY: {
		if (stack->Parameters.DeviceIoControl.InputBufferLength < sizeof(ReadWriteProcessMemory)) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}

		auto data = static_cast<ReadWriteProcessMemory*>(Irp->AssociatedIrp.SystemBuffer);
		PEPROCESS targetProcess;
		KAPC_STATE apcState;

		// get target process
		status = PsLookupProcessByProcessId(reinterpret_cast<HANDLE>(data->ProcessId), &targetProcess);
		if (!NT_SUCCESS(status))
			break;

		auto buffer = MmGetSystemAddressForMdlSafe(Irp->MdlAddress, NormalPagePriority);
		if (buffer == nullptr) {
			status = STATUS_INSUFFICIENT_RESOURCES;
			KdPrint((DRIVER_PREFIX "failed in MmgetSystemAddressForMdlSafe\n"));
			break;
		}
		len = stack->Parameters.DeviceIoControl.OutputBufferLength;

		// attach to the address space of the target process
		KeStackAttachProcess(targetProcess, &apcState);

		__try {
			// perform the write
			RtlCopyMemory(data->Address, buffer, len);
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {
			status = STATUS_ACCESS_VIOLATION;
			len = 0;
		}

		// detach
		KeUnstackDetachProcess(&apcState);
		ObDereferenceObject(targetProcess);
		break;
	}

	case KEXPLORE_IOCTL_INIT_KERNEL_FUNCTIONS: {
		auto size = stack->Parameters.DeviceIoControl.InputBufferLength;
		if (size == 0 || size % sizeof(PVOID) != 0) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}

		len = min(size, sizeof(KernelFunctions));
		RtlCopyMemory(&g_KernelFunctions, Irp->AssociatedIrp.SystemBuffer, len);
		break;
	}

	case KEXPLORE_IOCTL_ENUM_PROCESSES: {
		auto PsGetNextProcess = g_KernelFunctions.PsGetNextProcess;
		if (PsGetNextProcess == nullptr) {
			status = STATUS_NOT_FOUND;
			break;
		}

		auto size = stack->Parameters.DeviceIoControl.OutputBufferLength;
		if (size == 0 || size % sizeof(KernelObjectData) != 0) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}

		if(stack->Parameters.DeviceIoControl.InputBufferLength < sizeof(ACCESS_MASK)) {
			status = STATUS_BUFFER_TOO_SMALL;
			break;
		}

		auto buffer = Irp->AssociatedIrp.SystemBuffer;
		auto accessMask = *static_cast<ACCESS_MASK*>(buffer);
		auto output = static_cast<KernelObjectData*>(buffer);
		int count = 0, total = 0;
		KernelObjectData data;
		for (auto process = PsGetNextProcess(nullptr); process; process = PsGetNextProcess(process)) {
			total++;
			if (size >= sizeof(data)) {
				data.Address = process;
				if (NT_SUCCESS(ObOpenObjectByPointer(process, 0, nullptr, accessMask, nullptr, KernelMode, &data.Handle))) {
					output[count++] = data;
					size -= sizeof(data);
				}
			}
		}

		len = count * sizeof(data);
		if (count < total)
			status = STATUS_MORE_ENTRIES;
		break;
	}

	case KEXPLORE_IOCTL_DEREFERENCE_OBJECTS: {
		auto size = stack->Parameters.DeviceIoControl.InputBufferLength;
		if (size == 0 || size % sizeof(PVOID) != 0) {
			status = STATUS_INVALID_BUFFER_SIZE;
			break;
		}
		auto objects = static_cast<void**>(Irp->AssociatedIrp.SystemBuffer);
		for (int i = 0; i < size / sizeof(PVOID); i++) {
			ObDereferenceObject(objects[i]);
		}
		len = size;
		break;
	}

	default:
		status = STATUS_INVALID_DEVICE_REQUEST;
		break;
	}

	Irp->IoStatus.Status = status;
	Irp->IoStatus.Information = len;
	IoCompleteRequest(Irp, 0);
	return status;

}
