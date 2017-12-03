using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace BufferManager {
	[Flags]
	enum ProcessAccessMask {
		None = 0,
		VmRead = 0x10,
		VmWrite = 0x20,
		VmOperation = 0x08,
		QueryInformation = 0x400,
	}

    enum PageState : uint {
        Committed = 0x1000,
        Free = 0x10000,
        Reserved = 0x2000
    }

    enum PageProtection : uint {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 1,
        ReadOnly = 2,
        ReadWrite = 4,
        WriteCopy = 8,
    }

    enum PageType : uint {
        Image = 0x1000000,
        Mapped = 0x40000,
        Private = 0x20000
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MemoryBasicInformation {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public PageProtection AllocationProtect;
        public IntPtr RegionSize;
        public PageState State;
        public PageProtection Protect;
        public PageType Type;
    }

    [SuppressUnmanagedCodeSecurity]
	static class NativeMethods {
		[DllImport("kernel32", SetLastError = true)]
		public static extern SafeWaitHandle OpenProcess(ProcessAccessMask accessMask, bool inheritHandle, int pid);

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool ReadProcessMemory(SafeWaitHandle hProcess, IntPtr address, byte[] buffer, int size, out int bytesRead);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr VirtualQueryEx(SafeWaitHandle hProcess, IntPtr address, out MemoryBasicInformation info, int size);

    }
}
