using JobView.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static JobView.NativeMethods;

namespace JobView.ViewModels {
	class JobObjectViewModel : BindableBase, IDisposable {
		public JobObject Job { get; }

		public JobObjectViewModel(JobObject job) {
			Job = job;
			ProcessCount = job.ProcessCount;
		}

		public ulong Address => Job.Address.ToUInt64();
		public IList<JobObjectViewModel> ChildJobs { get; set; }

		public int ChildJobsCount => ChildJobs == null ? 0 : ChildJobs.Count;

		public string Name => Job.Name;

		public string Icon {
			get {
				if (ProcessCount == 0) {
					if (Job.Parent == null)
						return "/icons/rootjob.ico";
					return "/icons/job.ico";
				}
				if (Job.Parent == null)
					return "/icons/rootjob_plus.ico";
				return "/icons/job_plus.ico";
			}
		}

		private bool _isSelected;

		public bool IsSelected {
			get { return _isSelected; }
			set { SetProperty(ref _isSelected, value); }
		}

		private bool _isExpanded;

		public bool IsExpanded {
			get { return _isExpanded; }
			set { SetProperty(ref _isExpanded, value); }
		}


		public JobObject ParentJob => Job.Parent;

		private int _processCount;

		public int ProcessCount {
			get { return _processCount; }
			set {
				bool raiseIconChanged = (value == 0 && _processCount > 0) || (value > 0 && _processCount == 0);
				SetProperty(ref _processCount, value);
				if(raiseIconChanged)
					RaisePropertyChanged(nameof(Icon));
			}
		}

		public int JobId => Job.JobId;

		public unsafe JobObjectInformation JobInformation {
			get {
				JobBasicAccoutingInformation info1;
				QueryInformationJobObject(Job.Handle, JobInformationClass.BasicAccountingInformation, out info1, Marshal.SizeOf<JobBasicAccoutingInformation>());
                JobExtendedLimitInformation info2;
                QueryInformationJobObject(Job.Handle, JobInformationClass.ExtendedLimitInformation, out info2, Marshal.SizeOf<JobExtendedLimitInformation>());

                return new JobObjectInformation {
                    TotalProcesses = info1.TotalProcesses,
                    ActiveProcesses = info1.ActiveProcesses,
                    TerminatedProcesses = info1.TotalTerminatedProcesses,
                    TotalKernelTime = TimeSpan.FromTicks(info1.TotalKernelTime),
                    TotalUserTime = TimeSpan.FromTicks(info1.TotalUserTime),
                    TotalPageFaultCount = info1.TotalPageFaultCount,
                    LimitFlags = info2.BasicLimitInformation.LimitFlags,
                    PerProcessUserTimeLimit = info2.BasicLimitInformation.PerProcessUserTimeLimit,
                    PerJobUserTimeLimit = info2.BasicLimitInformation.PerJobUserTimeLimit,
                    PriorityClass = info2.BasicLimitInformation.PriorityClass,
                    SchedulingClass = info2.BasicLimitInformation.SchedulingClass,
                    ActiveProcessLimit = info2.BasicLimitInformation.ActiveProcessLimit,
                    MinimumWorkingSetSize = info2.BasicLimitInformation.MinimumWorkingSetSize.ToInt64(),
                    MaximumWorkingSetSize = info2.BasicLimitInformation.MaximumWorkingSetSize.ToInt64(),
                    Affinity = info2.BasicLimitInformation.Affinity.ToUInt64(),
                    ProcessMemoryLimit = info2.ProcessMemoryLimit.ToInt64(),
                    JobMemoryLimit = info2.JobMemoryLimit.ToInt64(),
                    PeakProcessMemory = info2.PeakProcessMemoryUsed.ToInt64() >> 10,
                    PeakJobMemory = info2.PeakJobMemoryUsed.ToInt64() >> 10
				};
			}
		}

		public void Dispose() {
			Job.Dispose();
		}
	}
}
