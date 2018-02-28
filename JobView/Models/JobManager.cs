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
using Microsoft.Win32.SafeHandles;

namespace JobView.Models {
	class JobManager : IDisposable {
		List<JobObject> _rootJobs = new List<JobObject>(64);
		Dictionary<UIntPtr, JobObject> _jobs = new Dictionary<UIntPtr, JobObject>(128);
		static StructDescriptor _ejobDescription;
		DriverInterface _driver;
		public JobManager(DriverInterface driver) {
			_driver = driver;
		}

		void BuildEjobDescription() {
			if (_ejobDescription == null) {
				using (var handler = SymbolHandler.Create(SymbolOptions.CaseInsensitive)) {
					var address = handler.LoadSymbolsForModule(@"%systemroot%\system32\ntoskrnl.exe");
					if (address == 0)
						throw new Win32Exception(Marshal.GetLastWin32Error());
					var types = handler.EnumTypes(address, "_ejob");
					Debug.Assert(types != null && types.Count == 1);

					_ejobDescription = handler.BuildStructDescriptor(address, types[0].TypeIndex);
				}
			}
		}

		public IReadOnlyList<JobObject> RootJobs => _rootJobs;

		public ICollection<JobObject> AllJobs => _jobs.Values;

		public unsafe void BuildJobTree() {
			foreach (var job in _jobs.Values)
				job.Dispose();

			_rootJobs.Clear();
			_jobs.Clear();

			if (_ejobDescription == null)
				BuildEjobDescription();

			var jobs = _driver.EnumJobs();
			var bytes = stackalloc byte[512];
			var pString = (UnicodeString*)bytes;
			int status;
			int processCount;
			int jobIdOffset = _ejobDescription.GetOffsetOf("JobId");
			int jobParentOffset = _ejobDescription.GetOffsetOf("ParentJob");
			var jobIdBuffer = new byte[4];

			foreach (var jobObject in jobs) {
				var handle = jobObject.Handle;
				status = NtQueryObject(handle, ObjectInformationClass.ObjectNameInformation, pString, 512);
				processCount = GetJobProcessCount(handle);

				var job = new JobObject(handle, jobObject.Address, status == 0 ? new string(pString->Buffer) : null, processCount);
				if (jobIdOffset >= 0 && _driver.ReadMemory(UIntPtr.Add(jobObject.Address, jobIdOffset), jobIdBuffer)) {
					job.JobId = BitConverter.ToInt32(jobIdBuffer, 0);
				}
				_jobs.Add(jobObject.Address, job);
			}

			if (jobParentOffset >= 0) {
				// nested jobs supported (Windows 8+)

				foreach (var jobObject in jobs) {
					// get parent job

					var parentJobAddress = UIntPtr.Add(jobObject.Address, jobParentOffset);

					var parentPointer = new byte[IntPtr.Size];
					_driver.ReadMemory(parentJobAddress, parentPointer);
					var parentAddress = new UIntPtr(BitConverter.ToUInt64(parentPointer, 0));
					var job = _jobs[jobObject.Address];
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

		private unsafe static int GetJobProcessCount(IntPtr handle) {
			JobBasicProcessIdList list;
			if (QueryInformationJobObject(handle, JobInformationClass.BasicProcessList, out list, Marshal.SizeOf<JobBasicProcessIdList>())) {
				return list.ProcessesInList;
			}
			return 0;
		}

		public void Dispose() {
			foreach (var job in _jobs.Values)
				job.Dispose();
		}
	}
}
