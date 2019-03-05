using System.ComponentModel;
using System.Windows.Forms;

namespace MyLibrary.Controls
{
    [System.Diagnostics.DebuggerStepThrough]
    public class MyTextBox : TextBox
    {
        [DefaultValue(false)]
        public bool NextTabOnEnterButton { get; set; }

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

            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = e.SuppressKeyPress = true;
                base.SelectAll();
            }

            base.OnKeyDown(e);
        }
    }
}
