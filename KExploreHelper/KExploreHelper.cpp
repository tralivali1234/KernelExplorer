#include "stdafx.h"
#include "KExploreHelper.h"

void* KExploreHelper::GetKernelBaseAddress() {
    void* kernel;
    DWORD needed;
    if(EnumDeviceDrivers(&kernel, sizeof(kernel), &needed))
        return kernel;
    return nullptr;
}

HANDLE KExploreHelper::OpenDriverHandle(PCWSTR name) {
	WCHAR fullname[64] = L"\\\\.\\";
	::wcscat_s(fullname, name ? name : L"kexplore");
    return ::CreateFile(fullname, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr);
}

bool KExploreHelper::ExtractResourceToFile(HMODULE hModule, PCWSTR resourceName, PCWSTR targetFile) {
    auto hResource = ::FindResource(hModule, resourceName, L"BIN");
    if(hResource == nullptr)
        return false;

    auto hGlobal = ::LoadResource(hModule, hResource);
    auto size = ::SizeofResource(hModule, hResource);
    auto data = ::LockResource(hGlobal);
    if(data == nullptr)
        return false;

    HANDLE hFile = ::CreateFile(targetFile, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, 0, nullptr);
    if(hFile == INVALID_HANDLE_VALUE) {
        return false;
    }

    DWORD written;
    BOOL success = ::WriteFile(hFile, data, size, &written, nullptr);
    ::CloseHandle(hFile);

    return success ? true : false;

}

bool KExploreHelper::LoadDriver(PCWSTR name) {
    auto hScm = ::OpenSCManager(nullptr, nullptr, SC_MANAGER_ALL_ACCESS);
    if(hScm == nullptr)
        return false;

    auto hService = ::OpenService(hScm, name, SERVICE_ALL_ACCESS);
    if(hService == nullptr) {
        ::CloseServiceHandle(hScm);
        return false;
    }

    BOOL success = ::StartService(hService, 0, nullptr);
    ::CloseServiceHandle(hService);
    ::CloseServiceHandle(hScm);

    return success ? true : false;
}

bool KExploreHelper::InstallDriver(PCWSTR name, PCWSTR sysFilePath) {
    auto hScm = ::OpenSCManager(nullptr, nullptr, SC_MANAGER_ALL_ACCESS);
    if(hScm == nullptr)
        return false;
	WCHAR path[MAX_PATH];
	if (::wcschr(sysFilePath, L'\\') == 0) {
		// just file name, use output directory
		if (::GetModuleFileName(nullptr, path, MAX_PATH) > 0) {
			*(::wcsrchr(path, L'\\') + 1) = 0;
			::wcscat_s(path, sysFilePath);
			::wcscat_s(path, L".sys");
			sysFilePath = path;
		}

	}

    auto hService = ::CreateService(hScm, name, name, SERVICE_ALL_ACCESS, 
        SERVICE_KERNEL_DRIVER, SERVICE_DEMAND_START, SERVICE_ERROR_NORMAL, sysFilePath,
        nullptr, nullptr, nullptr, nullptr, nullptr);
    
    bool ok = hService != nullptr;
    
    ::CloseServiceHandle(hService);
    ::CloseServiceHandle(hScm);

    return ok;
}

HANDLE KExploreHelper::OpenDriverHandleAllTheWay(PCWSTR name) {
	auto hDevice = OpenDriverHandle(name);
	if (hDevice != INVALID_HANDLE_VALUE)
		return hDevice;

	if (LoadDriver(name))
		return OpenDriverHandle(name);

	if (InstallDriver(name, name) && LoadDriver(name))
		return OpenDriverHandle(name);

	return INVALID_HANDLE_VALUE;
}

