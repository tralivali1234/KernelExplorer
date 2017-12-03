using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zodiacon.ManagedWindows.Processes;

namespace MemMapView.ViewModels {
    sealed class MemoryRegionViewModel {
        MemoryRegion _region;

        public MemoryRegionViewModel(MemoryRegion region, string details) {
            _region = region;
            Details = details;
        }

        public long Address => _region.StartAddress;
        public long Size => _region.Size;
        public PageProtection? Protect => State == PageState.Committed ? _region.Protect : (PageProtection?)null;
        public PageProtection? AllocateProtect => State == PageState.Free ? (PageProtection?)null : _region.AllocateProtect;
        public PageState State => _region.State;
        public PageType? Type => State == PageState.Free ? (PageType?)null : _region.Type;
        public string Details { get; }
    }
}
