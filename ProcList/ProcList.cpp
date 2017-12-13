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
	return ::DeviceIoControl(hDevice, static_cast<DWORD>(KExploreIoctls::InitKernelFunctions), 
		&functions, sizeof(functions), nullptr, 0, &returned, nullptr) ? true : false; 
}

bool EnumProcesses(HANDLE hDevice) {
	KernelObjectData processes[2048] = { 0 };
	DWORD returned;
	ACCESS_MASK access = PROCESS_ALL_ACCESS;
	WCHAR path[MAX_PATH] = { 0 };
	if(::DeviceIoControl(hDevice, static_cast<DWORD>(KExploreIoctls::EnumProcesses), &access, sizeof(access), processes, sizeof(processes), &returned, nullptr)) {
		int count = returned / sizeof(processes[0]);

		printf("Total processes: %d\n", count);
		int total = 0;
		for(int i = 0; i < count; i++) {
			auto handle = processes[i].Handle;
			if (::WaitForSingleObject(handle, 0) == WAIT_TIMEOUT) {
				// show only live processes

				DWORD size = MAX_PATH;
				total++;
				BOOL success = ::QueryFullProcessImageName(handle, 0, path, &size);
				printf("Process %p (%d) %ws\n", processes[i].Address, ::GetProcessId(handle), success ? path : L"(Unknown)");
			}
			::CloseHandle(handle);
		}
		printf("Total %d live processes\n", total);
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

