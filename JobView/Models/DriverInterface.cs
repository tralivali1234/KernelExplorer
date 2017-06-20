using DebugHelp;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static JobView.NativeMethods;

namespace JobView.Models {
	class DriverInterface : IDisposable {
		SafeFileHandle _hDevice;
		SymbolHandler _symbolHandler;
		UIntPtr _PspGetNextJob;
		ulong _ntoskrnlBase;
		UIntPtr _kernelAddress;

		public DriverInterface() {
			_hDevice = NativeMethods.CreateFile(@"\\.\KExplore", NativeMethods.FileAccessMask.GenericRead | NativeMethods.FileAccessMask.GenericWrite, NativeMethods.FileShareMode.Read,
				IntPtr.Zero, NativeMethods.CreationDisposition.OpenExisting, NativeMethods.CreateFileFlags.None, IntPtr.Zero);
			if (_hDevice.IsInvalid)
				throw new Win32Exception(Marshal.GetLastWin32Error());
			_symbolHandler = SymbolHandler.Create(SymbolOptions.AllowAbsoluteSymbols | SymbolOptions.CaseInsensitive | SymbolOptions.AllowZeroAddress);
			_ntoskrnlBase = _symbolHandler.LoadSymbolsForModule(@"%systemroot%\System32\Ntoskrnl.exe");
			if (_ntoskrnlBase == 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());
			GetKernelAddress(out _kernelAddress);
		}

		public unsafe SafeFileHandle OpenHandle(UIntPtr address, uint accessMask) {
			IntPtr handle = IntPtr.Zero;
			int returned;

			OpenHandleData data;
			data.Object = address;
			data.AccessMask = accessMask;

			NativeMethods.DeviceIoControl(_hDevice, NativeMethods.KExploreOpenHandle,
				ref data, Marshal.SizeOf<OpenHandleData>(), 
				out handle, IntPtr.Size, out returned);
			return new SafeFileHandle(handle, true);
		}

		public static void GetKernelAddress(out UIntPtr address) {
			int needed;
			NativeMethods.EnumDeviceDrivers(out address, UIntPtr.Size, out needed);
		}

		public unsafe UIntPtr[] EnumJobs() {
			if (_PspGetNextJob == UIntPtr.Zero) {
				var symbol = new SymbolInfo();
				symbol.Init();
				if (_symbolHandler.GetSymbolFromName("PspGetNextJob", ref symbol)) {
					var offset = symbol.Address - _ntoskrnlBase;
					Debug.Assert(_kernelAddress != UIntPtr.Zero);
					_PspGetNextJob = new UIntPtr(_kernelAddress.ToUInt64() + offset);
				}
			}

			if (_PspGetNextJob == UIntPtr.Zero)
				return null;

			var addresses = new UIntPtr[2048];       // unlikely to be more... (famous last words)
			int returned;
			if (NativeMethods.DeviceIoControl(_hDevice, NativeMethods.KExploreEnumJobs, 
				ref _PspGetNextJob, UIntPtr.Size, 
				addresses, addresses.Length * IntPtr.Size, 
				out returned)) {
				Array.Resize(ref addresses, returned / IntPtr.Size);
				return addresses;
			}

			return null;
		}

		public unsafe bool ReadMemory(UIntPtr address, byte[] buffer, int size = 0) {
			int returned;
			return NativeMethods.DeviceIoControl(_hDevice, NativeMethods.KExploreReadMemory,
				ref address, IntPtr.Size,
				buffer, size == 0 ? buffer.Length : size,
				out returned);
		}

		public void Dispose() {
			_hDevice.Dispose();
			_symbolHandler.Dispose();
		}
	}
}
