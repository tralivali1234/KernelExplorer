using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobView {

	[SuppressUnmanagedCodeSecurity]
	static class NativeMethods {
		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct UnicodeString {
			ushort Length;
			ushort MaximumLength;
			public char* Buffer;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct KernelObjectData {
			public UIntPtr Address;
			public IntPtr Handle;
		}

		public enum JobAccessMask {
			Query = 4
		}

		const int DeviceType = 0x22;

		const int MethodBufferred = 0;
		const int MethodInDirect = 1;
		const int MethodOutDirect = 2;
		const int MethodNeither = 3;

		const int FileReadAccess = 1;
		const int FileWriteAccess = 2;
		const int FileAnyAccess = 0;

		static int ControlCode(int DeviceType, int Function, int Method, int Access) =>
			(DeviceType << 16) | (Access << 14) | (Function << 2) | Method;

		public static readonly int KExploreEnumJobs =		ControlCode(DeviceType, 0x903, MethodBufferred, FileAnyAccess);
		public static readonly int KExploreOpenHandle =		ControlCode(DeviceType, 0x905, MethodBufferred, FileAnyAccess);
		public static readonly int KExploreReadMemory =		ControlCode(DeviceType, 0x901, MethodOutDirect, FileAnyAccess);
		public static readonly int KExploreInitFunctions =	ControlCode(DeviceType, 0x90a, MethodBufferred, FileAnyAccess);

		[StructLayout(LayoutKind.Sequential)]
		public struct OpenHandleData {
			public UIntPtr Object;
			public uint AccessMask;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct KernelFunctions {
			public UIntPtr PspGetNextJob;
			public UIntPtr PsGetNextProcess;
		}

		[DllImport("kernel32", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32", SetLastError = true)]
		public unsafe static extern bool DeviceIoControl(SafeFileHandle hDevice, int controlCode,
			ref int access, int inputSize,
			ref KernelObjectData output, int outputSize,
			out int returned, NativeOverlapped* overlapped = null);

		[DllImport("kernel32", SetLastError = true)]
		public unsafe static extern bool DeviceIoControl(SafeFileHandle hDevice, int controlCode,
			ref KernelFunctions functions, int inputSize,
			IntPtr output, int outputSize,
			out int returned, NativeOverlapped* overlapped = null);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool DeviceIoControl(SafeFileHandle hDevice, int controlCode,
			[MarshalAs(UnmanagedType.LPArray)] UIntPtr[] objects, int inputSize,
			IntPtr output, int outputSize,
			out int returned, NativeOverlapped* overlapped = null);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool DeviceIoControl(SafeFileHandle hDevice, int controlCode,
			ref OpenHandleData data, int inputSize,
			out IntPtr handle, int outputSize,
			out int returned, NativeOverlapped* overlapped = null);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool DeviceIoControl(SafeFileHandle hDevice, int controlCode,
			ref UIntPtr address, int inputSize,
			byte[] buffer, int outputSize,
			out int returned, NativeOverlapped* overlapped = null);

		[Flags]
		public enum FileShareMode {
			None = 0,
			Read = 1,
		}

		public enum CreationDisposition {
			OpenExisting = 3
		}

		public enum CreateFileFlags {
			None = 0,
			Overlapped = 0x40000000
		}

		[Flags]
		public enum FileAccessMask : uint {
			GenericRead = 0x80000000,
			GenericWrite = 0x40000000
		}

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern SafeFileHandle CreateFile(string path, FileAccessMask accessMask, FileShareMode shareMode,
			IntPtr sd, CreationDisposition disposition, CreateFileFlags flags, IntPtr hTemplateFile);

		[DllImport("psapi")]
		public static extern bool EnumDeviceDrivers(out UIntPtr address, int size, out int needed);

		public enum ObjectInformationClass {
			ObjectNameInformation = 1
		};

		[DllImport("ntdll")]
		public unsafe static extern int NtQueryObject(IntPtr hObject, ObjectInformationClass infoClass, UnicodeString* pString, int size, int* returnedSize = null);

		public enum JobInformationClass {
			BasicAccountingInformation = 1,
			BasicProcessList = 3,
            BasicLimitInformation = 2,
            BasicUIRestrictions = 4,
            ExtendedLimitInformation = 9,
            GroupInformation = 11,
            CpuRateControlInformation = 15,
            BasicAndIoAccountingInformation = 8,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct JobBasicProcessIdList {
			public int AssignedProcesses;
			public int ProcessesInList;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public IntPtr[] ProcessIds;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct JobBasicAccoutingInformation {
			public long TotalUserTime;
			public long TotalKernelTime;
			public long ThisPeriodTotalUserTime;
			public long ThisPeriodTotalKernelTime;
			public uint TotalPageFaultCount;
			public uint TotalProcesses;
			public uint ActiveProcesses;
			public uint TotalTerminatedProcesses;
		}

        [Flags]
        public enum JobLimitFlags : uint {
            None = 0,
            ActiveProcesses = 8,
            Affinity = 0x10,
            BreakawayOk = 0x800,
            DieOnUnhandledException = 0x400,
            JobMemory = 0x200,
            JobTime = 4,
            KillOnJobClose = 0x2000,
            PreserveJobTime = 0x40,
            PriorityClass = 0x20,
            ProcessMemory = 0x100,
            ProcessTime = 0x2,
            SchedulingClass = 0x80,
            SlientBreakawayOk = 0x1000,
            SubsetAffinity = 0x4000,
            WorkingSet = 1,
            JobMemoryLow = 0x8000,
            JobReadBytes = 0x10000,
            JobWriteBytes = 0x20000,
            RateControl = 0x40000,
            IoRateControl       = 0x80000,
            NetworkRateControl  = 0x100000,
            JobMemoryTotal      = 0x200000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobBasicLimitInformation {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public JobLimitFlags LimitFlags;
            public IntPtr MinimumWorkingSetSize;
            public IntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public ProcessPriorityClass PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IoCounters {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JobExtendedLimitInformation {
            public JobBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public IntPtr ProcessMemoryLimit;
            public IntPtr JobMemoryLimit;
            public IntPtr PeakProcessMemoryUsed;
            public IntPtr PeakJobMemoryUsed;
        }

        [Flags]
		public enum ServiceAccessMask {
			Connect = 0x0001,
			CreateService = 0x0002,
			EnumerateService = 0x0004,
			Lock = 0x0008,
			LockStatus = 0x0010,
			ModifyBootConfig = 0x0020,
			AllAccess = 0xf0000 | Connect | CreateService | EnumerateService | Lock | LockStatus | ModifyBootConfig
		}

		public enum ServiceType {
			KernelDriver = 1
		}

		public enum ServiceStartType {
			DemandStart = 3
		}

		public enum ServiceErrorControl {
			Normal = 1
		}

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool QueryInformationJobObject(IntPtr handle, JobInformationClass infoClass, out JobBasicProcessIdList processList, int size, int* returned = null);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool QueryInformationJobObject(IntPtr handle, JobInformationClass infoClass, out JobBasicAccoutingInformation info, int size, int* returned = null);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public unsafe static extern bool QueryInformationJobObject(IntPtr handle, JobInformationClass infoClass, out JobBasicLimitInformation info, int size, int* returned = null);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public unsafe static extern bool QueryInformationJobObject(IntPtr handle, JobInformationClass infoClass, out JobExtendedLimitInformation info, int size, int* returned = null);

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr OpenSCManager(string machineName, string databaseName, ServiceAccessMask accessMask);

		[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr OpenService(IntPtr hScm, string serviceName, ServiceAccessMask accessMask);

		[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool DeleteService(IntPtr hService);

		[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr CreateService(IntPtr hScm, string serviceName, string displayName, ServiceAccessMask desiredAccess,
			ServiceType serviceType, ServiceStartType startType, ServiceErrorControl errorControl,
			string imagePath, string loadOrderGroup, IntPtr tag,
			string dependencies = null, string serviceStartName = null, string password = null);

		[DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern bool CloseServiceHandle(IntPtr handle);

	}
}
