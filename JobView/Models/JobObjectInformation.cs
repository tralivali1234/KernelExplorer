using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView.Models {
	class JobObjectInformation {
		public uint TotalProcesses { get; set; }
		public uint ActiveProcesses { get; set; }
		public uint TerminatedProcesses { get; set; }
		public uint TotalPageFaultCount { get; set; }
		public TimeSpan TotalUserTime { get; set; }
		public TimeSpan TotalKernelTime { get; set; }
	}
}
