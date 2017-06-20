using DebugHelp;
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
	class JobManager {
		List<JobObject> _rootJobs = new List<JobObject>(64);
		Dictionary<UIntPtr, JobObject> _jobs = new Dictionary<UIntPtr, JobObject>(128);
		static int _parentJobOffset;

		static int ParentJobOffset {
			get {
				if (_parentJobOffset == 0) {
					using (var handler = SymbolHandler.Create(SymbolOptions.CaseInsensitive)) {
						var address = handler.LoadSymbolsForModule(@"%systemroot%\system32\ntoskrnl.exe");
						if (address == 0)
							throw new Win32Exception(Marshal.GetLastWin32Error());
						var types = handler.EnumTypes(address, "_ejob");
						Debug.Assert(types != null && types.Count == 1);

						var ejob = handler.BuildStructDescription(address, types[0]);
						_parentJobOffset = ejob.GetOffsetOf("ParentJob");
					}
				}
				return _parentJobOffset;
			}
		}

		public IReadOnlyList<JobObject> RootJobs => _rootJobs;

		public ICollection<JobObject> AllJobs => _jobs.Values;

		public unsafe void BuildJobTree(DriverInterface driver) {
			_rootJobs.Clear();
			_jobs.Clear();

			var jobAddresses = driver.EnumJobs();
			var bytes = stackalloc byte[512];
			var pString = (UnicodeString*)bytes;
			int status;
			foreach (var address in jobAddresses) {
				using (var handle = driver.OpenHandle(address, (int)JobAccessMask.Query)) {	
					status = NtQueryObject(handle.DangerousGetHandle(), ObjectInformationClass.ObjectNameInformation, pString, 512);
				}
				var job = new JobObject(address, status == 0 ? new string(pString->Buffer) : null);
				_jobs.Add(address, job);		
			}

			foreach (var address in jobAddresses) {
				// get parent job

				var parentJobAddress = UIntPtr.Add(address, ParentJobOffset);
				var parentPointer = new byte[IntPtr.Size];
				driver.ReadMemory(parentJobAddress, parentPointer);
				var parentAddress = new UIntPtr(BitConverter.ToUInt64(parentPointer, 0));
				var job = _jobs[address];
				if (parentAddress != UIntPtr.Zero) {
					var parentJob = _jobs[parentAddress];
					job.Parent = parentJob;
					parentJob.AddChildJob(job);
				}
				else {
					_rootJobs.Add(job);
				}
			}

		}
	}
}
