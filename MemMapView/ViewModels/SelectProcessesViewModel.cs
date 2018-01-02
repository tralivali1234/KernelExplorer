using Zodiacon.ManagedWindows.Processes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Zodiacon.WPF;
using System.Collections.ObjectModel;
using KernelExplorer.Driver;
using System.Windows.Media;
using Syncfusion.Data;
using Zodiacon.ManagedWindows.Core;

namespace MemMapView.ViewModels {
    sealed class SelectProcessesViewModel : DialogViewModelBase {
        public static readonly BitmapSource DefaultIcon = new BitmapImage(new Uri("/icons/application.ico", UriKind.Relative));
        DriverInterface _driver;

        public SelectProcessesViewModel(Window dialog, DriverInterface driver) : base(dialog) {
            _driver = driver;
            CanExecuteOKCommand = () => SelectedProcesses.Count > 0;
            OKCommand = OKCommand.ObservesProperty(() => SelectedItem);
        }

        ObservableCollection<ProcessViewModel> _processes;
        public IEnumerable<ProcessViewModel> Processes {
            get {
                if (_processes != null)
                    return _processes;
                _processes = new ObservableCollection<ProcessViewModel>();
                var processes = SystemInformation.EnumProcesses();
                foreach (var process in processes.Where(p => p.Id != 0)) {
                    using (var hProcess = _driver.OpenProcessHandle(ProcessAccessMask.QueryInformation, process.Id)) {
                        if (hProcess == null || hProcess.IsInvalid)
                            continue;
                        using (var nativeProcess = NativeProcess.FromHandle(hProcess.DangerousGetHandle(), false)) {
                            var icon = Helpers.ExtractIcon(nativeProcess.TryGetFullImageName()) ?? DefaultIcon;

                            _processes.Add(new ProcessViewModel {
                                Name = process.Name,
                                Id = process.Id,
                                Icon = icon,
                                Session = nativeProcess.SessionId
                            });
                        }
                    }
                }
                return _processes;
            }
        }

        public void Refresh() {
            _processes = null;
            RaisePropertyChanged(nameof(Processes));
        }

        ObservableCollection<object> _selectedProcesses = new ObservableCollection<object>();
        public ObservableCollection<object> SelectedProcesses {
            get => _selectedProcesses;
            set => SetProperty(ref _selectedProcesses, value);
        }

        ProcessViewModel _selectedItem;

        public ICollectionViewAdv View { get; set; }

        string _filterText;
        public string FilterText {
            get => _filterText;
            set {
                if (SetProperty(ref _filterText, value)) {
                    if (string.IsNullOrWhiteSpace(value))
                        View.Filter = null;
                    else {
                        value = value.ToLower();
                        View.Filter = obj => {
                            var process = (ProcessViewModel)obj;
                            return process.Name.ToLower().Contains(value);
                        };
                    }
                    View.RefreshFilter();
                }
            }
        }

        public ProcessViewModel SelectedItem {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }
    }
}
