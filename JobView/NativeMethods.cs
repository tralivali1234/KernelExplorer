using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
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

		public static readonly int KExploreEnumJobs = ControlCode(DeviceType, 0x903, MethodBufferred, FileReadAccess);
		public static readonly int KExploreOpenHandle = ControlCode(DeviceType, 0x905, MethodBufferred, FileReadAccess);
		public static readonly int KExploreReadMemory = ControlCode(DeviceType, 0x901, MethodOutDirect, FileReadAccess);

		[StructLayout(LayoutKind.Sequential)]
		public struct OpenHandleData {
			public UIntPtr Object;
			public uint AccessMask;
		};

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool DeviceIoControl(SafeFileHandle hDevice, int controlCode,
			ref UIntPtr PspGetNextJob, int inputSize,
			UIntPtr[] output, int outputSize,
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
			BasicProcessList = 3
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

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool QueryInformationJobObject(IntPtr handle, JobInformationClass infoClass, out JobBasicProcessIdList processList, int size, int* returned = null);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		public unsafe static extern bool QueryInformationJobObject(IntPtr handle, JobInformationClass infoClass, out JobBasicAccoutingInformation info, int size, int* returned = null);
	}
}
