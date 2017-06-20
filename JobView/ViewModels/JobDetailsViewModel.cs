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
using static JobView.NativeMethods;

namespace JobView.ViewModels {
	class JobDetailsViewModel : BindableBase {
		JobObjectViewModel _job;
		SafeFileHandle _jobHadle;
		IMainViewModel _mainViewModel;

		public DelegateCommandBase GoToJobCommand { get; }

		public JobDetailsViewModel(IMainViewModel mainViewModel) {
			_mainViewModel = mainViewModel;

			GoToJobCommand = new DelegateCommand<JobObjectViewModel>(job => {
				job.IsSelected = true;
			});
		}

		public bool IsJobSelected => _job != null;

		public JobObjectViewModel Job {
			get { return _job; }
			set {
				if (_job != null) {
					_jobHadle.Dispose();
					_jobHadle = null;
				}
				SetProperty(ref _job, value);

				if (_job != null) {
					// open a handle to the job
					_jobHadle = _mainViewModel.Driver.OpenHandle(_job.Job.Address, (int)JobAccessMask.Query);

				}

				_processes = null;

				// refresh all properties
				RaisePropertyChanged(nameof(Name));
				RaisePropertyChanged(nameof(Address));
				RaisePropertyChanged(nameof(ChildJobs));
				RaisePropertyChanged(nameof(IsJobSelected));
				RaisePropertyChanged(nameof(ParentJob));
				RaisePropertyChanged(nameof(Processes));
			}
		}

		public string Name => _job?.Name;
		public ulong? Address => _job?.Address;
		public IList<JobObjectViewModel> ChildJobs => _job?.ChildJobs;
		public JobObjectViewModel ParentJob => _job == null || _job.ParentJob == null ? null : _mainViewModel.GetJobByAddress(_job.ParentJob.Address);

		ProcessViewModel[] _processes;
		public unsafe ProcessViewModel[] Processes {
			get {
				if (_processes == null) {
					if (_job == null)
						return null;

					BasicProcessIdList list;
					if (QueryInformationJobObject(_jobHadle.DangerousGetHandle(), JobInformationClass.BasicProcessList, out list, Marshal.SizeOf<BasicProcessIdList>())) {
						_processes = list.ProcessIds.Take(list.ProcessesInList).Select(id => new ProcessViewModel {
							Id = id.ToInt32(),
							Name = Process.GetProcessById(id.ToInt32())?.ProcessName
						}).ToArray();
					}
				}
				return _processes;
			}
		}
	}
}
