using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemMapView {
    static class Helpers {
        public static BitmapSource ExtractIcon(string filename) {
            if (string.IsNullOrWhiteSpace(filename))
                return null;

            var hIcon = NativeMethods.ExtractIcon(IntPtr.Zero, filename);
            if (hIcon == IntPtr.Zero)
                return null;
            return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());               
        }

        public static string NativePathToDosPath(string nativePath) {
            var drives = DriveInfo.GetDrives();
            var path = new StringBuilder(128);
            foreach (var drive in drives) {
                if (NativeMethods.QueryDosDevice(drive.Name.Substring(0, 2), path, path.Capacity) > 0) {
                    path.Append(@"\");
                    if (nativePath.Substring(0, path.Length) == path.ToString())
                        return drive.Name + nativePath.Substring(path.Length);
                }
            }

            return nativePath;
        }
    }
}
