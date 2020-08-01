using MyLibrary.Win32.Interop;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MyLibrary.Win32.Controls
{
    [DefaultEvent(nameof(ClipboardChanged))]
    public class ClipboardMonitor : Control
    {
        public ClipboardMonitor()
        {
            BackColor = Color.Red;
            Visible = false;
            _nextClipboardViewer = (IntPtr)NativeMethods.SetClipboardViewer((int)Handle);
        }

        public event EventHandler<ClipboardChangedEventArgs> ClipboardChanged;

        protected override void WndProc(ref Message m)
        {
            // defined in winuser.h
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    OnClipboardChanged();
                    NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;

                case WM_CHANGECBCHAIN:
                    if (m.WParam == _nextClipboardViewer)
                    {
                        _nextClipboardViewer = m.LParam;
                    }
                    else
                    {
                        NativeMethods.SendMessage(_nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    }
                    break;

                default:
                    base.WndProc(ref m);
                    break;
            }
        }
        protected override void Dispose(bool disposing)
        {
            try
            {
                NativeMethods.ChangeClipboardChain(Handle, _nextClipboardViewer);
            }
            catch { }
        }

        private void OnClipboardChanged()
        {
            IDataObject iData = Clipboard.GetDataObject();
            ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(iData));
        }
        private IntPtr _nextClipboardViewer;
    }

    public class ClipboardChangedEventArgs : EventArgs
    {
        public IDataObject DataObject { get; private set; }

        public ClipboardChangedEventArgs(IDataObject dataObject)
        {
            DataObject = dataObject;
        }
    }
}
