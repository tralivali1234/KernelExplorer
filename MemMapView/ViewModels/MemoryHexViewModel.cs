using BufferManager;
using HexEditControl;
using Microsoft.Win32.SafeHandles;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Zodiacon.WPF;

namespace MemMapView.ViewModels {
    sealed class MemoryHexViewModel : TabItemViewModelBase {
        readonly MemoryRegionViewModel _region;
        readonly SafeWaitHandle _hProcess;
        readonly IUIServices _ui;

        public MemoryHexViewModel(ProcessViewModel process, MemoryRegionViewModel region, SafeWaitHandle hProcess, IUIServices ui) {
            Icon = "/icons/memory-info.ico";
            Text = $"Memory - {process.Name} ({process.Id}): 0x{region.Address:X}";
            _hProcess = hProcess;
            _region = region;
            _ui = ui;
        }

        IHexEdit _editor;
        IProcessBuffer _buffer;

        public long Address => _region.Address;
        public long Size => _region.Size;

        public IHexEdit Editor {
            get => _editor;
            set {
                if (value == null)
                    return;
                _editor = value;
                _editor.AttachToProcess(_hProcess.DangerousGetHandle());
                _buffer = (IProcessBuffer)_editor.BufferManager;
                _buffer.SetRange(_region.Address, _region.Size);
                _editor.StartOffset = _region.Address;
            }
        }

        public bool Is1Byte {
            get => _editor == null || _editor.WordSize == 1;
            set {
                if (value)
                    _editor.WordSize = 1;
            }
        }

        public bool Is2Byte {
            get => _editor?.WordSize == 2;
            set {
                if (value)
                    _editor.WordSize = 2;
            }
        }

        public bool Is4Byte {
            get => _editor?.WordSize == 4;
            set {
                if (value)
                    _editor.WordSize = 4;
            }
        }

        public bool Is8Byte {
            get => _editor?.WordSize == 8;
            set {
                if (value)
                    _editor.WordSize = 8;
            }
        }

        public bool Is16Bytes {
            get => _editor == null || _editor.BytesPerLine == 16;
            set {
                if (value)
                    _editor.BytesPerLine = 16;
            }
        }

        public bool Is24Bytes {
            get => _editor?.BytesPerLine == 24;
            set {
                if (value)
                    _editor.BytesPerLine = 24;
            }
        }

        public bool Is32Bytes {
            get => _editor?.BytesPerLine == 32;
            set {
                if (value)
                    _editor.BytesPerLine = 32;
            }
        }

        public bool Is48Bytes {
            get => _editor?.BytesPerLine == 48;
            set {
                if (value)
                    _editor.BytesPerLine = 48;
            }
        }

        public bool Is64Bytes {
            get => _editor?.BytesPerLine == 64;
            set {
                if (value)
                    _editor.BytesPerLine = 64;
            }
        }

        public ICommand ExportCommand => new DelegateCommand(() => {
            var filename = _ui.FileDialogService.GetFileForSave();
            if (filename != null) {
                try {
                    var count = (int)_editor.BufferManager.Size;
                    File.WriteAllBytes(filename, _editor.BufferManager.GetBytes(0, ref count));
                }
                catch (IOException ex) {
                    _ui.MessageBoxService.ShowMessage($"Error: {ex.Message}", App.Title);
                }
            }
        });

    }
}
