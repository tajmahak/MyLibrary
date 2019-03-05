using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyLibrary.Controls
{
    [System.Diagnostics.DebuggerStepThrough]
    public class MyPropertyGrid : PropertyGrid
    {
        private bool _readOnly;
        public bool ReadOnly
        {
            get { return _readOnly; }
            set
            {
                _readOnly = value;
                SetObjectAsReadOnly(SelectedObject, _readOnly);
            }
        }

        protected override void OnSelectedObjectsChanged(EventArgs e)
        {
            SetObjectAsReadOnly(SelectedObject, _readOnly);
            base.OnSelectedObjectsChanged(e);
        }
        private void SetObjectAsReadOnly(object selectedObject, bool isReadOnly)
        {
            if (SelectedObject != null)
            {
                TypeDescriptor.AddAttributes(SelectedObject, new Attribute[] { new ReadOnlyAttribute(_readOnly) });
                Refresh();
            }
        }
    }
}
