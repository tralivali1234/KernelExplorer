using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BufferManager {
    public interface IProcessBuffer {
        void SetRange(long startAddress, long size);
        IReadOnlyDictionary<long, long> MemoryMap { get; }
    }
}
