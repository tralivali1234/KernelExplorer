using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace MemMapView {
    [SuppressUnmanagedCodeSecurity]
    static class NativeMethods {
        [DllImport("Shell32", CharSet = CharSet.Unicode)]
        public static extern IntPtr ExtractIcon(IntPtr hInstance, string filename, int iconIndex = 0);

        [DllImport("psapi", CharSet = CharSet.Unicode)]
        public static extern int GetMappedFileName(SafeWaitHandle hProcess, IntPtr address, StringBuilder filename, int size);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern int QueryDosDevice(string deviceName, StringBuilder targetPath, int maxSize);
    }
}
