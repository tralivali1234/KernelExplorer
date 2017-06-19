using JobView.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobView.ViewModels {
	class MainViewModel : BindableBase, IDisposable {
		JobManager _jobManager;
		DriverInterface _driver;

		public MainViewModel() {
			Init();
		}

		void Init() {
			_jobManager = new JobManager();
			_driver = new DriverInterface();
			_jobManager.BuildJobTree(_driver);
		}

		public void Dispose() {
			_driver.Dispose();
		}

		IEnumerable<JobObjectViewModel> _rootJobs;
		public IEnumerable<JobObjectViewModel> RootJobs {
			get {
				if (_rootJobs == null)
					_rootJobs = _jobManager.RootJobs.Select(job => new JobObjectViewModel(job));
				return _rootJobs;
			}
		}
	}
}
