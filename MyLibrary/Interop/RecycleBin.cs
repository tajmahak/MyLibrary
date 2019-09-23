using System;
using static MyLibrary.Interop.NativeMethods;

namespace MyLibrary.Interop
{
    /// <summary>
    /// Send files directly to the recycle bin.
    /// </summary>
    public static class RecycleBin
    {
        /// <summary>
        /// Send file to recycle bin.  Display dialog, display warning if files are too big to fit (FOF_WANTNUKEWARNING)
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        public static bool Send(string path)
        {
            return Send(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);
        }

        /// <summary>
        /// Send file silently to recycle bin.  Surpress dialog, surpress errors, delete if too large.
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        public static bool SendSilent(string path)
        {
            return Send(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT);
        }

        private static bool Send(string path, FileOperationFlags flags)
        {
            try
            {
                if (IntPtr.Size == 8)
                {
                    var fs = new SHFILEOPSTRUCT_x64();
                    fs.wFunc = FileOperationType.FO_DELETE;
                    // important to double-terminate the string.
                    fs.pFrom = path + '\0' + '\0';
                    fs.fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags;
                    SHFileOperation_x64(ref fs);
                }
                else
                {
                    var fs = new SHFILEOPSTRUCT_x86();
                    fs.wFunc = FileOperationType.FO_DELETE;
                    // important to double-terminate the string.
                    fs.pFrom = path + '\0' + '\0';
                    fs.fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags;
                    SHFileOperation_x86(ref fs);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
