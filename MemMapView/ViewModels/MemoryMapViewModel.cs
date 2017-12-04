using KernelExplorer.Driver;
using Microsoft.Win32.SafeHandles;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Zodiacon.ManagedWindows.Processes;
using Zodiacon.WPF;

namespace MemMapView.ViewModels {
    class MemoryMapViewModel : TabItemViewModelBase, IDisposable {
        readonly MemoryMap _memoryMap;
        readonly ProcessViewModel _process;
        readonly SafeWaitHandle _hProcess;
        readonly IList<TabItemViewModelBase> _tabs;
        readonly IUIServices _ui;

        public MemoryMapViewModel(ProcessViewModel process, DriverInterface driver, IList<TabItemViewModelBase> tabs, IUIServices ui) {
            _process = process;
            _tabs = tabs;
            _ui = ui;
            _hProcess = driver.OpenProcessHandle(ProcessAccessMask.AllAccess, process.Id);
            if (_hProcess == null)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            _memoryMap = new MemoryMap(_hProcess);

            Text = $"Map - {process.Name} ({process.Id})";
            Icon = "/icons/memory.ico";
        }

        IEnumerable<MemoryRegionViewModel> _regions;

        public IEnumerable<MemoryRegionViewModel> Regions => _regions ?? (_regions = _memoryMap.Select(region => new MemoryRegionViewModel(region, BuildDetails(region))).ToArray());

        StringBuilder _details = new StringBuilder(512);
        private string BuildDetails(MemoryRegion region) {
            if (region.Type == PageType.Image) {
                if (NativeMethods.GetMappedFileName(_hProcess, new IntPtr(region.StartAddress), _details, _details.Capacity) > 0)
                    return Helpers.NativePathToDosPath(_details.ToString());
            }

            return string.Empty;
        }

        public void Dispose() {
            _memoryMap.Dispose();
        }

        public void Refresh() {
            _regions = null;
            RaisePropertyChanged(nameof(Regions));
        }

        public new ICommand RefreshCommand => new DelegateCommand(() => Refresh());

        MemoryRegionViewModel _selectedItem;
        public MemoryRegionViewModel SelectedItem {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public new ICommand ViewMemoryCommand => new DelegateCommand(() => {
            var vm = new MemoryHexViewModel(_process, SelectedItem, _hProcess, _ui);
            _tabs.Add(vm);
        }, () => SelectedItem != null && SelectedItem.State == PageState.Committed && IsReadable(SelectedItem.Protect)).ObservesProperty(() => SelectedItem);

        private bool IsReadable(PageProtection? protect) {
            if (protect == null || protect.Value.HasFlag(PageProtection.Guard))
                return false;
            protect = protect & (~PageProtection.Guard);

            return protect == PageProtection.ExecuteRead || protect == PageProtection.ExecuteReadWrite ||
                protect == PageProtection.ReadOnly || protect == PageProtection.ReadWrite || protect == PageProtection.WriteCopy;
        }
    }
}
