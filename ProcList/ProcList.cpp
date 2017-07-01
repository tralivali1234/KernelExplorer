// ProcList.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

#include "..\KExploreHelper\KExploreHelper.h"
#include "..\KExploreHelper\SymbolsHandler.h"
#include "..\KExplore\KExploreClient.h"

int Error(const char* message) {
	printf("%s (error=%d)\n", message, ::GetLastError());
	return 1;
}

bool InitKernelFunctions(HANDLE hDevice) {
	SymbolsHandler handler;
	auto address = handler.LoadSymbolsForModule("%systemroot%\\system32\\ntoskrnl.exe");
	auto kernelAddress = KExploreHelper::GetKernelBaseAddress();

	KernelFunctions functions = { 0 };
	functions.PsGetNextProcess = (PVOID)((ULONG_PTR)handler.GetSymbolFromName("PsGetNextProcess")->GetSymbolInfo()->Address - address + (ULONG_PTR)kernelAddress);

	DWORD returned;
	return ::DeviceIoControl(hDevice, KEXPLORE_IOCTL_INIT_KERNEL_FUNCTIONS, &functions, sizeof(functions), nullptr, 0, &returned, nullptr) ? true : false; 
}

bool EnumProcesses(HANDLE hDevice) {
	PVOID processes[2048] = { 0 };
	DWORD returned;
	if(::DeviceIoControl(hDevice, KEXPLORE_IOCTL_ENUM_PROCESSES, nullptr, 0, processes, sizeof(processes), &returned, nullptr)) {
		int count = returned / sizeof(processes[0]);
		for(int i = 0; i < count; i++) {
		}
	}

	return false;
}

int main(int argc, const char* argv[]) {
    auto hDevice = KExploreHelper::OpenDriverHandle();
	if(hDevice == INVALID_HANDLE_VALUE) {
		return Error("Failed to open handle to driver");
	}

	if(!InitKernelFunctions(hDevice)) {
		return Error("Failed to init kernel functions");
	}

	EnumProcesses(hDevice);

	::CloseHandle(hDevice);

	return 0;
}

