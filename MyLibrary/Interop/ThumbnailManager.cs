using System.Drawing;
using System.Runtime.InteropServices;

namespace MyLibrary.Interop
{
    public static class ThumbnailManager
    {
        public static Icon GetFileIcon(string name, bool smallSize, bool linkOverlay)
        {
            var shfi = new NativeMethods.Shfileinfo();
            var flags = NativeMethods.ShgfiIcon | NativeMethods.ShgfiUsefileattributes;
            if (linkOverlay)
            {
                flags += NativeMethods.ShgfiLinkoverlay;
            }

            /* Check the size specified for return. */
            if (smallSize)
            {
                flags += NativeMethods.ShgfiSmallicon;
            }
            else
            {
                flags += NativeMethods.ShgfiLargeicon;
            }
            NativeMethods.SHGetFileInfo(name,
                NativeMethods.FileAttributeNormal,
                ref shfi,
                (uint)Marshal.SizeOf(shfi),
                flags);

            // Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
            var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
            NativeMethods.DestroyIcon(shfi.hIcon);     // Cleanup
            return icon;
        }
    }
}
