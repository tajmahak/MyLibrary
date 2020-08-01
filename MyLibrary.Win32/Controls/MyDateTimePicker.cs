using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace MyLibrary.Win32.Controls
{
    [System.Diagnostics.DebuggerStepThrough]
    public class MyDateTimePicker : DateTimePicker
    {
        [DefaultValue(false)]
        public bool NextTabOnEnterButton { get; set; }
        public DateTime? CheckedValue
        {
            get => Checked ? (DateTime?)Value : null;
            set
            {
                if (value == null)
                {
                    Checked = false;
                }
                else
                {
                    Value = value.Value;
                    Checked = true;
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            SendKeys.Send((e.Delta > 0) ? "{UP}" : "{DOWN}");
            base.OnMouseWheel(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (NextTabOnEnterButton)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = e.SuppressKeyPress = true;
                    SendKeys.Send("{TAB}");
                }
            }
            base.OnKeyDown(e);
        }
    }
}
