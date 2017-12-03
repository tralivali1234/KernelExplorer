using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BufferManager {
    sealed class ProcessBuffer : BufferBase, IBufferOperations, IProcessBuffer {
        readonly SafeWaitHandle _hProcess;
        //readonly byte[] _buffer = new byte[16 << 20];   // 16 MB
        readonly SortedDictionary<long, long> _processMap = new SortedDictionary<long, long>();

        public ProcessBuffer(int pid) {
            var flags = ProcessAccessMask.QueryInformation | ProcessAccessMask.VmRead | ProcessAccessMask.VmOperation;
            _hProcess = NativeMethods.OpenProcess(flags | ProcessAccessMask.VmWrite, false, pid);
            if (_hProcess.IsInvalid) {
                _hProcess = NativeMethods.OpenProcess(flags, false, pid);
                if (_hProcess.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            MapProcess();
        }

        public ProcessBuffer(IntPtr handle) {
            _hProcess = new SafeWaitHandle(handle, false);
            MapProcess();
        }

        public long StartAddress { get; private set; }

        public IReadOnlyDictionary<long, long> MemoryMap => (IReadOnlyDictionary<long, long>)_processMap;

        private void MapProcess() {
            _processMap.Clear();
            long offset = 0;
            for (; ; ) {
                var size = NativeMethods.VirtualQueryEx(_hProcess, new IntPtr(offset), out var info, Marshal.SizeOf<MemoryBasicInformation>());
                if (size.ToInt64() == 0)
                    break;
                if (size.ToInt64() == 0 || (info.Protect != PageProtection.ReadOnly && info.Protect != PageProtection.ReadWrite) || info.State != PageState.Committed) {
                    offset += info.RegionSize.ToInt64();
                    continue;
                }
                _processMap.Add(offset, info.RegionSize.ToInt64());
                offset += info.RegionSize.ToInt64();
            }
            _size = _processMap.Sum(pair => pair.Value);
        }

        long _size;
        public override long Size => _size;


        public override void Delete(long offset, int count) {
            throw new NotImplementedException();
        }

        public override byte[] GetBytes(long offset, ref int count) {
            var buffer = new byte[count];
            if (NativeMethods.ReadProcessMemory(_hProcess, new IntPtr(offset + StartAddress), buffer, count, out count))
                return buffer;

            return null;
        }

        public override void Insert(long offset, byte[] data) {
            throw new NotImplementedException();
        }

        public override void SetData(long offset, byte[] data) {
            throw new NotImplementedException();
        }

        public override void Dispose() {
            _hProcess.Close();
        }

        public void InsertData(long offset, byte[] data) {
            throw new NotImplementedException();
        }

        public void DeleteRange(long offset, int count) {
            throw new NotImplementedException();
        }

        public void SetBytes(long offset, byte[] data) {
            throw new NotImplementedException();
        }

        public void SetRange(long startAddress, long size) {
            StartAddress = startAddress;
            _size = size;
            OnSizeChanged();
        }
    }
}
