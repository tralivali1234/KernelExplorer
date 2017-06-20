using JobView.Models;
using JobView.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView {
	interface IMainViewModel {
		JobObjectViewModel SelectedJob { get; set; }
		DriverInterface Driver { get; }
		JobObjectViewModel GetJobByAddress(UIntPtr address);
	}
}
