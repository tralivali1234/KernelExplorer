using System;
using System.Diagnostics;
using static JobView.NativeMethods;

namespace JobView.Models {
    class JobObjectInformation {
		public uint TotalProcesses { get; set; }
		public uint ActiveProcesses { get; set; }
		public uint TerminatedProcesses { get; set; }
		public uint TotalPageFaultCount { get; set; }
		public TimeSpan TotalUserTime { get; set; }
		public TimeSpan TotalKernelTime { get; set; }
        public JobLimitFlags LimitFlags { get; set; }
        public long PerProcessUserTimeLimit { get; set; }
        public long PerJobUserTimeLimit { get; set; }
        public long MinimumWorkingSetSize { get; set; }
        public long MaximumWorkingSetSize { get; set; }
        public uint ActiveProcessLimit { get; set; }
        public ulong Affinity { get; set; }
        public ProcessPriorityClass PriorityClass { get; set; }
        public uint SchedulingClass { get; set; }
        public long JobMemoryLimit { get; internal set; }
        public long ProcessMemoryLimit { get; internal set; }
        public long PeakProcessMemory { get; internal set; }
        public long PeakJobMemory { get; internal set; }

    }
}
