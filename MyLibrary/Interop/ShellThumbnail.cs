using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static MyLibrary.Interop.NativeMethods;

namespace MyLibrary.Interop
{
    public class ShellThumbnail : IDisposable
    {
        /// <summary>
        /// Получение значка, сопоставленного с расширением файла
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="smallSize"></param>
        /// <param name="linkOverlay"></param>
        /// <returns></returns>
        public static Icon GetFileIcon(string filePath, bool smallSize, bool linkOverlay = false)
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
            SHGetFileInfo(filePath,
                FileAttributeNormal,
                ref shfi,
                (uint)Marshal.SizeOf(shfi),
                flags);

            // Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
            var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
            DestroyIcon(shfi.hIcon); // Cleanup
            return icon;
        }
        /// <summary>
        /// Получение эскиза файла
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public Bitmap GetFileThumbnail(string filePath, Size size)
        {
            Bitmap thumbnail = null;
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
                var pidlMain = IntPtr.Zero;
                try
                {
                    var cParsed = 0;
                    var pdwAttrib = 0;
                    var directoryName = Path.GetDirectoryName(filePath);
                    folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, directoryName, ref cParsed, ref pidlMain, ref pdwAttrib);
                }
                catch (Exception ex)
                {
                    Marshal.ReleaseComObject(folder);
                    throw ex;
                }
                if (pidlMain != IntPtr.Zero)
                {
                    var iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
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
                            var hRes = 0;
                            var pidl = IntPtr.Zero;
                            var fetched = 0;
                            var complete = false;
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
                                    if (GetThumbNail(filePath, pidl, item, size, ref thumbnail))
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
            return thumbnail;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_alloc != null)
                {
                    Marshal.ReleaseComObject(_alloc);
                    _alloc = null;
                }
                _disposed = true;
            }
        }
        ~ShellThumbnail()
        {
            Dispose();
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
                return _alloc;
            }
        }
        private bool GetThumbNail(string file, IntPtr pidl, IShellFolder item, Size size, ref Bitmap thumbnail)
        {
            var hBmp = IntPtr.Zero;
            IExtractImage extractImage = null;
            try
            {
                var pidlPath = PathFromPidl(pidl);
                if (Path.GetFileName(pidlPath).ToUpper().Equals(Path.GetFileName(file).ToUpper()))
                {
                    IUnknown iunk = null;
                    var prgf = 0;
                    var iidExtractImage = new Guid("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1");
                    item.GetUIObjectOf(IntPtr.Zero, 1, ref pidl, ref iidExtractImage, ref prgf, ref iunk);
                    extractImage = (IExtractImage)iunk;
                    if (extractImage != null)
                    {
                        var sz = new SIZE
                        {
                            cx = size.Width,
                            cy = size.Height
                        };
                        var location = new StringBuilder(260, 260);
                        var priority = 0;
                        var requestedColourDepth = 32;
                        var flags = EIEIFLAG.IEIFLAG_ASPECT | EIEIFLAG.IEIFLAG_SCREEN;
                        var uFlags = (int)flags;
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
                            thumbnail = Image.FromHbitmap(hBmp);
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
            var path = new StringBuilder(260, 260);
            var result = SHGetPathFromIDList(pidl, path);
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
                var r = SHGetDesktopFolder(ref ppshf);
                return ppshf;
            }
        }
        private IMalloc _alloc;
        private bool _disposed = false;
    }
}