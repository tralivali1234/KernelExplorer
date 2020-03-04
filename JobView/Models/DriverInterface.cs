using DebugHelp;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using static JobView.NativeMethods;

namespace JobView.Models {
	class DriverInterface : IDisposable {
		SafeFileHandle _hDevice;
		SymbolHandler _symbolHandler;
		ulong _ntoskrnlBase;
		UIntPtr _kernelAddress;
		static bool _initialized;

		public const string DriverName = "KExplore";
		 
		public DriverInterface() {
			_hDevice = CreateFile(@"\\.\" + DriverName, FileAccessMask.GenericRead | FileAccessMask.GenericWrite, FileShareMode.Read,
				IntPtr.Zero, CreationDisposition.OpenExisting, CreateFileFlags.None, IntPtr.Zero);
			if (_hDevice.IsInvalid) {
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			_symbolHandler = SymbolHandler.Create(SymbolOptions.AllowAbsoluteSymbols | SymbolOptions.CaseInsensitive | SymbolOptions.AllowZeroAddress);
			_ntoskrnlBase = _symbolHandler.LoadSymbolsForModule(@"%systemroot%\System32\Ntoskrnl.exe");
			if (_ntoskrnlBase == 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());
			GetKernelAddress(out _kernelAddress);

		}

		public static void GetKernelAddress(out UIntPtr address) {
			int needed;
			EnumDeviceDrivers(out address, UIntPtr.Size, out needed);
		}

		public unsafe KernelObjectData[] EnumJobs() {
			int returned;
			if (!_initialized) {
				var symbol = new SymbolInfo();
				symbol.Init();
				if (_symbolHandler.GetSymbolFromName("PspGetNextJob", ref symbol)) {
					var offset = symbol.Address - _ntoskrnlBase;
					Debug.Assert(_kernelAddress != UIntPtr.Zero);

					var functions = new KernelFunctions {
						PspGetNextJob = new UIntPtr(_kernelAddress.ToUInt64() + offset)
					};
					_initialized = DeviceIoControl(_hDevice, KExploreInitFunctions, ref functions, Marshal.SizeOf<KernelFunctions>(), 
						IntPtr.Zero, 0, out returned);
				}
			}
			if (!_initialized)
				throw new InvalidOperationException("Failed to locate symbols");

			var jobs = new KernelObjectData[2048];       // unlikely to be more... (famous last words)
			var access = (int)JobAccessMask.Query;
			if (DeviceIoControl(_hDevice, KExploreEnumJobs,
				ref access, sizeof(int),
				ref jobs[0], jobs.Length * Marshal.SizeOf<KernelObjectData>(), out returned)) {
				Array.Resize(ref jobs, returned / Marshal.SizeOf<KernelObjectData>());
				return jobs;
			}

			return null;
		}

		public unsafe bool ReadMemory(UIntPtr address, byte[] buffer, int size = 0) {
			return DeviceIoControl(_hDevice, KExploreReadMemory,
				ref address, IntPtr.Size,
				buffer, size == 0 ? buffer.Length : size,
				out var _);
		}

		public static async Task<ServiceControllerStatus?> LoadDriverAsync(string drivername) {
            var controller = new ServiceController(drivername);
            try {
                if (controller == null)
                    return null;

                if (controller.Status == ServiceControllerStatus.Running)
                    return controller.Status;
            }
            catch (Exception) {
                return null;
            }

			try {
				controller.Start();
				await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5)));
			}
			catch (Exception ex) {
                Debug.WriteLine(ex.Message);
			}
			return controller.Status;
		}
		 
		public static async Task<bool> InstallDriverAsync(string drivername, string driverpath) {
			await Task.Run(() => {
				var hScm = OpenSCManager(null, null, ServiceAccessMask.AllAccess);
				if (hScm == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				// if driver exists, delete it first
				var hService = OpenService(hScm, drivername, ServiceAccessMask.AllAccess);
				if (hService != IntPtr.Zero) {
					// delete service
					DeleteService(hService);
					CloseServiceHandle(hService);
				}

				hService = CreateService(hScm, drivername, drivername, ServiceAccessMask.AllAccess,
					NativeMethods.ServiceType.KernelDriver, ServiceStartType.DemandStart, ServiceErrorControl.Normal,
					driverpath, null, IntPtr.Zero);
				CloseServiceHandle(hScm);

				if (hService == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error());
				CloseServiceHandle(hService);
			});

			return true;
		}

		public void Dispose() {
			_hDevice.Dispose();
			_symbolHandler.Dispose();
		}
	}
}
