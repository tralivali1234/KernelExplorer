using JobView.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView.ViewModels {
	class JobObjectViewModel : BindableBase {
		public JobObject Job { get; }

		public JobObjectViewModel(JobObject job) {
			Job = job;
		}

		public ulong Address => Job.Address.ToUInt64();
		public IList<JobObjectViewModel> ChildJobs { get; set; }

		public string Name => Job.Name;

		public string Icon => Job.Parent == null ? "/icons/rootjob.ico" : "/icons/job.ico";

		private bool _isSelected;

		public bool IsSelected {
			get { return _isSelected; }
			set { SetProperty(ref _isSelected, value); }
		}

		public JobObject ParentJob => Job.Parent;

	}
}
