using KernelExplorer.Driver;
using MemMapView.Views;
using Prism.Commands;
using Prism.Mvvm;
using Syncfusion.Windows.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Zodiacon.WPF;

namespace MemMapView.ViewModels {
    class MainViewModel : BindableBase {
        ObservableCollection<TabItemViewModelBase> _tabItems = new ObservableCollection<TabItemViewModelBase>();
        DriverInterface _driver;

        public IList<TabItemViewModelBase> TabItems => _tabItems;

        public readonly IUIServices UI;
        public MainViewModel(IUIServices ui) {
            UI = ui;
        }

        public ICommand InitCommand => new DelegateCommand(async () => await Init());

        async Task Init() {
            try {
                _driver = new DriverInterface();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2) {
                // driver not loaded or not installed
                bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                if (isAdmin) {
                    await InstallAndLoadDriverAsync();
                    await Init();
                }
                else {
                    if (UI.MessageBoxService.ShowMessage("Requried driver is not loaded or not installed. Restart application with elevated provileges?",
                        App.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK) {
                        var startInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location) {
                            Verb = "runas"
                        };
                        Process.Start(startInfo);
                    }
                    Application.Current.Shutdown();
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

        public ICommand OpenProcessCommand => new DelegateCommand(() => {
            var dlg = UI.DialogService.CreateDialog<SelectProcessesViewModel, SelectProcessesDialog>(_driver);
            if (dlg.ShowDialog() == true) {
                foreach (var process in dlg.SelectedProcesses.Cast<ProcessViewModel>()) {
                    var vm = new MemoryMapViewModel(process, _driver, TabItems, UI);
                    TabItems.Add(vm);
                }
            }
        });

        public ICommand EmptyCommand => new DelegateCommand(() => { }, () => false);

        TabItemViewModelBase _selectedTab;
        public TabItemViewModelBase SelectedTab {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public ICommand TabClosedCommand => new DelegateCommand<CloseTabEventArgs>(args => {
            var item = (TabItemViewModelBase)args.TargetTabItem.DataContext;
            if (item is IDisposable disposable)
                disposable.Dispose();
            TabItems.Remove(item);
        });
    }
}
