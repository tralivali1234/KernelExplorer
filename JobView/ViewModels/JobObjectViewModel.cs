using JobView.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView.ViewModels {
	class JobObjectViewModel : BindableBase, IDisposable {
		public JobObject Job { get; }

		public JobObjectViewModel(JobObject job) {
			Job = job;
			ProcessCount = job.ProcessCount;
		}

		public ulong Address => Job.Address.ToUInt64();
		public IList<JobObjectViewModel> ChildJobs { get; set; }

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

		public void Dispose() {
			Job.Dispose();
		}
	}
}
