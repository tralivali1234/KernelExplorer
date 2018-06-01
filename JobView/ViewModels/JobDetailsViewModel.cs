using JobView.Models;
using Microsoft.Win32.SafeHandles;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using static JobView.NativeMethods;

namespace JobView.ViewModels {
	class JobDetailsViewModel : BindableBase {
		JobObjectViewModel _job;
		IntPtr? _jobHadle;
		IMainViewModel _mainViewModel;

		public DelegateCommandBase GoToJobCommand { get; }

		public JobDetailsViewModel(IMainViewModel mainViewModel) {
			_mainViewModel = mainViewModel;

			GoToJobCommand = new DelegateCommand<JobObjectViewModel>(async job => {
				await Dispatcher.CurrentDispatcher.InvokeAsync(() => _job.IsExpanded = true);
				_mainViewModel.SelectedJob = job;
			});
		}

		public bool IsJobSelected => _job != null;

		public JobObjectViewModel Job {
			get { return _job; }
			set {
                if (SetProperty(ref _job, value)) {

                    _jobHadle = _job?.Job.Handle;

                    _processes = null;

                    // refresh all properties
                    RaisePropertyChanged(nameof(Name));
                    RaisePropertyChanged(nameof(Address));
                    RaisePropertyChanged(nameof(ChildJobs));
                    RaisePropertyChanged(nameof(IsJobSelected));
                    RaisePropertyChanged(nameof(ParentJob));
                    RaisePropertyChanged(nameof(Processes));
                    RaisePropertyChanged(nameof(JobInformation));
                    RaisePropertyChanged(nameof(JobId));
                    RaisePropertyChanged(nameof(JobLimits));
                }
			}
		}

		public string Name => _job?.Name;
        public ulong? Address => _job?.Address;

		public int? JobId => _job?.JobId;

		public IList<JobObjectViewModel> ChildJobs => _job?.ChildJobs;
		public JobObjectViewModel ParentJob => _job?.ParentJob == null ? null : _mainViewModel.GetJobByAddress(_job.ParentJob.Address);

		ProcessViewModel[] _processes;
		public unsafe ProcessViewModel[] Processes {
			get {
				if (_processes == null) {
					if (_job == null)
						return null;

					JobBasicProcessIdList list;
					if (QueryInformationJobObject(_jobHadle.Value, JobInformationClass.BasicProcessList, out list, Marshal.SizeOf<JobBasicProcessIdList>())) {
						_processes = list.ProcessIds.Take(list.ProcessesInList).Select(id => new ProcessViewModel {
							Id = id.ToInt32(),
							Name = Process.GetProcessById(id.ToInt32())?.ProcessName
						}).OrderBy(process => process.Name).ToArray();
						_job.ProcessCount = _processes.Length;
					}
				}
				return _processes;
			}
		}

		public JobObjectInformation JobInformation => _job?.JobInformation;

        public IEnumerable<object> JobLimits {
            get {
                var info = JobInformation;
                if (info == null)
                    yield break;

                if (info.LimitFlags.HasFlag(JobLimitFlags.ActiveProcesses)) {
                    yield return new {
                        Name = "Active Process Limit:",
                        Value = info.ActiveProcessLimit
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.ProcessTime)) {
                    yield return new {
                        Name = "Process Time Limit:",
                        Value = new TimeSpan(info.PerProcessUserTimeLimit).ToString()
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.JobTime)) {
                    yield return new {
                        Name = "Job Time Limit:",
                        Value = new TimeSpan(info.PerJobUserTimeLimit).ToString()
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.WorkingSet)) {
                    yield return new {
                        Name = "Minimum Working Set:",
                        Value = (info.MinimumWorkingSetSize >> 10).ToString("N0") + " KB"
                    };
                    yield return new {
                        Name = "Maximum Working Set:",
                        Value = (info.MaximumWorkingSetSize >> 10).ToString("N0") + " KB"
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.ProcessMemory)) {
                    yield return new {
                        Name = "Process Commit Limit:",
                        Value = (info.ProcessMemoryLimit >> 10).ToString("N0") + " KB"
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.JobMemory)) {
                    yield return new {
                        Name = "Job Commit Limit:",
                        Value = (info.JobMemoryLimit >> 10).ToString("N0") + " KB"
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.Affinity)) {
                    yield return new {
                        Name = "Affinity:",
                        Value = "0x" + info.Affinity.ToString("X")
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.PriorityClass)) {
                    yield return new {
                        Name = "Priority Class:",
                        Value = info.PriorityClass.ToString()
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.SchedulingClass)) {
                    yield return new {
                        Name = "Scheduling Class:",
                        Value = info.SchedulingClass
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.BreakawayOk)) {
                    yield return new {
                        Name = "Breakaway OK:",
                        Value = "Yes"
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.DieOnUnhandledException)) {
                    yield return new {
                        Name = "Die on Unhandled Exception:",
                        Value = "Yes"
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.KillOnJobClose)) {
                    yield return new {
                        Name = "Kill on Job Close:",
                        Value = "Yes"
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.SlientBreakawayOk)) {
                    yield return new {
                        Name = "Silent Breakway OK:",
                        Value = "Yes"
                    };
                }

                if (info.LimitFlags.HasFlag(JobLimitFlags.JobMemory)) {
                    yield return new {
                        Name = "Job Memory Limit:",
                        Value = "Yes"
                    };
                }

            }
        }
	}
}
