using DebugHelp;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Zodiacon.ManagedWindows.Processes;
using static KernelExplorer.Driver.NativeMethods;

namespace KernelExplorer.Driver {
	public sealed class DriverInterface : IDisposable {
		SafeFileHandle _hDevice;
		SymbolHandler _symbolHandler;
		UIntPtr _PspGetNextJob;
		ulong _ntoskrnlBase;
		UIntPtr _kernelAddress;

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
			if (DeviceIoControl(_hDevice, KExploreEnumJobs,
				ref _PspGetNextJob, UIntPtr.Size,
				addresses, addresses.Length * IntPtr.Size,
				out returned)) {
				Array.Resize(ref addresses, returned / IntPtr.Size);
				return addresses;
			}

			return null;
		}

        public unsafe SafeWaitHandle OpenProcessHandle(ProcessAccessMask accessMask, int pid) {
            var data = new OpenProcessData {
                ProcessId = pid,
                AccessMask = accessMask
            };

            if (!DeviceIoControl(_hDevice, KExploreOpenProcessHandle, &data, Marshal.SizeOf<OpenProcessData>(), out IntPtr handle, IntPtr.Size, out var returned)) {
                return null;
            }

            return new SafeWaitHandle(handle, true);
        }

        public unsafe SafeWaitHandle OpenThreadHandle(ThreadAccessMask accessMask, int tid) {
            var data = new OpenThreadData {
                ThreadId = tid,
                AccessMask = accessMask
            };

            if (!DeviceIoControl(_hDevice, KExploreOpenThreadHandle, &data, Marshal.SizeOf<OpenThreadData>(), out IntPtr handle, IntPtr.Size, out var returned)) {
                return null;
            }

            return new SafeWaitHandle(handle, true);
        }

        public unsafe bool ReadMemory(UIntPtr address, byte[] buffer, int size = 0) {
			int returned;
			return DeviceIoControl(_hDevice, KExploreReadMemory,
				ref address, IntPtr.Size,
				buffer, size == 0 ? buffer.Length : size,
				out returned);
		}

		public static async Task<ServiceControllerStatus?> LoadDriverAsync(string drivername) {
			var controller = new ServiceController(drivername);
            try {
                if (controller == null || controller.ServiceHandle.IsInvalid)
                    return null;
            }
            catch {
                return null;
            }

			if (controller.Status == ServiceControllerStatus.Running)
				return controller.Status;

			try {
				controller.Start();
				await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5)));
			}
			catch (Exception) {
			}
			return controller.Status;
		}

        public unsafe IntPtr OpenObject(UIntPtr objectAddress, uint accessMask) {
            OpenHandleData data;
            data.AccessMask = accessMask;
            data.Object = objectAddress;

            return DeviceIoControl(_hDevice, KExploreOpenHandle, ref data, sizeof(OpenHandleData), out var handle, sizeof(IntPtr), out var returned) ? handle : IntPtr.Zero;
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
