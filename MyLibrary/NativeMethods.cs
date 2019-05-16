using System;
using System.Runtime.InteropServices;

namespace MyLibrary
{
    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int SetClipboardViewer(int hWndNewViewer);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("User32.dll")]
        internal static extern int DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Shfileinfo
        {
            private const int Namesize = 80;
            public readonly IntPtr hIcon;
            private readonly int iIcon;
            private readonly uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            private readonly string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Namesize)]
            private readonly string szTypeName;
        };

        internal const uint ShgfiIcon = 0x000000100;     // get icon
        internal const uint ShgfiLinkoverlay = 0x000008000;     // put a link overlay on icon
        internal const uint ShgfiLargeicon = 0x000000000;     // get large icon
        internal const uint ShgfiSmallicon = 0x000000001;     // get small icon
        internal const uint ShgfiUsefileattributes = 0x000000010;     // use passed dwFileAttribute
        internal const uint FileAttributeNormal = 0x00000080;

        [DllImport("shell32.dll")]
        internal static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref Shfileinfo psfi,
            uint cbFileInfo,
            uint uFlags);
    }
}
