using JobView.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Zodiacon.WPF;

namespace JobView.ViewModels {
	class MainViewModel : BindableBase, IMainViewModel, IDisposable {
		JobManager _jobManager;
		DriverInterface _driver;
		List<JobObjectViewModel> _rootJobs;
		Dictionary<UIntPtr, JobObjectViewModel> _jobs;
		readonly IUIServices UI;

		public DriverInterface Driver => _driver;

		public JobDetailsViewModel JobDetails { get; }

		public MainViewModel(IUIServices ui) {
			UI = ui;
			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			_jobManager = new JobManager();
			JobDetails = new JobDetailsViewModel(this);

			Init();
		}

		async void Init() {
			try {
				_driver = new DriverInterface();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				Refresh();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			}
			catch (Win32Exception ex) when (ex.NativeErrorCode == 2) {
				// driver not loaded or not installed
				bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
				if (isAdmin) {
					await InstallAndLoadDriverAsync();
					Init();
				}
				else {
					if (UI.MessageBoxService.ShowMessage("Requried driver is not loaded or not installed. Restart application with elevated provileges?",
						App.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK) {
						var startInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location) {
							Verb = "runas"
						};
						Process.Start(startInfo);
						Application.Current.Shutdown();
						return;
					}
				}
			}
			catch (Exception ex) {
				UI.MessageBoxService.ShowMessage($"Error: {ex.Message}", App.Title);
				Application.Current.Shutdown(1);
			}
		}

		private async Task InstallAndLoadDriverAsync() {
			var status = await DriverInterface.LoadDriverAsync("KExplore");
			if (status == null) {
				var ok = await DriverInterface.InstallDriverAsync("KExplore", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\KExplore.sys");
				if (!ok) {
					UI.MessageBoxService.ShowMessage("Failed to install driver. Exiting", App.Title);
					Application.Current.Shutdown(1);
				}
				status = await DriverInterface.LoadDriverAsync("KExplore");
			}
			if (status != ServiceControllerStatus.Running) {
				UI.MessageBoxService.ShowMessage("Failed to start driver. Exiting", App.Title);
				Application.Current.Shutdown(1);
			}
		}

		public void Dispose() {
			_driver.Dispose();
		}

		public IEnumerable<JobObjectViewModel> RootJobs => _rootJobs;

		public ICommand RefreshCommand => new DelegateCommand(async () => await Refresh());

		private bool _IsBusy;

		public bool IsBusy {
			get { return _IsBusy; }
			set { SetProperty(ref _IsBusy, value); }
		}

		private async Task Refresh() {
			_rootJobs = null;
			_jobs = null;
			IsBusy = true;

			await Task.Run(() => {
				_jobManager.BuildJobTree(_driver);
				_jobs = _jobManager.AllJobs.Select(job => new JobObjectViewModel(job)).ToDictionary(job => job.Job.Address);
				_rootJobs = _jobs.Values.Where(job => job.ParentJob == null).ToList();
				foreach (var job in _jobs.Values.Where(job => job.Job.ChildJobs != null)) {
					job.ChildJobs = job.Job.ChildJobs.Select(child => new JobObjectViewModel(child)).ToList();
				}

				//foreach (var job in _jobs.Values.Where(job => job.Job.Parent != null)) {
				//	job.ParentJob = _jobs[job.Job.Parent.Address];
				//}
			});

			RaisePropertyChanged(nameof(RootJobs));
			IsBusy = false;
			SelectedJob = null;
		}

		public JobObjectViewModel GetJobByAddress(UIntPtr address) {
			return _jobs[address];
		}

		private JobObjectViewModel _selectedJob;

		public JobObjectViewModel SelectedJob {
			get { return _selectedJob; }
			set {
				if (SetProperty(ref _selectedJob, value)) {
					JobDetails.Job = value;
					_selectedJob.IsSelected = true;
				}
			}
		}

	}
}
