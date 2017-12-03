using BufferManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexEditControl {
	public interface IHexEdit {
		void LoadFile(string path);
		void AttachToProcess(int pid);
        void AttachToProcess(IntPtr hProcess);
		void CreateNew();
		int WordSize { get; set; }
		int BytesPerLine { get; set; }
		IBufferEditor Editor { get; }
		IBufferManager BufferManager { get; }
        long StartOffset { get; set; }
    }
}
