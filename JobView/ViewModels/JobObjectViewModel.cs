using JobView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView.ViewModels {
	class JobObjectViewModel {
		public JobObject Job { get; }

		public JobObjectViewModel(JobObject job) {
			Job = job;
		}

		public ulong Address => Job.Address.ToUInt64();
		public IEnumerable<JobObjectViewModel> ChildJobs => Job.ChildJobs?.Select(job => new JobObjectViewModel(job));
		public string Name => Job.Name;

		public string Icon => "/icons/job.ico";
	}
}
