using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static MyLibrary.NativeMethods;

namespace MyLibrary.Interop
{
    public class ShellThumbnail : IDisposable
    {
        public static Icon GetFileIcon(string path, bool smallSize, bool linkOverlay = false)
        {
            var shfi = new Shfileinfo();
            var flags = ShgfiIcon | ShgfiUsefileattributes;
            if (linkOverlay)
            {
                flags += ShgfiLinkoverlay;
            }

            /* Check the size specified for return. */
            if (smallSize)
            {
                flags += ShgfiSmallicon;
            }
            else
            {
                flags += ShgfiLargeicon;
            }
            SHGetFileInfo(path,
                FileAttributeNormal,
                ref shfi,
                (uint)Marshal.SizeOf(shfi),
                flags);

            // Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
            var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
            DestroyIcon(shfi.hIcon); // Cleanup
            return icon;
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                if (_alloc != null)
                {
                    Marshal.ReleaseComObject(_alloc);
                }
                _alloc = null;
                if (ThumbNail != null)
                {
                    ThumbNail.Dispose();
                }
                _disposed = true;
            }
        }
        ~ShellThumbnail()
        {
            Dispose();
        }


        public Bitmap ThumbNail { get; private set; }
        public Size DesiredSize { get; set; } = new Size(100, 100);
        public Bitmap GetThumbnail(string fileName)
        {
            if (ThumbNail != null)
            {
                ThumbNail.Dispose();
                ThumbNail = null;
            }
            IShellFolder folder = null;
            try
            {
                folder = GetDesktopFolder;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (folder != null)
            {
                IntPtr pidlMain = IntPtr.Zero;
                try
                {
                    int cParsed = 0;
                    int pdwAttrib = 0;
                    string filePath = Path.GetDirectoryName(fileName);
                    folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, filePath, ref cParsed, ref pidlMain, ref pdwAttrib);
                }
                catch (Exception ex)
                {
                    Marshal.ReleaseComObject(folder);
                    throw ex;
                }
                if (pidlMain != IntPtr.Zero)
                {
                    Guid iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
                    IShellFolder item = null;
                    try
                    {
                        folder.BindToObject(pidlMain, IntPtr.Zero, ref iidShellFolder, ref item);
                    }
                    catch (Exception ex)
                    {
                        Marshal.ReleaseComObject(folder);
                        Allocator.Free(pidlMain);
                        throw ex;
                    }
                    if (item != null)
                    {
                        IEnumIDList idEnum = null;
                        try
                        {
                            item.EnumObjects(IntPtr.Zero, (ESHCONTF.SHCONTF_FOLDERS | ESHCONTF.SHCONTF_NONFOLDERS), ref idEnum);
                        }
                        catch (Exception ex)
                        {
                            Marshal.ReleaseComObject(folder);
                            Allocator.Free(pidlMain);
                            throw ex;
                        }
                        if (idEnum != null)
                        {
                            int hRes = 0;
                            IntPtr pidl = IntPtr.Zero;
                            int fetched = 0;
                            bool complete = false;
                            while (!complete)
                            {
                                hRes = idEnum.Next(1, ref pidl, ref fetched);
                                if (hRes != 0)
                                {
                                    pidl = IntPtr.Zero;
                                    complete = true;
                                }
                                else
                                {
                                    if (GetThumbNail(fileName, pidl, item))
                                    {
                                        complete = true;
                                    }
                                }
                                if (pidl != IntPtr.Zero)
                                {
                                    Allocator.Free(pidl);
                                }
                            }
                            Marshal.ReleaseComObject(idEnum);
                        }
                        Marshal.ReleaseComObject(item);
                    }
                    Allocator.Free(pidlMain);
                }
                Marshal.ReleaseComObject(folder);
            }
            return ThumbNail;
        }





        private IMalloc Allocator
        {
            get
            {
                if (!_disposed)
                {
                    if (_alloc == null)
                    {
                        SHGetMalloc(ref _alloc);
                    }
                }
                else
                {
                    Debug.Assert(false, "Object has been disposed.");
                }
                return _alloc;
            }
        }
        private bool GetThumbNail(string file, IntPtr pidl, IShellFolder item)
        {
            IntPtr hBmp = IntPtr.Zero;
            IExtractImage extractImage = null;
            try
            {
                string pidlPath = PathFromPidl(pidl);
                if (Path.GetFileName(pidlPath).ToUpper().Equals(Path.GetFileName(file).ToUpper()))
                {
                    IUnknown iunk = null;
                    int prgf = 0;
                    Guid iidExtractImage = new Guid("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1");
                    item.GetUIObjectOf(IntPtr.Zero, 1, ref pidl, ref iidExtractImage, ref prgf, ref iunk);
                    extractImage = (IExtractImage)iunk;
                    if (extractImage != null)
                    {
                        SIZE sz = new SIZE
                        {
                            cx = DesiredSize.Width,
                            cy = DesiredSize.Height
                        };
                        StringBuilder location = new StringBuilder(260, 260);
                        int priority = 0;
                        int requestedColourDepth = 32;
                        EIEIFLAG flags = EIEIFLAG.IEIFLAG_ASPECT | EIEIFLAG.IEIFLAG_SCREEN;
                        int uFlags = (int)flags;
                        try
                        {
                            extractImage.GetLocation(location, location.Capacity, ref priority, ref sz, requestedColourDepth, ref uFlags);
                            extractImage.Extract(ref hBmp);
                        }
                        catch (COMException)
                        {

                        }
                        if (hBmp != IntPtr.Zero)
                        {
                            ThumbNail = Image.FromHbitmap(hBmp);
                        }
                        Marshal.ReleaseComObject(extractImage);
                        extractImage = null;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (hBmp != IntPtr.Zero)
                {
                    DeleteObject(hBmp);
                }
                if (extractImage != null)
                {
                    Marshal.ReleaseComObject(extractImage);
                }
                throw ex;
            }
        }
        private string PathFromPidl(IntPtr pidl)
        {
            StringBuilder path = new StringBuilder(260, 260);
            int result = SHGetPathFromIDList(pidl, path);
            if (result == 0)
            {
                return string.Empty;
            }
            else
            {
                return path.ToString();
            }
        }
        private IShellFolder GetDesktopFolder
        {
            get
            {
                IShellFolder ppshf = null;
                int r = SHGetDesktopFolder(ref ppshf);
                return ppshf;
            }
        }
        private IMalloc _alloc = null;
        private bool _disposed = false;

    }
}