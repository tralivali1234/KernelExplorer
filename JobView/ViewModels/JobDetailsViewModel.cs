using JobView.Models;
using Microsoft.Win32.SafeHandles;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView.ViewModels {
	class JobDetailsViewModel : BindableBase {
		JobObjectViewModel _job;
		SafeFileHandle _jobHadle;
		IMainViewModel _mainViewModel;

		public DelegateCommandBase GoToJobCommand { get; }

		public JobDetailsViewModel(IMainViewModel mainViewModel) {
			_mainViewModel = mainViewModel;

			GoToJobCommand = new DelegateCommand<JobObjectViewModel>(job => {
				_mainViewModel.SelectedJob = job;
//				job.IsSelected = true;
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
					_jobHadle = _mainViewModel.Driver.OpenHandle(_job.Job.Address);
				}

				// refresh all properties
				RaisePropertyChanged(nameof(Name));
				RaisePropertyChanged(nameof(Address));
				RaisePropertyChanged(nameof(ChildJobs));
				RaisePropertyChanged(nameof(IsJobSelected));
				RaisePropertyChanged(nameof(ParentJob));
			}
		}

		public string Name => _job?.Name;
		public ulong? Address => _job?.Address;
		public IList<JobObjectViewModel> ChildJobs => _job?.ChildJobs;
		public JobObjectViewModel ParentJob => _job == null ? null : _mainViewModel.GetJobByAddress(_job.ParentJob.Address);
	
	}
}
