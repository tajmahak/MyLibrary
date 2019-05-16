using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MyLibrary.Interop
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

        [DllImport("user32.dll")]
        internal static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern int SHGetMalloc(ref IMalloc ppMalloc);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern int SHGetDesktopFolder(ref IShellFolder ppshf);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        internal static extern int SHGetPathFromIDList(IntPtr pidl, StringBuilder pszPath);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        internal static extern int DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll")]
        internal static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref Shfileinfo psfi, uint cbFileInfo, uint uFlags);

        #region const

        internal const uint ShgfiIcon = 0x000000100;     // get icon
        internal const uint ShgfiLinkoverlay = 0x000008000;     // put a link overlay on icon
        internal const uint ShgfiLargeicon = 0x000000000;     // get large icon
        internal const uint ShgfiSmallicon = 0x000000001;     // get small icon
        internal const uint ShgfiUsefileattributes = 0x000000010;     // use passed dwFileAttribute
        internal const uint FileAttributeNormal = 0x00000080;

        #endregion

        #region struct

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

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Auto)]
        internal struct STRRET_CSTR
        {
            public ESTRRET uType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 520)]
            public byte[] cStr;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Auto)]
        internal struct STRRET_ANY
        {
            [FieldOffset(0)]
            public ESTRRET uType;
            [FieldOffset(4)]
            public IntPtr pOLEString;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SIZE
        {
            public int cx;
            public int cy;
        }

        #endregion

        #region interface

        [ComImport]
        [Guid("00000002-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IMalloc
        {

            [PreserveSig()]
            IntPtr Alloc(int cb);

            [PreserveSig()]
            IntPtr Realloc(IntPtr pv, int cb);

            [PreserveSig()]
            void Free(IntPtr pv);

            [PreserveSig()]
            int GetSize(IntPtr pv);

            [PreserveSig()]
            int DidAlloc(IntPtr pv);

            [PreserveSig()]
            void HeapMinimize();
        }

        [ComImport]
        [Guid("000214F2-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IEnumIDList
        {

            [PreserveSig()]
            int Next(int celt, ref IntPtr rgelt, ref int pceltFetched);

            void Skip(int celt);

            void Reset();

            void Clone(ref IEnumIDList ppenum);
        }

        [ComImport]
        [Guid("000214E6-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellFolder
        {

            void ParseDisplayName(IntPtr hwndOwner, IntPtr pbcReserved,
              [MarshalAs(UnmanagedType.LPWStr)]string lpszDisplayName,
              ref int pchEaten, ref IntPtr ppidl, ref int pdwAttributes);

            void EnumObjects(IntPtr hwndOwner,
              [MarshalAs(UnmanagedType.U4)]ESHCONTF grfFlags,
              ref IEnumIDList ppenumIDList);

            void BindToObject(IntPtr pidl, IntPtr pbcReserved, ref Guid riid,
              ref IShellFolder ppvOut);

            void BindToStorage(IntPtr pidl, IntPtr pbcReserved, ref Guid riid, IntPtr ppvObj);

            [PreserveSig()]
            int CompareIDs(IntPtr lParam, IntPtr pidl1, IntPtr pidl2);

            void CreateViewObject(IntPtr hwndOwner, ref Guid riid,
              IntPtr ppvOut);

            void GetAttributesOf(int cidl, IntPtr apidl,
              [MarshalAs(UnmanagedType.U4)]ref ESFGAO rgfInOut);

            void GetUIObjectOf(IntPtr hwndOwner, int cidl, ref IntPtr apidl, ref Guid riid, ref int prgfInOut, ref IUnknown ppvOut);

            void GetDisplayNameOf(IntPtr pidl,
              [MarshalAs(UnmanagedType.U4)]ESHGDN uFlags,
              ref STRRET_CSTR lpName);

            void SetNameOf(IntPtr hwndOwner, IntPtr pidl,
              [MarshalAs(UnmanagedType.LPWStr)]string lpszName,
              [MarshalAs(UnmanagedType.U4)] ESHCONTF uFlags,
              ref IntPtr ppidlOut);
        }

        [ComImport]
        [Guid("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IExtractImage
        {
            void GetLocation([Out(), MarshalAs(UnmanagedType.LPWStr)]
        StringBuilder pszPathBuffer, int cch, ref int pdwPriority, ref SIZE prgSize, int dwRecClrDepth, ref int pdwFlags);

            void Extract(ref IntPtr phBmpThumbnail);
        }

        [ComImport]
        [Guid("00000000-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IUnknown
        {

            [PreserveSig()]
            IntPtr QueryInterface(ref Guid riid, ref IntPtr pVoid);

            [PreserveSig()]
            IntPtr AddRef();

            [PreserveSig()]
            IntPtr Release();
        }

        #endregion

        #region enum

        [Flags]
        internal enum ESTRRET
        {
            STRRET_WSTR = 0,
            STRRET_OFFSET = 1,
            STRRET_CSTR = 2
        }

        [Flags]
        internal enum ESHCONTF
        {
            SHCONTF_FOLDERS = 32,
            SHCONTF_NONFOLDERS = 64,
            SHCONTF_INCLUDEHIDDEN = 128,
        }

        [Flags]
        internal enum ESHGDN
        {
            SHGDN_NORMAL = 0,
            SHGDN_INFOLDER = 1,
            SHGDN_FORADDRESSBAR = 16384,
            SHGDN_FORPARSING = 32768
        }

        [Flags]
        internal enum ESFGAO
        {
            SFGAO_CANCOPY = 1,
            SFGAO_CANMOVE = 2,
            SFGAO_CANLINK = 4,
            SFGAO_CANRENAME = 16,
            SFGAO_CANDELETE = 32,
            SFGAO_HASPROPSHEET = 64,
            SFGAO_DROPTARGET = 256,
            SFGAO_CAPABILITYMASK = 375,
            SFGAO_LINK = 65536,
            SFGAO_SHARE = 131072,
            SFGAO_READONLY = 262144,
            SFGAO_GHOSTED = 524288,
            SFGAO_DISPLAYATTRMASK = 983040,
            SFGAO_FILESYSANCESTOR = 268435456,
            SFGAO_FOLDER = 536870912,
            SFGAO_FILESYSTEM = 1073741824,
            SFGAO_HASSUBFOLDER = -2147483648,
            SFGAO_CONTENTSMASK = -2147483648,
            SFGAO_VALIDATE = 16777216,
            SFGAO_REMOVABLE = 33554432,
            SFGAO_COMPRESSED = 67108864,
        }

        internal enum EIEIFLAG
        {
            IEIFLAG_ASYNC = 1,
            IEIFLAG_CACHE = 2,
            IEIFLAG_ASPECT = 4,
            IEIFLAG_OFFLINE = 8,
            IEIFLAG_GLEAM = 16,
            IEIFLAG_SCREEN = 32,
            IEIFLAG_ORIGSIZE = 64,
            IEIFLAG_NOSTAMP = 128,
            IEIFLAG_NOBORDER = 256,
            IEIFLAG_QUALITY = 512
        }

        #endregion
    }
}
