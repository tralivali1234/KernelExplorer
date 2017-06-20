using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView.Models {
	[DebuggerDisplay("0x{Address,x} ({Name})")]
	class JobObject {
		List<JobObject> _childJobs;
		
		public UIntPtr Address { get; }
		public string Name { get; }
		public int ProcessCount { get; }

		public JobObject Parent { get; internal set; }

		public JobObject(UIntPtr address, string name, int processCount) {
			Address = address;
			Name = name;
			ProcessCount = processCount;
		}

		public void AddChildJob(JobObject job) {
			if (_childJobs == null)
				_childJobs = new List<JobObject>(2);
			_childJobs.Add(job);
		}

		public IReadOnlyList<JobObject> ChildJobs => _childJobs;

	}
}
